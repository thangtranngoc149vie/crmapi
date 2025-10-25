using System.ComponentModel.DataAnnotations;

namespace CrmApi.Models;

public class WorkItemAttachment
{
    public Guid Id { get; set; }

    public Guid WorkItemId { get; set; }

    public required WorkItem WorkItem { get; set; }

    [MaxLength(256)]
    public required string FileName { get; set; }

    [MaxLength(128)]
    public string? ContentType { get; set; }

    public long Size { get; set; }

    [MaxLength(512)]
    public string? StorageUri { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
