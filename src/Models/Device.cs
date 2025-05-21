using System.ComponentModel.DataAnnotations;

namespace Models;

public class Device
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = null!;

    [Required]
    public string AdditionalProperties { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public int DeviceTypeId { get; set; }
    public DeviceType DeviceType { get; set; } = null!;

    public ICollection<DeviceEmployee> DeviceEmployees { get; set; } = new List<DeviceEmployee>();
}