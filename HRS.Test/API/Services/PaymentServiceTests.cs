using FluentAssertions;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using NSubstitute;
using Stripe.Checkout;

namespace HRS.Test.API.Services;

public class PaymentServiceTests
{
    private readonly IAppConfiguration _appConfiguration;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRentalOrderService _rentalOrderService;
    private readonly PaymentService _service;
    private readonly SessionService _sessionService;
    private readonly IUserContextService _userContextService;

    public PaymentServiceTests()
    {
        _userContextService = Substitute.For<IUserContextService>();
        _appConfiguration = Substitute.For<IAppConfiguration>();
        _rentalOrderService = Substitute.For<IRentalOrderService>();
        _paymentRepository = Substitute.For<IPaymentRepository>();
        _sessionService = Substitute.For<SessionService>();
        _service = new PaymentService(_userContextService, _appConfiguration, _rentalOrderService, _paymentRepository, _sessionService);
    }

    [Fact]
    public async Task CreatePayments_CreatesStripeSessionAndAssignsSessionId()
    {
        // Arrange
        _appConfiguration.StripeApiKey.Returns("sk_test");
        _appConfiguration.PaymentReturnPath.Returns("https://return.url");
        var user = new User { Id = 1, Email = "test@example.com" };
        _userContextService.GetUserAsync().Returns(user);
        var session = new Session { Id = "sess_123", PaymentStatus = "unpaid", Status = "open" };
        _sessionService.CreateAsync(Arg.Any<SessionCreateOptions>()).Returns(session);

        // Act
        var result = await _service.CreatePayments(42, 99.99);

        // Assert
        result.Should().Be(session);
        await _rentalOrderService.Received(1).AssignStripeSessionIdAsync(42, "sess_123");
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenClientSecretIsNullOrEmpty_ThrowsArgumentException()
    {
        _appConfiguration.StripeApiKey.Returns("sk_test");
        await Assert.ThrowsAsync<ArgumentException>(() => _service.VerifyPaymentAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.VerifyPaymentAsync(""));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenClientSecretFormatInvalid_ThrowsArgumentException()
    {
        _appConfiguration.StripeApiKey.Returns("sk_test");
        await Assert.ThrowsAsync<ArgumentException>(() => _service.VerifyPaymentAsync("invalid_secret"));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenSessionNotFound_ThrowsInvalidOperationException()
    {
        _appConfiguration.StripeApiKey.Returns("sk_test");
        _sessionService.GetAsync(Arg.Any<string>()).Returns((Session)null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyPaymentAsync("cs_test_abc_def_ghi"));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenPaymentNotCompleted_ThrowsInvalidOperationException()
    {
        _appConfiguration.StripeApiKey.Returns("sk_test");
        var session = new Session { Id = "sess_123", PaymentStatus = "unpaid", Status = "open" };
        _sessionService.GetAsync(Arg.Any<string>()).Returns(session);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyPaymentAsync("cs_test_abc_def_ghi"));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenPaymentCompleted_ApprovesPayment()
    {
        _appConfiguration.StripeApiKey.Returns("sk_test");
        var session = new Session { Id = "sess_123", PaymentStatus = "paid", Status = "complete", AmountTotal = 12345 };
        _sessionService.GetAsync(Arg.Any<string>()).Returns(session);

        await _service.VerifyPaymentAsync("cs_test_abc_def_ghi");
        await _rentalOrderService.Received(1).ApprovePaymentAsync("cs_test_abc", 12345);
    }

    [Fact]
    public async Task RecordPayment_AddsPayment_WhenNoExistingPayment()
    {
        // Arrange
        _paymentRepository.GetByRentalOrderIdAsync(Arg.Any<int>()).Returns(null as Payment);
        var user = new User { Id = 1 };
        _userContextService.GetUserAsync().Returns(user);

        // Act
        await _service.RecordPayment(42, 10000, "sess_123", PaymentType.Stripe);

        // Assert
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p =>
            p.RentalOrderId == 42 &&
            p.StripeSessionId == "sess_123" &&
            p.Amount == 100 &&
            p.PaymentType == PaymentType.Stripe &&
            p.Status == PaymentStatus.Completed &&
            p.CreatedBy == user
        ));
    }

    [Fact]
    public async Task RecordPayment_DoesNothing_WhenPaymentAlreadyExists()
    {
        // Arrange
        var existing = new Payment { RentalOrderId = 42, PaymentType = PaymentType.Stripe };
        _paymentRepository.GetByRentalOrderIdAsync(Arg.Any<int>()).Returns(existing);

        // Act
        await _service.RecordPayment(42, 10000, "sess_123", PaymentType.Stripe);

        // Assert
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>());
    }
}
