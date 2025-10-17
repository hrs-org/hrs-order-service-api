using FluentAssertions;
using HRS.API.Contracts.DTOs.Payment;
using HRS.API.Controllers;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Stripe.Checkout;

namespace HRS.Test.API.Controllers;

public class PaymentControllerTests
{
    private readonly PaymentController _controller;
    private readonly IPaymentService _service;

    public PaymentControllerTests()
    {
        _service = Substitute.For<IPaymentService>();
        _controller = new PaymentController(_service);
    }

    [Fact]
    public async Task GetAvailability_ReturnsOkWithSession()
    {
        // Arrange
        var request = new PaymentRequestDto { OrderId = 1, Amount = 100 };
        var session = new Session { Id = "sess_123" };
        _service.CreatePayments(request.OrderId, request.Amount).Returns(session);

        // Act
        var result = await _controller.GetAvailability(request);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((Session)apiResponse?.Data!).Should().BeEquivalentTo(session);
    }

    [Fact]
    public async Task VerifyPayment_ReturnsOkWithApiResponse()
    {
        // Arrange
        var request = new VerifyPaymentRequestDto { SecretKey = "sk_test" };
        _service.VerifyPaymentAsync(request.SecretKey).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.VerifyPayment(request);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((object)apiResponse?.Data!).Should().BeNull();
        ((string)apiResponse?.Message!).Should().Be("Payment verified successfully");
        await _service.Received(1).VerifyPaymentAsync(request.SecretKey);
    }
}
