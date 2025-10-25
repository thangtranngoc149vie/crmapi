using System.ComponentModel.DataAnnotations;

namespace CrmApi.Models;

public class WorkItemComment
{
    public Guid Id { get; set; }

    public Guid WorkItemId { get; set; }

    public required WorkItem WorkItem { get; set; }

    [MaxLength(2000)]
    public required string Body { get; set; }

    public Guid? AuthorId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
