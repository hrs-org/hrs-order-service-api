using System.ComponentModel.DataAnnotations;

namespace HRS.API.Contracts.DTOs.Maintenance;

public class ItemMaintenanceRequestDto
{
    [Required] public int Id { get; set; }
    [Required] public int QuantityFixed { get; set; }
    public string? Remarks { get; set; }
}
