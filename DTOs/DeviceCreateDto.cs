using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DTOs;

public class DeviceCreateDto
{
    [Required]
    public string DeviceTypeName { get; set; } = null!;
    public bool IsEnabled { get; set; }
    [Required]
    public JsonElement AdditionalProperties { get; set; }
}