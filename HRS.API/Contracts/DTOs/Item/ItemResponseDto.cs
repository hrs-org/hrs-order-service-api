namespace HRS.API.Contracts.DTOs.Item;

public class ItemResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public ICollection<ItemRateResponseDto>? Rates { get; set; }
    public ICollection<ItemResponseDto>? Children { get; set; }
    public bool HasChildren => Children?.Count > 0;
}

public class ItemRateResponseDto
{
    public int Id { get; set; }
    public int MinDays { get; set; }
    public decimal DailyRate { get; set; }
    public bool IsActive { get; set; }
}
