using CrmApi.Data;
using CrmApi.Dtos;
using CrmApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Services;

public class WorkItemService
{
    private static readonly IReadOnlyDictionary<WorkItemStatus, WorkItemStatus[]> AllowedTransitions =
        new Dictionary<WorkItemStatus, WorkItemStatus[]>
        {
            [WorkItemStatus.Draft] = new[] { WorkItemStatus.Open, WorkItemStatus.Cancelled },
            [WorkItemStatus.Open] = new[] { WorkItemStatus.InProgress, WorkItemStatus.Cancelled },
            [WorkItemStatus.InProgress] = new[] { WorkItemStatus.Resolved, WorkItemStatus.Cancelled },
            [WorkItemStatus.Resolved] = new[] { WorkItemStatus.Closed, WorkItemStatus.InProgress },
            [WorkItemStatus.Closed] = Array.Empty<WorkItemStatus>(),
            [WorkItemStatus.Cancelled] = Array.Empty<WorkItemStatus>()
        };

    private readonly CrmDbContext _context;

    public WorkItemService(CrmDbContext context)
    {
        _context = context;
    }

    public async Task<WorkItemSummary> CreateAsync(WorkItemCreateRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new WorkItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            AssigneeId = request.AssigneeId,
            DueDate = request.DueDate,
            CreatedAt = now,
            UpdatedAt = now,
            Status = WorkItemStatus.Draft,
            StateVersion = 1
        };

        await _context.WorkItems.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapSummary(entity);
    }

    public async Task<PaginatedResult<WorkItemSummary>> ListAsync(WorkItemListQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var baseQuery = _context.WorkItems.AsNoTracking();

        if (query.Status is not null)
        {
            baseQuery = baseQuery.Where(x => x.Status == query.Status);
        }

        if (query.AssigneeId is not null)
        {
            baseQuery = baseQuery.Where(x => x.AssigneeId == query.AssigneeId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            baseQuery = baseQuery.Where(x => EF.Functions.ILike(x.Title, $"%{search}%") ||
                                             (x.Description != null && EF.Functions.ILike(x.Description, $"%{search}%")));
        }

        var total = await baseQuery.CountAsync(cancellationToken);
        var entities = await baseQuery
            .OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(MapSummary).ToList();

        return new PaginatedResult<WorkItemSummary>(items, page, pageSize, total);
    }

    public async Task<WorkItemDetailResponse?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _context.WorkItems
            .AsNoTracking()
            .Include(x => x.Comments)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : MapDetail(entity);
    }

    public async Task<WorkItemDetailResponse?> UpdateAsync(Guid id, WorkItemUpdateRequest request, long expectedVersion, CancellationToken cancellationToken)
    {
        var entity = await _context.WorkItems
            .Include(x => x.Comments)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        if (entity.StateVersion != expectedVersion)
        {
            throw new DbUpdateConcurrencyException("The work item was modified by another request.");
        }

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Priority = request.Priority;
        entity.AssigneeId = request.AssigneeId;
        entity.DueDate = request.DueDate;
        entity.Status = request.Status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.StateVersion += 1;

        await _context.SaveChangesAsync(cancellationToken);

        return MapDetail(entity);
    }

    public async Task<WorkItemCommentResponse?> AddCommentAsync(Guid workItemId, WorkItemCommentCreateRequest request, CancellationToken cancellationToken)
    {
        var workItem = await _context.WorkItems.FirstOrDefaultAsync(x => x.Id == workItemId, cancellationToken);

        if (workItem is null)
        {
            return null;
        }

        var comment = new WorkItemComment
        {
            Id = Guid.NewGuid(),
            WorkItemId = workItemId,
            WorkItem = workItem,
            Body = request.Body,
            AuthorId = request.AuthorId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _context.WorkItemComments.AddAsync(comment, cancellationToken);
        workItem.UpdatedAt = DateTimeOffset.UtcNow;
        workItem.StateVersion += 1;
        await _context.SaveChangesAsync(cancellationToken);

        return new WorkItemCommentResponse(comment.Id, comment.Body, comment.AuthorId, comment.CreatedAt);
    }

    public async Task<WorkItemAttachmentResponse?> AddAttachmentAsync(Guid workItemId, WorkItemAttachmentCreateRequest request, CancellationToken cancellationToken)
    {
        var workItem = await _context.WorkItems.FirstOrDefaultAsync(x => x.Id == workItemId, cancellationToken);

        if (workItem is null)
        {
            return null;
        }

        var attachment = new WorkItemAttachment
        {
            Id = Guid.NewGuid(),
            WorkItemId = workItemId,
            WorkItem = workItem,
            FileName = request.FileName,
            ContentType = request.ContentType,
            Size = request.Size,
            StorageUri = request.StorageUri,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _context.WorkItemAttachments.AddAsync(attachment, cancellationToken);
        workItem.UpdatedAt = DateTimeOffset.UtcNow;
        workItem.StateVersion += 1;
        await _context.SaveChangesAsync(cancellationToken);

        return new WorkItemAttachmentResponse(attachment.Id, attachment.FileName, attachment.ContentType, attachment.Size, attachment.StorageUri, attachment.CreatedAt);
    }

    public async Task<WorkItemDetailResponse?> AssignAsync(Guid id, WorkItemAssignRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.Database.ExecuteSqlRawAsync(
            "SELECT 1 FROM \"WorkItems\" WHERE \"Id\" = {0} FOR UPDATE",
            new object?[] { id },
            cancellationToken);

        var entity = await _context.WorkItems
            .Include(x => x.Comments)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        entity.AssigneeId = request.AssigneeId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.StateVersion += 1;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MapDetail(entity);
    }

    public async Task<WorkItemDetailResponse?> TransitionStatusAsync(Guid id, WorkItemStatusChangeRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.WorkItems
            .Include(x => x.Comments)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        if (!AllowedTransitions.TryGetValue(entity.Status, out var allowed) || !allowed.Contains(request.TargetStatus))
        {
            throw new InvalidOperationException($"Transition from {entity.Status} to {request.TargetStatus} is not permitted.");
        }

        entity.Status = request.TargetStatus;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.StateVersion += 1;

        await _context.SaveChangesAsync(cancellationToken);

        return MapDetail(entity);
    }

    private static WorkItemSummary MapSummary(WorkItem workItem) =>
        new(workItem.Id, workItem.Title, workItem.Status, workItem.Priority, workItem.AssigneeId, workItem.DueDate, workItem.UpdatedAt, workItem.StateVersion);

    private static WorkItemDetailResponse MapDetail(WorkItem workItem)
    {
        var comments = workItem.Comments
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new WorkItemCommentResponse(c.Id, c.Body, c.AuthorId, c.CreatedAt))
            .ToList();

        var attachments = workItem.Attachments
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new WorkItemAttachmentResponse(a.Id, a.FileName, a.ContentType, a.Size, a.StorageUri, a.CreatedAt))
            .ToList();

        return new WorkItemDetailResponse(
            workItem.Id,
            workItem.Title,
            workItem.Description,
            workItem.Status,
            workItem.Priority,
            workItem.AssigneeId,
            workItem.DueDate,
            workItem.CreatedAt,
            workItem.UpdatedAt,
            workItem.StateVersion,
            comments,
            attachments);
    }
}
