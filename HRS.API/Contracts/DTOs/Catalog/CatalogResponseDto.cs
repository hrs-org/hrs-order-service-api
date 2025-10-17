namespace HRS.API.Contracts.DTOs.Catalog;

public class CatalogResponseDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public ICollection<CatalogItemNodeDto> Items { get; set; } = [];
    public ICollection<CatalogPackageDto> Packages { get; set; } = [];
}

public class CatalogItemNodeDto
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public int AvailableQuantity { get; set; }
    public ICollection<CatalogItemNodeDto> Children { get; set; } = [];
}

public class CatalogPackageDto
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public int AvailablePackages { get; set; }
    public ICollection<CatalogPackageItemNodeDto> Items { get; set; } = [];
}

public class CatalogPackageItemNodeDto
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public int AvailableQuantity { get; set; }
    public ICollection<CatalogItemNodeDto> Children { get; set; } = [];
}
