using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using Data;
using Models;
using DTOs;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);


var app = builder.Build();

// Devices endpoints
app.MapGet("/api/devices", async (AppDbContext db) =>
{
    var devices = await db.Devices
        .Select(d => new DeviceSummaryDto { Id = d.Id, Name = d.Name })
        .ToListAsync();
    return Results.Ok(devices);
});

app.MapGet("/api/devices/{id:int}", async (int id, AppDbContext db) =>
{
    var device = await db.Devices
        .Include(d => d.DeviceType)
        .Include(d => d.DeviceEmployees)
            .ThenInclude(de => de.Employee)
            .ThenInclude(e => e.Person)
        .FirstOrDefaultAsync(d => d.Id == id);

    if (device == null)
        return Results.NotFound(new { message = "Device not found." });

    var currentAssignment = device.DeviceEmployees
        .FirstOrDefault(de => de.ReturnDate == null);

    var dto = new DeviceDetailDto
    {
        DeviceTypeName = device.DeviceType?.Name ?? string.Empty,
        IsEnabled = device.IsEnabled,
        AdditionalProperties = JsonDocument.Parse(device.AdditionalProperties).RootElement,
        CurrentEmployee = currentAssignment == null ? null : new CurrentEmployeeDto
        {
            Id = currentAssignment.Employee.Id,
            FullName = $"{currentAssignment.Employee.Person.FirstName} {currentAssignment.Employee.Person.LastName}"
        }
    };

    return Results.Ok(dto);
});

app.MapPost("/api/devices", async (DeviceCreateDto input, AppDbContext db) =>
{
    var type = await db.DeviceTypes.FirstOrDefaultAsync(t => t.Name == input.DeviceTypeName);
    if (type == null)
        return Results.BadRequest(new { message = "Invalid device type." });

    var device = new Device
    {
        Name = input.DeviceTypeName,
        DeviceTypeId = type.Id,
        IsEnabled = input.IsEnabled,
        AdditionalProperties = input.AdditionalProperties.GetRawText()
    };

    db.Devices.Add(device);
    await db.SaveChangesAsync();

    return Results.Created($"/api/devices/{device.Id}", new { device.Id });
});

app.MapPut("/api/devices/{id:int}", async (int id, DeviceUpdateDto input, AppDbContext db) =>
{
    var device = await db.Devices.FindAsync(id);
    if (device == null)
        return Results.NotFound(new { message = "Device not found." });

    var type = await db.DeviceTypes.FirstOrDefaultAsync(t => t.Name == input.DeviceTypeName);
    if (type == null)
        return Results.BadRequest(new { message = "Invalid device type." });

    device.DeviceTypeId = type.Id;
    device.IsEnabled = input.IsEnabled;
    device.AdditionalProperties = input.AdditionalProperties.GetRawText();
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/api/devices/{id:int}", async (int id, AppDbContext db) =>
{
    var device = await db.Devices.FindAsync(id);
    if (device == null)
        return Results.NotFound(new { message = "Device not found." });

    db.Devices.Remove(device);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Employees endpoints
app.MapGet("/api/employees", async (AppDbContext db) =>
{
    var employees = await db.Employees
        .Include(e => e.Person)
        .Select(e => new EmployeeSummaryDto
        {
            Id = e.Id,
            FullName = $"{e.Person.FirstName} {e.Person.LastName}"
        })
        .ToListAsync();
    return Results.Ok(employees);
});

app.MapGet("/api/employees/{id:int}", async (int id, AppDbContext db) =>
{
    var emp = await db.Employees
        .Include(e => e.Person)
        .Include(e => e.Position)
        .FirstOrDefaultAsync(e => e.Id == id);

    if (emp == null)
        return Results.NotFound(new { message = "Employee not found." });

    var dto = new EmployeeDetailDto
    {
        PassportNumber = emp.Person.PassportNumber,
        FirstName = emp.Person.FirstName,
        MiddleName = emp.Person.MiddleName,
        LastName = emp.Person.LastName,
        PhoneNumber = emp.Person.PhoneNumber,
        Email = emp.Person.Email,
        Salary = emp.Salary,
        Position = new PositionDto { Id = emp.Position.Id, Name = emp.Position.Name },
        HireDate = emp.HireDate
    };

    return Results.Ok(dto);
});

app.Run();