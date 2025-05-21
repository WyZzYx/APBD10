using System.Text.Json;

namespace DTOs;

public class DeviceDetailDto
{
    public string DeviceTypeName { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public JsonElement AdditionalProperties { get; set; }
    public CurrentEmployeeDto? CurrentEmployee { get; set; }
}