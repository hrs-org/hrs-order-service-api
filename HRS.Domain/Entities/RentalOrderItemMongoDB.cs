using HRS.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace HRS.Domain.Entities;

public class RentalOrderItemMongoDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public int RentalOrderId { get; set; }
    public RentalStatus RentalOrderStatus { get; set; } = RentalStatus.Pending;
    public DateTime RentalOrderStartDate { get; set; }
    public DateTime RentalOrderEndDate { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public int ItemId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public int? ItemRateId { get; set; }

    public string ItemNameSnapshot { get; set; } = null!;
    public decimal DailyRateSnapshot { get; set; }
    public int Quantity { get; set; }

    public int GoodQty { get; set; }
    public int RepairQty { get; set; }
    public int DamagedQty { get; set; }
    public int LostQty { get; set; }

    [BsonIgnore]
    public bool HasIssues => RepairQty + DamagedQty + LostQty > 0;

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
