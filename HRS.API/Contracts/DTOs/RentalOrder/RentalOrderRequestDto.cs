using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HRS.Domain.Enums;

namespace HRS.API.Contracts.DTOs.RentalOrder;

public class RentalOrderItemRequestDto
{
    [Required] public int ItemId { get; set; }
    [Required][Range(1, int.MaxValue)] public int Quantity { get; set; }
}

public class RentalOrderPackageItemRequestDto
{
    [Required] public int PackageItemId { get; set; }
    public int? SelectedItemId { get; set; }
}

public class RentalOrderPackageRequestDto
{
    [Required] public int PackageId { get; set; }
    [Required][Range(1, int.MaxValue)] public int Quantity { get; set; }
    public ICollection<RentalOrderPackageItemRequestDto>? SelectedItems { get; set; }
}

public class CreateRentalOrderRequestDto
{
    public int? CustomerId { get; set; }
    [MaxLength(150)] public string? GuestName { get; set; }
    [MaxLength(50)] public string? GuestPhone { get; set; }
    [MaxLength(150)] public string? GuestEmail { get; set; }

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
    public int RentalOrderItemId { get; set; }

    public int GoodQty { get; set; }
    public int RepairQty { get; set; }
    public int DamagedQty { get; set; }
    public int LostQty { get; set; }
}

public class ReturnPackageConditionDto
{
    public int RentalOrderPackageId { get; set; }
    public ICollection<ReturnPackageItemConditionDto> PackageItems { get; set; } = [];
}

public class ReturnPackageItemConditionDto
{
    public int RentalOrderPackageItemId { get; set; }

    public int GoodQty { get; set; }
    public int RepairQty { get; set; }
    public int DamagedQty { get; set; }
    public int LostQty { get; set; }
}
