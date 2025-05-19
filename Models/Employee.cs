namespace Models;

public class Employee
{
    public int Id { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }

    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
}