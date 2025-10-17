using System.Formats.Asn1;
using FluentValidation;
using HRS.API.Contracts.DTOs.User;
using HRS.Domain.Interfaces;
namespace HRS.API.Validators.User;


public class RegisterEmployeeDetailDtoValidators : AbstractValidator<RegisterEmployeeDetailDto>
{
    public RegisterEmployeeDetailDtoValidators(IUserRepository userRepository)
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, cancellation) =>
            {
                return await userRepository.IsEmailUniqueAsync(email);
            })
            .WithMessage("Email is already in use.");
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Role).NotEmpty()
            .Must(r => r == "Employee" || r == "Admin" || r == "Manager")
            .WithMessage("Role must be Employee or Admin or Manager");



    }
}

public class UpdateEmployeeDtoValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeDtoValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.Id)
            .NotNull().WithMessage("User Id is required")
            .GreaterThan(0).WithMessage("User Id must be greater than 0");
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (dto, email, cancellation) =>
            {
                var existingUser = await userRepository.GetByIdAsync(dto.Id);
                if (existingUser == null) return false;
                if (existingUser.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                    return true;
                return await userRepository.IsEmailUniqueAsync(email);
            })
            .WithMessage("Email is already in use. or Wrong Email format");
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Role).NotEmpty()
            .Must(r => r == "Employee" || r == "Admin" || r == "Manager")
            .WithMessage("Role must be Employee or Admin or Manager");

    }
}
