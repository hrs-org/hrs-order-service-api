using HRS.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.ObjectModel;

namespace HRS.Domain.Entities;

public class RentalOrderMongoDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string StoreId { get; set; } = null!;
    public int? CustomerId { get; set; }
    public string GuestName { get; set; } = null!;
    public string GuestPhone { get; set; } = null!;
    public string GuestEmail { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalAmount { get; set; }
    public RentalStatus Status { get; set; }
    public OrderChannel Channel { get; set; }
    public OrderPaymentType PaymentType { get; set; }
    public string? PaymentId { get; set; }
    public int? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ReturnedById { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public int? ClosedById { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool HasIssues { get; set; }
    public int ItemsGoodCount { get; set; }
    public int ItemsIssueCount { get; set; }
    public string? ReturnRemarks { get; set; }
    public string? StripeSessionId { get; set; }
    public int CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedById { get; set; }
    public DateTime UpdatedAt { get; set; }


    public Collection<Item> RentalOrderItems { get; set; } = new();
    public Collection<Package> RentalOrderPackages { get; set; } = new();
}
public class Item
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string ItemId { get; set; } = null!;
    public string ItemNameSnapshot { get; set; } = null!;
    public decimal DailyRateSnapshot { get; set; }
    public int Quantity { get; set; }
    public int GoodQty { get; set; }
    public int RepairQty { get; set; }
    public int DamagedQty { get; set; }
    public int LostQty { get; set; }
    public bool HasIssues { get; set; }


    public void SetReturnConditions(int good, int repair, int damaged, int lost)
    {
        if (good + repair + damaged + lost != Quantity)
            throw new InvalidOperationException("Condition totals must match quantity.");
        GoodQty = good;
        RepairQty = repair;
        DamagedQty = damaged;
        LostQty = lost;
    }
}

public class Package
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string PackageId { get; set; } = null!;
    public string PackageNameSnapshot { get; set; } = null!;
    public decimal DailyRateSnapshot { get; set; }
    public int Quantity { get; set; }
    public Collection<PackageItem> PackageItems { get; set; } = new();
}
public class PackageItem
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string ItemId { get; set; } = null!;
    public string ItemNameSnapshot { get; set; } = null!;
    public int QuantityPerPackageSnapshot { get; set; }
    public int GoodQty { get; set; }
    public int RepairQty { get; set; }
    public int DamagedQty { get; set; }
    public int LostQty { get; set; }
    public bool HasIssues { get; set; }
}



