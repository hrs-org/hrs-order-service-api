namespace HRS.API.Contracts.DTOs.User;

public class RegisterEmployeeDetailDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
}
public class UpdateEmployeeDto : RegisterEmployeeDetailDto
{
    public required int Id { get; set; }
}
