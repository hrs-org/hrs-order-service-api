using FluentAssertions;
using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.API.Controllers;
using HRS.API.Services.Interfaces;
using HRS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace HRS.Test.API.Controllers;

public class RentalOrderControllerTests
{
    private readonly RentalOrderController _controller;
    private readonly IRentalOrderService _service;

    public RentalOrderControllerTests()
    {
        _service = Substitute.For<IRentalOrderService>();
        _controller = new RentalOrderController(_service);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithOrder()
    {
        var order = new RentalOrderResponseDto
        {
            Id = 1,
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _service.GetAsync(1).Returns(order);
        var result = await _controller.GetById(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((RentalOrderResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(order);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var orders = new List<RentalOrderListDto> { new() { Id = 1 }, new() { Id = 2 } };
        _service.GetAllAsync().Returns(orders);
        var result = await _controller.GetAll();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((IEnumerable<RentalOrderListDto>)apiResponse?.Data!).Should().BeEquivalentTo(orders);
    }

    [Fact]
    public async Task GetAllBookings_ReturnsOkWithList()
    {
        var bookings = new List<RentalOrderResponseDto>
        {
            new()
            {
                Id = 1,
                Status = null!,
                Channel = null!,
                PaymentType = null!
            }
        };
        _service.GetByStasusesAsync(Arg.Any<RentalStatus[]>()).Returns(bookings);
        var result = await _controller.GetAllBookings();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((IEnumerable<RentalOrderResponseDto>)apiResponse?.Data!).Should().BeEquivalentTo(bookings);
    }

    [Fact]
    public async Task GetAllRents_ReturnsOkWithList()
    {
        var rents = new List<RentalOrderResponseDto>
        {
            new()
            {
                Id = 2,
                Status = null!,
                Channel = null!,
                PaymentType = null!
            }
        };
        _service.GetByStasusesAsync(Arg.Any<RentalStatus[]>()).Returns(rents);
        var result = await _controller.GetAllRents();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((IEnumerable<RentalOrderResponseDto>)apiResponse?.Data!).Should().BeEquivalentTo(rents);
    }

    [Fact]
    public async Task Create_ReturnsOkWithApiResponse()
    {
        var dto = new CreateRentalOrderRequestDto { GuestName = "John" };
        var response = new RentalOrderResponseDto
        {
            Id = 1,
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _service.CreateAsync(dto).Returns(response);
        var result = await _controller.Create(dto);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((RentalOrderResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(response);
        ((string)apiResponse.Message!).Should().Be("Order created successfully");
    }

    [Fact]
    public async Task ApprovePayment_ReturnsOkWithApiResponse()
    {
        var response = new RentalOrderResponseDto
        {
            Id = 1,
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _service.ApproveAsync(1).Returns(response);
        var result = await _controller.ApprovePayment(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((RentalOrderResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(response);
        ((string)apiResponse.Message!).Should().Be("Order approved successfully");
    }

    [Fact]
    public async Task Approve_ReturnsOkWithApiResponse()
    {
        var response = new RentalOrderResponseDto
        {
            Id = 1,
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _service.ApproveAsync(1).Returns(response);
        var result = await _controller.Approve(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((RentalOrderResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(response);
        ((string)apiResponse.Message!).Should().Be("Order approved successfully");
    }

    [Fact]
    public async Task CancelOrder_ReturnsOkWithApiResponse()
    {
        var response = new RentalOrderResponseDto
        {
            Id = 1,
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _service.ApproveAsync(1).Returns(response);
        var result = await _controller.CancelOrder(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((RentalOrderResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(response);
        ((string)apiResponse.Message!).Should().Be("Order approved successfully");
    }

    [Fact]
    public async Task MarkAsRented_ReturnsOkWithApiResponse()
    {
        var response = new RentalOrderResponseDto
        {
            Id = 1,
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _service.MarkAsRentedAsync(1).Returns(response);
        var result = await _controller.MarkAsRented(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((RentalOrderResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(response);
        ((string)apiResponse.Message!).Should().Be("Order marked as rented successfully");
    }

    [Fact]
    public async Task Return_ReturnsOkWithApiResponse()
    {
        var dto = new ReturnRentalOrderRequestDto();
        var response = new RentalOrderResponseDto
        {
            Id = 1,
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _service.ReturnAsync(1, dto).Returns(response);
        var result = await _controller.Return(1, dto);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((RentalOrderResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(response);
        ((string)apiResponse.Message!).Should().Be("Order returned successfully");
    }

    [Fact]
    public async Task Close_ReturnsOkWithApiResponse()
    {
        var response = new RentalOrderResponseDto
        {
            Id = 1,
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _service.CloseAsync(1).Returns(response);
        var result = await _controller.Close(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((RentalOrderResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(response);
        ((string)apiResponse.Message!).Should().Be("Order closed successfully");
    }
}
