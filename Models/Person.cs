using System.ComponentModel.DataAnnotations;

namespace Models;

public class Person
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string PassportNumber { get; set; } = null!;

    [Required, StringLength(100)]
    public string FirstName { get; set; } = null!;
    [StringLength(100)] public string? MiddleName { get; set; }
    [Required, StringLength(100)] public string LastName { get; set; } = null!;
    [Required, StringLength(20)] public string PhoneNumber { get; set; } = null!;
    [Required, StringLength(150), EmailAddress] public string Email { get; set; } = null!;
}