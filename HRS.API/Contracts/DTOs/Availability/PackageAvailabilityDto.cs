namespace HRS.API.Contracts.DTOs.Availability;

public class PackageAvailabilityDto
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public int AvailablePackages { get; set; }

    public ICollection<ItemAvailabilityDto> ItemBreakdown { get; set; } = [];
}
