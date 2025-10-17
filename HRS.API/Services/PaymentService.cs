using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace HRS.API.Services;

public class PaymentService : IPaymentService
{
    private readonly IAppConfiguration _appConfiguration;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRentalOrderService _rentalOrderService;
    private readonly SessionService _sessionService;
    private readonly IUserContextService _userContextService;

    public PaymentService(
        IUserContextService userContextService,
        IAppConfiguration appConfiguration,
        IRentalOrderService rentalOrderService,
        IPaymentRepository paymentRepository,
        SessionService? sessionService = null)
    {
        _userContextService = userContextService;
        _appConfiguration = appConfiguration;
        _rentalOrderService = rentalOrderService;
        _paymentRepository = paymentRepository;

        // Use the provided service for testing, fallback to real service for production
        _sessionService = sessionService ?? new SessionService();
    }

    public async Task<Session> CreatePayments(int orderId, double amount)
    {
        StripeConfiguration.ApiKey = _appConfiguration.StripeApiKey;
        var user = await _userContextService.GetUserAsync();
        var id = user.Id.ToString();
        var orderName = "OrderID:" + orderId + "-User:" + id;

        var options = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(amount * 100),
                        Currency = "SGD",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = orderName
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            CustomerEmail = user.Email,
            UiMode = "embedded",
            ReturnUrl = _appConfiguration.PaymentReturnPath,
            ExpiresAt = DateTime.UtcNow.AddMinutes(35)
        };

        var session = await _sessionService.CreateAsync(options);

        await _rentalOrderService.AssignStripeSessionIdAsync(orderId, session.Id);

        return session;
    }

    public async Task VerifyPaymentAsync(string clientSecret)
    {
        StripeConfiguration.ApiKey = _appConfiguration.StripeApiKey;
        if (string.IsNullOrEmpty(clientSecret))
            throw new ArgumentException("clientSecret is required");

        var parts = clientSecret.Split('_');
        if (parts.Length < 4)
            throw new ArgumentException("Invalid clientSecret format");

        var sessionId = string.Join("_", parts[0], parts[1], parts[2]);

        var session = await _sessionService.GetAsync(sessionId) ?? throw new InvalidOperationException("Invalid Stripe session.");

        if (session.PaymentStatus != "paid" || session.Status != "complete")
            throw new InvalidOperationException("Payment not completed.");

        if (session.Status == "complete")
            await _rentalOrderService.ApprovePaymentAsync(sessionId, session.AmountTotal);
    }

    public async Task RecordPayment(int orderId, long? amount, string? sessionId, PaymentType paymentType)
    {
        var existingPayments = await _paymentRepository
            .GetByRentalOrderIdAsync(orderId);

        if (existingPayments != null)
            return;

        var user = await _userContextService.GetUserAsync();

        var payment = new Payment
        {
            RentalOrderId = orderId,
            StripeSessionId = sessionId,
            Amount = (decimal)(amount ?? 0) / 100,
            PaymentType = paymentType,
            PaymentDate = DateTime.UtcNow,
            Status = PaymentStatus.Completed,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        };
        await _paymentRepository.AddAsync(payment);
    }
}
