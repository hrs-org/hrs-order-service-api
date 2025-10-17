namespace HRS.API.Contracts.DTOs.Package;

public class PackageItemRequestDto
{
    public int ItemId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class PackageRateRequestDto
{
    public int MinDays { get; set; }
    public decimal DailyRate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PackageRequestDto
{
    public required string Name { get; set; }
    public string? Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }

    public ICollection<PackageItemRequestDto>? Items { get; set; }
    public ICollection<PackageRateRequestDto>? Rates { get; set; }
}

public class AddPackageRequestDto : PackageRequestDto
{
}

public class UpdatePackageRequestDto : PackageRequestDto
{
    public int Id { get; set; }
}
