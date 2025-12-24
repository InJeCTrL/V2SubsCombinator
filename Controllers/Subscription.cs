using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using V2SubsCombinator.Attributes;
using V2SubsCombinator.DTOs;
using V2SubsCombinator.IServices;

namespace V2SubsCombinator.Controllers;

[Authorize]
[ApiController]
[Route("/api/[controller]")]
public class SubscriptionController(ISubscription subscriptionService) : ControllerBase
{
    private readonly ISubscription _subscriptionService = subscriptionService;

    [HttpGet("groups")]
    [InjectUserId]
    public async Task<ActionResult<ExportSubGroupResult>> GetExportSubGroups([FromQuery] GetExportSubGroupRequest request)
    {
        var result = await _subscriptionService.GetExportSubGroupsAsync(request);
        return Ok(result);
    }

    [HttpGet("groups/detail")]
    [InjectUserId]
    public async Task<ActionResult<ExportSubGroupResult>> GetExportSubGroupDetail([FromQuery] GetExportSubGroupRequest request)
    {
        var result = await _subscriptionService.GetExportSubGroupDetailAsync(request);
        return Ok(result);
    }

    [HttpPost("groups")]
    [InjectUserId]
    public async Task<ActionResult<ExportSubGroupResult>> AddExportSubGroup([FromBody] AddExportSubGroupRequest request)
    {
        var result = await _subscriptionService.AddExportSubGroupAsync(request);
        return Ok(result);
    }

    [HttpPut("groups")]
    [InjectUserId]
    public async Task<ActionResult<ExportSubGroupResult>> UpdateExportSubGroup([FromBody] UpdateExportSubGroupRequest request)
    {
        var result = await _subscriptionService.UpdateExportSubGroupAsync(request);
        return Ok(result);
    }

    [HttpDelete("groups")]
    [InjectUserId]
    public async Task<ActionResult<ExportSubGroupResult>> RemoveExportSubGroup([FromQuery] RemoveExportSubGroupRequest request)
    {
        var result = await _subscriptionService.RemoveExportSubGroupAsync(request);
        return Ok(result);
    }

    [HttpPost("import-subs")]
    [InjectUserId]
    public async Task<ActionResult<ImportSubResult>> AddImportSub([FromBody] AddImportSubRequest request)
    {
        var result = await _subscriptionService.AddImportSubToExportSubGroupAsync(request);
        return Ok(result);
    }

    [HttpPut("import-subs")]
    [InjectUserId]
    public async Task<ActionResult<ImportSubResult>> UpdateImportSub([FromBody] UpdateImportSubRequest request)
    {
        var result = await _subscriptionService.UpdateImportSubAsync(request);
        return Ok(result);
    }

    [HttpDelete("import-subs")]
    [InjectUserId]
    public async Task<ActionResult<ImportSubResult>> RemoveImportSub([FromQuery] RemoveImportSubRequest request)
    {
        var result = await _subscriptionService.RemoveImportSubFromExportSubGroupAsync(request);
        return Ok(result);
    }

    [HttpPost("export-subs")]
    [InjectUserId]
    public async Task<ActionResult<ExportSubResult>> AddExportSub([FromBody] AddExportSubRequest request)
    {
        var result = await _subscriptionService.AddExportSubToExportSubGroupAsync(request);
        return Ok(result);
    }

    [HttpPut("export-subs")]
    [InjectUserId]
    public async Task<ActionResult<ExportSubResult>> UpdateExportSub([FromBody] UpdateExportSubRequest request)
    {
        var result = await _subscriptionService.UpdateExportSubAsync(request);
        return Ok(result);
    }

    [HttpDelete("export-subs")]
    [InjectUserId]
    public async Task<ActionResult<ExportSubResult>> RemoveExportSub([FromQuery] RemoveExportSubRequest request)
    {
        var result = await _subscriptionService.RemoveExportSubFromExportSubGroupAsync(request);
        return Ok(result);
    }

    [AllowAnonymous]
    [Route("/sub/{suffix}")]
    [HttpGet]
    public async Task<ActionResult> GetExportSubContent([FromRoute] string suffix)
    {
        var request = new GetExportSubContentRequest { Suffix = suffix };
        var result = await _subscriptionService.GetExportSubContentAsync(request);
        
        if (string.IsNullOrEmpty(result))
            return NotFound();

        var bytes = System.Text.Encoding.UTF8.GetBytes(result);
        Response.Headers.Append("subscription-userinfo", "name=Aladdin_Network_Optimizer; remark=Aladdin_Network_Optimizer; upload=5332838657; download=130590945813; total=685047283712; expire=1772798220");
        Response.Headers.Append("content-disposition", "attachment; filename*=UTF-8''Config.txt");
        // return File(bytes, "application/octet-stream; charset=utf-8", "Config.txt");
        return File(bytes, "application/octet-stream; charset=utf-8");
    }
}
