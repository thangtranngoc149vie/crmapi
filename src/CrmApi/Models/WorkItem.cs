using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmApi.Models;

public class WorkItem
{
    public Guid Id { get; set; }

    [MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    public WorkItemStatus Status { get; set; } = WorkItemStatus.Draft;

    [MaxLength(64)]
    public string? Priority { get; set; }

    public Guid? AssigneeId { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    [ConcurrencyCheck]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long StateVersion { get; set; }

    public ICollection<WorkItemComment> Comments { get; set; } = new List<WorkItemComment>();

    public ICollection<WorkItemAttachment> Attachments { get; set; } = new List<WorkItemAttachment>();
}
