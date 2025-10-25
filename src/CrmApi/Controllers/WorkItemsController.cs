using CrmApi.Dtos;
using CrmApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

[ApiController]
[Route("api/crm/work-items")]
public class WorkItemsController : ControllerBase
{
    private readonly WorkItemService _service;

    public WorkItemsController(WorkItemService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<WorkItemSummary>> CreateAsync([FromBody] WorkItemCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetDetailAsync), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<WorkItemSummary>>> ListAsync(
        [FromQuery] WorkItemListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _service.ListAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkItemDetailResponse>> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetDetailAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkItemDetailResponse>> UpdateAsync(Guid id, [FromBody] WorkItemUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("If-Match", out var etag) || !long.TryParse(etag.ToString().Trim('"'), out var expectedVersion))
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired);
        }

        try
        {
            var result = await _service.UpdateAsync(id, request, expectedVersion, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed);
        }
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<WorkItemCommentResponse>> AddCommentAsync(Guid id, [FromBody] WorkItemCommentCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.AddCommentAsync(id, request, cancellationToken);
        return result is null ? NotFound() : CreatedAtAction(nameof(GetDetailAsync), new { id }, result);
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<ActionResult<WorkItemAttachmentResponse>> AddAttachmentAsync(Guid id, [FromBody] WorkItemAttachmentCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.AddAttachmentAsync(id, request, cancellationToken);
        return result is null ? NotFound() : CreatedAtAction(nameof(GetDetailAsync), new { id }, result);
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<WorkItemDetailResponse>> AssignAsync(Guid id, [FromBody] WorkItemAssignRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.AssignAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<WorkItemDetailResponse>> ChangeStatusAsync(Guid id, [FromBody] WorkItemStatusChangeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.TransitionStatusAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
