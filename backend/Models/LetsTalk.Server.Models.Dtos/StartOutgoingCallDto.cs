using System.ComponentModel.DataAnnotations;

namespace LetsTalk.Server.Models.Dtos;

public class StartOutgoingCallDto
{
    [Required]
    public required string CallId { get; set; }
}
