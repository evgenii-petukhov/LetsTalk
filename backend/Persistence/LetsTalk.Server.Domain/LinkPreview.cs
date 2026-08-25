using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetsTalk.Server.Domain;

[Table("linkpreviews")]
[Index(nameof(Url), IsUnique = true)]
public class LinkPreview : BaseEntity
{
    [Required]
    [MaxLength(4000)]
    public string? Url { get; protected set; }

    [MaxLength(4000)]
    public string? Title { get; protected set; }

    [MaxLength(4000)]
    public string? ImageUrl { get; protected set; }

    protected LinkPreview()
    {
    }

    public LinkPreview(string url, string title, string imageUrl)
    {
        Url = url;
        Title = title;
        ImageUrl = imageUrl;
    }
}
