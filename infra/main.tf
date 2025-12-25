terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

variable "location" {
  default = "eastasia"
}

variable "project_name" {
  default = "v2subscomb"
}

variable "ghcr_image" {
  description = "GitHub Container Registry image, e.g. ghcr.io/username/v2subscomb:latest"
}

variable "jwt_secret" {
  sensitive = true
}

variable "custom_domain" {
  default = ""
}

resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-${var.project_name}"
  location = var.location
}

# Cosmos DB (MongoDB API)
resource "azurerm_cosmosdb_account" "db" {
  name                = "cosmos-${var.project_name}-${random_string.suffix.result}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  offer_type          = "Standard"
  kind                = "MongoDB"

  capabilities {
    name = "EnableMongo"
  }

  capabilities {
    name = "EnableServerless"
  }

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = azurerm_resource_group.rg.location
    failover_priority = 0
  }
}

resource "azurerm_cosmosdb_mongo_database" "mongodb" {
  name                = "V2SubsCombinator"
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.db.name
}


# Log Analytics
resource "azurerm_log_analytics_workspace" "logs" {
  name                = "logs-${var.project_name}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# Container Apps Environment
resource "azurerm_container_app_environment" "env" {
  name                       = "cae-${var.project_name}"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.logs.id
}

# Container App
resource "azurerm_container_app" "app" {
  name                         = "ca-${var.project_name}"
  container_app_environment_id = azurerm_container_app_environment.env.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  secret {
    name  = "mongodb-conn"
    value = azurerm_cosmosdb_account.db.primary_mongodb_connection_string
  }

  secret {
    name  = "jwt-secret"
    value = var.jwt_secret
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "v2subscomb"
      image  = var.ghcr_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name        = "ConnectionStrings__DefaultConnection"
        secret_name = "mongodb-conn"
      }

      env {
        name  = "ConnectionStrings__Database"
        value = "V2SubsCombinator"
      }

      env {
        name        = "Jwt__Secret"
        secret_name = "jwt-secret"
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "http2"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
}


# Custom Domain Name
resource "azurerm_container_app_custom_domain" "custom" {
  count            = var.custom_domain != "" ? 1 : 0
  name             = var.custom_domain
  container_app_id = azurerm_container_app.app.id
}

# Outputs
output "app_url" {
  value = "https://${azurerm_container_app.app.ingress[0].fqdn}"
}

output "mongodb_connection_string" {
  value     = azurerm_cosmosdb_account.db.primary_mongodb_connection_string
  sensitive = true
}

output "custom_domain_cname" {
  value = var.custom_domain != "" ? "Add CNAME: ${var.custom_domain} -> ${azurerm_container_app.app.ingress[0].fqdn}" : "No custom domain name"
}
