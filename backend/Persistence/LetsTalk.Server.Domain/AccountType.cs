using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetsTalk.Server.Domain;

[Table("accounttypes")]
public class AccountType(int id, string name) : BaseEntity(id)
{
    [MaxLength(100)]
    public string? Name { get; protected set; } = name;
}
