using System.ComponentModel.DataAnnotations;

namespace Models;

public class Position
{
    public int Id { get; set; }

    [Required, StringLength(100)] public string Name { get; set; } = null!;
    public int MinExpYears { get; set; }
}