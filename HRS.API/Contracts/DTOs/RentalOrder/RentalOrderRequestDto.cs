using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HRS.Domain.Enums;
using HRS.Shared.Core.Dtos;

namespace HRS.API.Contracts.DTOs.RentalOrder;

public class RentalOrderItemRequestDto
{
    [Required] public string ItemId { get; set; } = string.Empty;
    [Required][Range(1, int.MaxValue)] public int Quantity { get; set; }
}

public class RentalOrderPackageItemRequestDto
{
    [Required] public string PackageItemId { get; set; } = string.Empty;
    public string? SelectedItemId { get; set; }
}

public class RentalOrderPackageRequestDto
{
    [Required] public string PackageId { get; set; } = string.Empty;
    [Required][Range(1, int.MaxValue)] public int Quantity { get; set; }
    public ICollection<RentalOrderPackageItemRequestDto>? SelectedItems { get; set; }
}

public class CreateRentalOrderRequestDto
{
    public int? CustomerId { get; set; }
    [MaxLength(150)] public string? GuestName { get; set; }
    [MaxLength(50)] public string? GuestPhone { get; set; }
    [MaxLength(150)] public string? GuestEmail { get; set; }
    [MaxLength(100)] public string StoreId { get; set; } = string.Empty;

    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderChannel Channel { get; set; } = OrderChannel.Online;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderPaymentType PaymentType { get; set; } = OrderPaymentType.Other;

    public ICollection<RentalOrderItemRequestDto>? Items { get; set; }

    public ICollection<RentalOrderPackageRequestDto>? Packages { get; set; }
}

public class ReturnRentalOrderRequestDto
{
    public ICollection<ReturnItemConditionDto>? Items { get; set; }
    public ICollection<ReturnPackageConditionDto>? Packages { get; set; }

    public string? Remarks { get; set; }
}

public class ReturnItemConditionDto
{
    public string RentalOrderItemId { get; set; } = string.Empty;

    public int GoodQty { get; set; }
    public int RepairQty { get; set; }
    public int DamagedQty { get; set; }
    public int LostQty { get; set; }
}

public class ReturnPackageConditionDto
{
    public string RentalOrderPackageId { get; set; } = string.Empty;
    public ICollection<ReturnPackageItemConditionDto> PackageItems { get; set; } = [];
}

public class ReturnPackageItemConditionDto
{
    public string RentalOrderPackageItemId { get; set; } = string.Empty;

    public int GoodQty { get; set; }
    public int RepairQty { get; set; }
    public int DamagedQty { get; set; }
    public int LostQty { get; set; }
}
public class AssignStripeSessionRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}
public class ApprovePaymentRequest
{
    public string SessionId { get; set; } = string.Empty;
    public long? Amount { get; set; }
}

public class UpdateItemRequestDto
{
    public string StoreId { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public ICollection<ItemRateResponseDto>? Rates { get; set; }
    public ICollection<ItemResponseDto>? Children { get; set; }
    public bool HasChildren => Children?.Count > 0;

};
