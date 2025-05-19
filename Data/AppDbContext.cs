using Microsoft.EntityFrameworkCore;
using Models;

namespace Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<DeviceType> DeviceTypes { get; set; } = null!;
    public DbSet<Person> Persons { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    public DbSet<DeviceEmployee> DeviceEmployees { get; set; } = null!;

    
}