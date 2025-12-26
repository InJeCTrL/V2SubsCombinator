terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

variable "subscription_id" {
  description = "Azure Subscription ID"
}

variable "location" {
  default = "japaneast"
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

# Cosmos DB with MongoDB Compatibility
resource "azurerm_cosmosdb_account" "db" {
  name                = "mongo-${var.project_name}-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  kind                = "MongoDB" # Set to MongoDB API
  offer_type          = "Standard"
  consistency_policy {
    consistency_level = "Session"
  }

  # At least one geo_location block is required, so keep it here
  geo_location {
    location          = var.location
    failover_priority = 0
  }

  # Removed backup block to avoid errors
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
        name        = "JWTSettings__Key"
        secret_name = "jwt-secret"
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
}

# Custom Domain Name (certificate needs to be configured manually in Azure Portal)
resource "azurerm_container_app_custom_domain" "custom" {
  count            = var.custom_domain != "" ? 1 : 0
  name             = var.custom_domain
  container_app_id = azurerm_container_app.app.id

  lifecycle {
    ignore_changes = [certificate_binding_type, container_app_environment_certificate_id]
  }
}

# Outputs
output "app_url" {
  value = "https://${azurerm_container_app.app.ingress[0].fqdn}"
}

output "mongodb_connection_string" {
  value     = azurerm_cosmosdb_account.db.primary_mongodb_connection_string
  sensitive = true
}

output "custom_domain_setup" {
  sensitive = true
  value = var.custom_domain != "" ? join("\n", [
    "Custom domain setup:",
    "1. Add CNAME record: ${var.custom_domain} -> ${azurerm_container_app.app.ingress[0].fqdn}",
    "2. Add TXT record: asuid.${var.custom_domain} -> ${azurerm_container_app.app.custom_domain_verification_id}",
    "3. After DNS propagation, go to Azure Portal -> Container App -> Custom domains -> Add managed certificate"
  ]) : "No custom domain configured"
}
