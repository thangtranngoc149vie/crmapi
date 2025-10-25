using System.ComponentModel.DataAnnotations;
using CrmApi.Models;

namespace CrmApi.Dtos;

public sealed record WorkItemCreateRequest(
    [property: Required, MaxLength(200)] string Title,
    [property: MaxLength(4000)] string? Description,
    [property: MaxLength(64)] string? Priority,
    Guid? AssigneeId,
    DateTimeOffset? DueDate
);

public sealed record WorkItemUpdateRequest(
    [property: Required, MaxLength(200)] string Title,
    [property: MaxLength(4000)] string? Description,
    [property: MaxLength(64)] string? Priority,
    Guid? AssigneeId,
    DateTimeOffset? DueDate,
    WorkItemStatus Status
);

public sealed record WorkItemSummary(Guid Id, string Title, WorkItemStatus Status, string? Priority, Guid? AssigneeId, DateTimeOffset? DueDate, DateTimeOffset UpdatedAt, long StateVersion);

public sealed record PaginatedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);

public sealed record WorkItemCommentResponse(Guid Id, string Body, Guid? AuthorId, DateTimeOffset CreatedAt);

public sealed record WorkItemAttachmentResponse(Guid Id, string FileName, string? ContentType, long Size, string? StorageUri, DateTimeOffset CreatedAt);

public sealed record WorkItemDetailResponse(Guid Id, string Title, string? Description, WorkItemStatus Status, string? Priority, Guid? AssigneeId, DateTimeOffset? DueDate, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, long StateVersion, IReadOnlyCollection<WorkItemCommentResponse> Comments, IReadOnlyCollection<WorkItemAttachmentResponse> Attachments);

public sealed record WorkItemAssignRequest([property: Required] Guid AssigneeId);

public sealed record WorkItemStatusChangeRequest([property: Required] WorkItemStatus TargetStatus);

public sealed record WorkItemCommentCreateRequest([property: Required, MaxLength(2000)] string Body, Guid? AuthorId);

public sealed record WorkItemAttachmentCreateRequest(
    [property: Required, MaxLength(256)] string FileName,
    [property: MaxLength(128)] string? ContentType,
    long Size,
    [property: MaxLength(512)] string? StorageUri
);

public sealed record WorkItemListQuery(
    WorkItemStatus? Status,
    Guid? AssigneeId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null
);
