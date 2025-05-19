using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using DTOs;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var app = builder.Build();

app.MapGet("/api/devices", async () =>
{
    var list = new List<DeviceSummaryDto>();
    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new SqlCommand("SELECT Id, Name FROM Devices", conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        list.Add(new DeviceSummaryDto
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1)
        });
    }
    return Results.Ok(list);
});

app.MapGet("/api/devices/{id:int}", async (int id) =>
{
    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();

    var deviceQuery = @"
SELECT d.Id, dt.Name AS DeviceTypeName, d.IsEnabled, d.AdditionalProperties
FROM Devices d
JOIN DeviceTypes dt ON d.DeviceTypeId = dt.Id
WHERE d.Id = @Id";
    await using var cmd = new SqlCommand(deviceQuery, conn);
    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.NotFound(new { message = "Device not found." });

    var dto = new DeviceDetailDto
    {
        DeviceTypeName = reader.GetString(1),
        IsEnabled = reader.GetBoolean(2),
        AdditionalProperties = JsonDocument.Parse(reader.GetString(3)).RootElement,
        CurrentEmployee = null
    };
    reader.Close();

    var currentQuery = @"
SELECT e.Id, p.FirstName, p.LastName
FROM DeviceEmployees de
JOIN Employees e ON de.EmployeeId = e.Id
JOIN Persons p ON e.PersonId = p.Id
WHERE de.DeviceId = @Id AND de.ReturnDate IS NULL";
    await using var subCmd = new SqlCommand(currentQuery, conn);
    subCmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
    await using var subReader = await subCmd.ExecuteReaderAsync();
    if (await subReader.ReadAsync())
    {
        dto.CurrentEmployee = new CurrentEmployeeDto
        {
            Id = subReader.GetInt32(0),
            FullName = $"{subReader.GetString(1)} {subReader.GetString(2)}"
        };
    }
    return Results.Ok(dto);
});

app.MapPost("/api/devices", async (DeviceCreateDto input) =>
{
    if (string.IsNullOrWhiteSpace(input.DeviceTypeName))
        return Results.BadRequest(new { message = "DeviceTypeName is required." });

    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();
    await using var tx = await conn.BeginTransactionAsync();
    try
    {
        var typeCmd = new SqlCommand("SELECT Id FROM DeviceTypes WHERE Name = @Name", conn);
        typeCmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = input.DeviceTypeName;
        var typeId = (int?)await typeCmd.ExecuteScalarAsync();
        if (typeId is null)
            return Results.BadRequest(new { message = "Invalid device type." });

        var insertCmd = new SqlCommand(
            "INSERT INTO Devices (Name, DeviceTypeId, IsEnabled, AdditionalProperties) OUTPUT INSERTED.Id VALUES (@Name, @TypeId, @Enabled, @Props)",
            conn);
        insertCmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = input.DeviceTypeName;
        insertCmd.Parameters.Add("@TypeId", SqlDbType.Int).Value = typeId.Value;
        insertCmd.Parameters.Add("@Enabled", SqlDbType.Bit).Value = input.IsEnabled;
        insertCmd.Parameters.Add("@Props", SqlDbType.NVarChar).Value = input.AdditionalProperties.GetRawText();

        var newId = (int)await insertCmd.ExecuteScalarAsync();
        await tx.CommitAsync();
        return Results.Created($"/api/devices/{newId}", new { Id = newId });
    }
    catch
    {
        await tx.RollbackAsync();
        return Results.Problem("An error occurred creating the device.");
    }
});

app.MapPut("/api/devices/{id:int}", async (int id, DeviceUpdateDto input) =>
{
    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();
    await using var tx = await conn.BeginTransactionAsync();
    try
    {
        var existsCmd = new SqlCommand("SELECT COUNT(*) FROM Devices WHERE Id = @Id", conn);
        existsCmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        var exists = (int)await existsCmd.ExecuteScalarAsync() > 0;
        if (!exists)
            return Results.NotFound(new { message = "Device not found." });

        var typeCmd = new SqlCommand("SELECT Id FROM DeviceTypes WHERE Name = @Name", conn);
        typeCmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = input.DeviceTypeName;
        var typeId = (int?)await typeCmd.ExecuteScalarAsync();
        if (typeId is null)
            return Results.BadRequest(new { message = "Invalid device type." });

        var updateCmd = new SqlCommand(
            "UPDATE Devices SET DeviceTypeId = @TypeId, IsEnabled = @Enabled, AdditionalProperties = @Props WHERE Id = @Id",
            conn);
        updateCmd.Parameters.Add("@TypeId", SqlDbType.Int).Value = typeId.Value;
        updateCmd.Parameters.Add("@Enabled", SqlDbType.Bit).Value = input.IsEnabled;
        updateCmd.Parameters.Add("@Props", SqlDbType.NVarChar).Value = input.AdditionalProperties.GetRawText();
        updateCmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await updateCmd.ExecuteNonQueryAsync();

        await tx.CommitAsync();
        return Results.NoContent();
    }
    catch
    {
        await tx.RollbackAsync();
        return Results.Problem("An error occurred updating the device.");
    }
});

app.MapDelete("/api/devices/{id:int}", async (int id) =>
{
    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new SqlCommand("DELETE FROM Devices WHERE Id = @Id", conn);
    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.NoContent() : Results.NotFound(new { message = "Device not found." });
});

app.MapGet("/api/employees", async () =>
{
    var list = new List<EmployeeSummaryDto>();
    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new SqlCommand("SELECT e.Id, p.FirstName, p.LastName FROM Employees e JOIN Persons p ON e.PersonId = p.Id", conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        list.Add(new EmployeeSummaryDto
        {
            Id = reader.GetInt32(0),
            FullName = $"{reader.GetString(1)} {reader.GetString(2)}"
        });
    }
    return Results.Ok(list);
});

app.MapGet("/api/employees/{id:int}", async (int id) =>
{
    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();

    var query = @"
SELECT p.PassportNumber, p.FirstName, p.MiddleName, p.LastName, p.PhoneNumber, p.Email,
       e.Salary, pos.Id AS PositionId, pos.Name AS PositionName, e.HireDate
FROM Employees e
JOIN Persons p ON e.PersonId = p.Id
JOIN Positions pos ON e.PositionId = pos.Id
WHERE e.Id = @Id";
    await using var cmd = new SqlCommand(query, conn);
    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.NotFound(new { message = "Employee not found." });

    var dto = new EmployeeDetailDto
    {
        PassportNumber = reader.GetString(0),
        FirstName = reader.GetString(1),
        MiddleName = reader.IsDBNull(2) ? null : reader.GetString(2),
        LastName = reader.GetString(3),
        PhoneNumber = reader.GetString(4),
        Email = reader.GetString(5),
        Salary = reader.GetDecimal(6),
        Position = new PositionDto { Id = reader.GetInt32(7), Name = reader.GetString(8) },
        HireDate = reader.GetDateTime(9)
    };
    return Results.Ok(dto);
});

app.Run();