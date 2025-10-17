using HRS.Domain.Enums;

namespace HRS.API.Contracts.DTOs.RentalOrder;

public class RentalOrderItemDto
{
    public int Id { get; set; }
    public int? ItemId { get; set; }
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public decimal DailyRateSnapshot { get; set; }
    public int Quantity { get; set; }
}

public class RentalOrderPackageDto
{
    public int Id { get; set; }
    public int? PackageId { get; set; }
    public string PackageNameSnapshot { get; set; } = string.Empty;
    public decimal DailyRateSnapshot { get; set; }
    public int Quantity { get; set; }

    public ICollection<RentalOrderPackageItemDto> Items { get; set; } = [];
}

public class RentalOrderPackageItemDto
{
    public int Id { get; set; }
    public int? ItemId { get; set; }
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public int QuantityPerPackageSnapshot { get; set; }
}

public class RentalOrderResponseDto
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public string? GuestEmail { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public decimal TotalAmount { get; set; }
    public required string Status { get; set; }
    public required string Channel { get; set; }
    public required string PaymentType { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<RentalOrderItemDto> Items { get; set; } = [];
    public ICollection<RentalOrderPackageDto> Packages { get; set; } = [];
}

public class RentalOrderListDto
{
    public int Id { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public decimal TotalAmount { get; set; }
    public RentalStatus Status { get; set; }
    public OrderChannel Channel { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
