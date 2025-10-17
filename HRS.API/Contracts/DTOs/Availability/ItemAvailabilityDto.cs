namespace HRS.API.Contracts.DTOs.Availability;

public class ItemAvailabilityDto
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}
