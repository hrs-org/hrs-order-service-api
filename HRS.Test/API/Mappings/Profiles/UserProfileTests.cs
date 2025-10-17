using AutoMapper;
using FluentAssertions;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Mappings.Profiles;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HRS.Test.API.Mappings.Profiles;

public class UserProfileTests
{
    private readonly IMapper _mapper;

    public UserProfileTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserProfile>();
        }, loggerFactory);


        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Should_Map_User_To_UserDto()
    {
        var user = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@mail.com",
            Role = UserRole.Admin,
            PasswordHash = "hash"
        };

        var dto = _mapper.Map<UserDto>(user);

        dto.Id.Should().Be(1);
        dto.FirstName.Should().Be("John");
        dto.LastName.Should().Be("Doe");
        dto.Email.Should().Be("john.doe@mail.com");
        dto.Role.Should().Be("Admin");
    }

    [Fact]
    public void Should_Map_RegisterDto_To_User_IgnoringPasswordHash()
    {
        var registerDto = new RegisterDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@mail.com",
            Password = "password123"
        };

        var user = _mapper.Map<User>(registerDto);

        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        user.Email.Should().Be("jane.smith@mail.com");
        user.PasswordHash.Should().BeNull(); // PasswordHash ถูก ignore
    }

    [Fact]
    public void Should_Map_RegisterEmployeeDetailDto_To_User_WithRoleEnum()
    {
        var dto = new RegisterEmployeeDetailDto
        {
            FirstName = "Alice",
            LastName = "Brown",
            Email = "alice@mail.com",
            Role = "Employee"
        };

        var user = _mapper.Map<User>(dto);

        user.FirstName.Should().Be("Alice");
        user.LastName.Should().Be("Brown");
        user.Email.Should().Be("alice@mail.com");
        user.Role.Should().Be(UserRole.Employee); // Role แปลงจาก string เป็น enum
        user.PasswordHash.Should().BeNull(); // PasswordHash ถูก ignore
    }

    [Fact]
    public void Should_Map_User_To_RegisterEmployeeDetailDto()
    {
        var user = new User
        {
            FirstName = "Bob",
            LastName = "Marley",
            Email = "bob@mail.com",
            Role = UserRole.Manager
        };

        var dto = _mapper.Map<RegisterEmployeeDetailDto>(user);

        dto.FirstName.Should().Be("Bob");
        dto.LastName.Should().Be("Marley");
        dto.Email.Should().Be("bob@mail.com");
        dto.Role.Should().Be("Manager"); // Role แปลงจาก enum เป็น string
    }
}

