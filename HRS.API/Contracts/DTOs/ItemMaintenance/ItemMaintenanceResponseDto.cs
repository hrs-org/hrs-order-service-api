namespace HRS.API.Contracts.DTOs.Maintenance;

public class ItemMaintenanceResponseDto
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public int? RentalOrderId { get; set; }
    public string? Type { get; set; }
    public int Quantity { get; set; }
    public int? QuantityFixed { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
