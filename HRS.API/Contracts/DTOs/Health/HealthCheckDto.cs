namespace HRS.API.Contracts.DTOs.Health;

public class HealthCheckDto
{
    public required string Status { get; set; }
    public required DateTime Timestamp { get; set; }
    public required string Environment { get; set; }
}
