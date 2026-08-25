using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetsTalk.Server.Domain;

[Table("accounts")]
[Index(nameof(ExternalId), nameof(AccountTypeId), IsUnique = true)]
public class Account : BaseEntity
{
    public AccountType? AccountType { get; protected set; }

    public int AccountTypeId { get; protected set; }

    [MaxLength(100)]
    public string? ExternalId { get; protected set; }

    [MaxLength(100)]
    public string? Email { get; protected set; }

    [MaxLength(4000)]
    public string? PhotoUrl { get; protected set; }

    [MaxLength(100)]
    public string? FirstName { get; protected set; }

    [MaxLength(100)]
    public string? LastName { get; protected set; }

    public Image? Image { get; protected set; }

    public string? ImageId { get; protected set; }

    public Account(int accountTypeId, string email)
    {
        AccountTypeId = accountTypeId;
        Email = email;
    }

    protected Account()
    {
    }

    public void UpdateProfile(string firstName, string lastName, Image? image = null)
    {
        FirstName = firstName;
        LastName = lastName;

        if (image != null)
        {
            Image = image;
        }
    }
}
