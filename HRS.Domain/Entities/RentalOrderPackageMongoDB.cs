using HRS.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HRS.Domain.Entities;
public class RentalOrderPackageMongoDB
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
    public int? PackageId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public int? PackageRateId { get; set; }

    public string PackageNameSnapshot { get; set; } = null!;
    public decimal DailyRateSnapshot { get; set; }
    public int Quantity { get; set; }

    public Collection<RentalOrderPackageItemMongoDB> Items { get; set; }
            = new Collection<RentalOrderPackageItemMongoDB>();
}

