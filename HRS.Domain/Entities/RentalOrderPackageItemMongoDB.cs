using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace HRS.Domain.Entities;
public class RentalOrderPackageItemMongoDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;


    public int RentalOrderPackageId { get; set; }

    public int ItemId { get; set; }

    public string ItemNameSnapshot { get; set; } = null!;
    public int QuantityPerPackageSnapshot { get; set; }

    public int GoodQty { get; set; }
    public int RepairQty { get; set; }
    public int DamagedQty { get; set; }
    public int LostQty { get; set; }

    [BsonIgnore]
    public bool HasIssues => RepairQty + DamagedQty + LostQty > 0;
}
