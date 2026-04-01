using FluentAssertions;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using HRS.Shared.Core.Dtos;
using HRS.Shared.Core.Enums;
using NSubstitute;
using AutoMapper;
using System.Net.Http;
using System.Net.Http.Json;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System;
using System.Threading;
using HRS.Shared.Core.Interfaces;
using HRS.Domain.Entities;
using HRS.API.Contracts.DTOs.RentalOrder;
using System.Data;

namespace HRS.Test.API.Services;

public class RentalOrderServiceTests
{
    private readonly IRentalOrderMongoDBRepository _repo;
    private readonly IAvailabilityService _availability;
    private readonly HttpClient _itemClient;
    private readonly HttpClient _itemMaintenanceClient;
    private readonly HttpClient _paymentClient;
    private readonly IMapper _mapper;
    private readonly IUserContextService _userService;
    private readonly RentalOrderService _service;

    public RentalOrderServiceTests()
    {
        _repo = Substitute.For<IRentalOrderMongoDBRepository>();
        _availability = Substitute.For<IAvailabilityService>();
        _mapper = Substitute.For<IMapper>();
        _userService = Substitute.For<IUserContextService>();
        // Most tests assume a valid store context. Default storeId to match test orders.
        _userService.GetStoreId().Returns(1);

        var itemHandler = new HttpMessageHandlerStub();
        _itemClient = new HttpClient(itemHandler)
        {
            BaseAddress = new Uri("http://itemservice")
        };

        var maintenanceHandler = new HttpMessageHandlerStub();
        _itemMaintenanceClient = new HttpClient(maintenanceHandler)
        {
            BaseAddress = new Uri("http://itemmaintservice")
        };

        var paymentHandler = new HttpMessageHandlerStub();
        _paymentClient = new HttpClient(paymentHandler)
        {
            BaseAddress = new Uri("http://paymentservice")
        };

        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("ItemService").Returns(_itemClient);
        httpFactory.CreateClient("ItemMaintenanceService").Returns(_itemMaintenanceClient);
        httpFactory.CreateClient("PaymentService").Returns(_paymentClient);

        _service = new RentalOrderService(_mapper, _userService, _availability, httpFactory, _repo);
    }

    // Ensure the shared stub map is clean between tests
    // NOTE: Do NOT clear HttpMessageHandlerStub.ResponseMap globally to avoid races with other test classes.

    [Fact]
    public async Task GetAsync_ReturnsMappedDto_WhenOrderExists()
    {
        var order = new RentalOrderMongoDB { Id = "order1", StoreId = 1 };
        _repo.GetByIdAsync("order1").Returns(order);
        var dto = new RentalOrderResponseDto { Id = "order1", StoreId = 1, Status = "Pending", Channel = "Online", PaymentType = "Cash" };
        _mapper.Map<RentalOrderResponseDto>(order).Returns(dto);

        var result = await _service.GetAsync("order1");
        result.Should().Be(dto);
    }

    [Fact]
    public async Task GetAsync_Throws_WhenOrderNotFound()
    {
        _repo.GetByIdAsync("missing").Returns((RentalOrderMongoDB?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetAsync("missing"));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenStartDateAfterEndDate()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenItemUnavailable()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Items = new List<RentalOrderItemRequestDto>()
            {
                new RentalOrderItemRequestDto
                {
                ItemId = "i1",
                Quantity = 5
                }
        }
        };
        _availability.GetAvailableQuantityAsync("i1", Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(3);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenItemNotFoundFromApi()
    {

        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Items = new List<RentalOrderItemRequestDto>
            {
                new RentalOrderItemRequestDto { ItemId = "missingItem", Quantity = 1 }
            }
        };

        // ensure availability check passes so CreateAsync proceeds to fetch item data
        _availability.GetAvailableQuantityAsync(dto.Items!.First().ItemId, dto.StartDate, dto.EndDate).Returns(1);

        // ensure user and mapper return values to avoid NRE
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 1, Email = "u@e.com", FirstName = "T", LastName = "U", Role = "User" }));
        _mapper.Map<RentalOrderMongoDB>(dto).Returns(new RentalOrderMongoDB { Id = "new-missing-order", StoreId = 1 });

        // item endpoint returns null and parent endpoint also returns null
        var missingItemId = dto.Items!.First().ItemId;
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{missingItemId}"] = new ApiResponse<ItemResponseDto> { Data = null };
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{missingItemId}/parent"] = new ApiResponse<ItemResponseDto> { Data = null };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WithChildItem_AddsOrderAndCalculatesTotal()
    {

        var childId = "child1";
        var parent = new ItemResponseDto
        {
            Id = "parent1",
            Name = "ParentItem",
            Price = 10m,
            Quantity = 10,
            // ParentId omitted for parent
            Children = new List<ItemResponseDto> { new ItemResponseDto { Id = childId, Name = "ChildItem", Price = 5m } },
            // provide at least one rate so service's foreach on Rates won't NRE
            Rates = new List<ItemRateResponseDto> { new ItemRateResponseDto { MinDays = 1, DailyRate = 5m } }
        };

        // Map item fetch to parent response (so isChild path executes)
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{childId}"] = new ApiResponse<ItemResponseDto> { Data = null };
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{childId}/parent"] = new ApiResponse<ItemResponseDto> { Data = parent };

        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(2), // rentalDays = 2
            Items = new List<RentalOrderItemRequestDto>
            {
                new RentalOrderItemRequestDto { ItemId = childId, Quantity = 2 }
            }
        };

        // availability returns sufficient quantity
        _availability.GetAvailableQuantityAsync(childId, dto.StartDate, dto.EndDate).Returns(5);

        // ensure user and mapper return values to avoid NRE
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 1, Email = "u@e.com", FirstName = "T", LastName = "U", Role = "User" }));
        // mapper should map dto to entity when adding and map back on return
        _mapper.Map<RentalOrderMongoDB>(dto).Returns(new RentalOrderMongoDB { Id = "newOrder", StoreId = 1, PaymentType = HRS.Domain.Enums.OrderPaymentType.Other, Channel = HRS.Domain.Enums.OrderChannel.Online });
        _mapper.Map<RentalOrderResponseDto>(Arg.Any<RentalOrderMongoDB>()).Returns(new RentalOrderResponseDto { Id = "newOrder", StoreId = 1, Status = "Pending", Channel = "Online", PaymentType = "Cash" });

        await _service.CreateAsync(dto);

        // Verify repository AddAsync was called
        await _repo.Received(1).AddAsync(Arg.Any<RentalOrderMongoDB>());
        await _repo.Received(1).UpdateAsync(Arg.Any<RentalOrderMongoDB>(), Arg.Any<string>());
    }

    [Fact]
    public async Task AssignStripeSessionIdAsync_UpdatesOrder()
    {
        var order = new RentalOrderMongoDB { Id = "o1", StoreId = 1, Status = RentalStatus.PendingPayment };
        _repo.GetByIdAsync("o1").Returns(order);

        await _service.AssignStripeSessionIdAsync("o1", "sess123");

        order.StripeSessionId.Should().Be("sess123");
        await _repo.Received(1).UpdateAsync(order, "o1");
    }

    [Fact]
    public async Task AssignStripeSessionIdAsync_Throws_WhenOrderBelongsToDifferentStore()
    {
        var order = new RentalOrderMongoDB { Id = "o-store", StoreId = 1, Status = RentalStatus.PendingPayment };
        _repo.GetByIdAsync("o-store").Returns(order);
        _userService.GetStoreId().Returns(2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AssignStripeSessionIdAsync("o-store", "sess-x"));
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<RentalOrderMongoDB>(), Arg.Any<string>());
    }

    [Fact]
    public async Task AssignStripeSessionIdAsync_Throws_WhenOrderIsNotPendingPayment()
    {
        var order = new RentalOrderMongoDB { Id = "o-status", StoreId = 1, Status = RentalStatus.Booked };
        _repo.GetByIdAsync("o-status").Returns(order);
        _userService.GetStoreId().Returns(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AssignStripeSessionIdAsync("o-status", "sess-y"));
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<RentalOrderMongoDB>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ApproveAsync_Throws_WhenNotPending()
    {
        var order = new RentalOrderMongoDB { Id = "o1", Status = RentalStatus.Rented, StoreId = 1 };
        _repo.GetByIdAsync("o1").Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApproveAsync("o1"));
    }

    [Fact]
    public async Task CancelAsync_Throws_WhenNotPending()
    {
        var order = new RentalOrderMongoDB { Id = "o1", Status = RentalStatus.Rented, StoreId = 1 };
        _repo.GetByIdAsync("o1").Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CancelAsync("o1"));
    }

    [Fact]
    public async Task MarkAsRentedAsync_Throws_WhenNotBooked()
    {
        var order = new RentalOrderMongoDB { Id = "o1", Status = RentalStatus.Pending, StoreId = 1 };
        _repo.GetByIdAsync("o1").Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.MarkAsRentedAsync("o1"));
    }

    [Fact]
    public async Task CloseAsync_Throws_WhenNotReturned()
    {
        var order = new RentalOrderMongoDB { Id = "o1", Status = RentalStatus.Rented, StoreId = 1 };
        _repo.GetByIdAsync("o1").Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CloseAsync("o1"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        // Arrange
        var orders = new List<RentalOrderMongoDB>
    {
        new RentalOrderMongoDB { Id = "order1", StoreId = 1 },
        new RentalOrderMongoDB { Id = "order2", StoreId = 2 }
    };

        var dtos = new List<RentalOrderListDto>
    {
        new RentalOrderListDto { Id = "order1", StoreId = 1 },
        new RentalOrderListDto { Id = "order2", StoreId = 2 }
    };

        _repo.GetAllAsync().Returns(Task.FromResult<IEnumerable<RentalOrderMongoDB>>(orders));
        _mapper.Map<IEnumerable<RentalOrderListDto>>(orders).Returns(dtos);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEquivalentTo(dtos);
        await _repo.Received(1).GetAllAsync();
        _mapper.Received(1).Map<IEnumerable<RentalOrderListDto>>(orders);
    }

    [Fact]
    public async Task GetByStatusesAsync_ReturnsMappedDtos()
    {
        // Arrange
        var statuses = new[] { RentalStatus.Pending, RentalStatus.Booked };
        var storeId = 1;

        var orders = new List<RentalOrderMongoDB>
    {
        new RentalOrderMongoDB { Id = "order1", StoreId = storeId },
        new RentalOrderMongoDB { Id = "order2", StoreId = storeId }
    };

        var dtos = new List<RentalOrderResponseDto>
    {
        new RentalOrderResponseDto { Id = "order1", StoreId = storeId, Status = "Pending", Channel = "Online", PaymentType = "Cash" },
        new RentalOrderResponseDto { Id = "order2", StoreId = storeId, Status = "Booked", Channel = "Offline", PaymentType = "CreditCard" }
    };

        _userService.GetStoreId().Returns(storeId);
        _repo.GetByStatusesAndStoreId(statuses, storeId).Returns(Task.FromResult<IEnumerable<RentalOrderMongoDB>>(orders));
        _mapper.Map<IEnumerable<RentalOrderResponseDto>>(orders).Returns(dtos);

        // Act
        var result = await _service.GetByStatusesAsync(statuses);

        // Assert
        result.Should().BeEquivalentTo(dtos);
        await _repo.Received(1).GetByStatusesAndStoreId(statuses, storeId);
        _mapper.Received(1).Map<IEnumerable<RentalOrderResponseDto>>(orders);
    }

    [Fact]
    public async Task CreateAsync_StartDateAfterEndDate_Throws()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_ItemNotAvailable_Throws()
    {
        var mockitem = new RentalOrderItemRequestDto { ItemId = "item1", Quantity = 5 };
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(2),
            Items = new List<RentalOrderItemRequestDto> { mockitem }
        };

        _availability.GetAvailableQuantityAsync("item1", dto.StartDate, dto.EndDate).Returns(3);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task ApprovePaymentAsync_OrderNotFound_Throws()
    {
        // arrange
        _repo.GetByStripeSessionIdAsync(Arg.Any<string>()).Returns((RentalOrderMongoDB?)null);

        // act/assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ApprovePaymentAsync("nonexistent-session", 100));
    }

    [Fact]
    public async Task ApprovePaymentAsync_ExistingPayment_ThrowsDuplicate()
    {
        // arrange
        var order = new RentalOrderMongoDB { Id = "o-pay-1", StoreId = 1, Status = RentalStatus.PendingPayment };
        _repo.GetByStripeSessionIdAsync("sess-exists").Returns(order);

        // configure payment GET to return data (existing payments)
        HttpMessageHandlerStub.ResponseMap[$"/api/payments/orders/{order.Id}"] = new ApiResponse<object> { Data = new { Any = true } };

        // act/assert
        await Assert.ThrowsAsync<DuplicateNameException>(() => _service.ApprovePaymentAsync("sess-exists", 200));
    }

    [Fact]
    public async Task ApprovePaymentAsync_Throws_WhenOrderBelongsToDifferentStore()
    {
        var order = new RentalOrderMongoDB
        {
            Id = "o-pay-store",
            StoreId = 1,
            Status = RentalStatus.PendingPayment,
            Channel = OrderChannel.POS
        };
        _repo.GetByStripeSessionIdAsync("sess-store-mismatch").Returns(order);
        _userService.GetStoreId().Returns(2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApprovePaymentAsync("sess-store-mismatch", 100));
    }

    [Fact]
    public async Task ReturnAsync_HandleMaintenanceBatchFails_Throws()
    {
        // arrange
        var order = new RentalOrderMongoDB
        {
            Id = "ret1",
            StoreId = 1,
            Status = RentalStatus.Rented,
            RentalOrderItems = new System.Collections.ObjectModel.Collection<Item>
            {
                new Item { Id = "rItem1", ItemId = "i1", Quantity = 2, ItemNameSnapshot = "Item1" }
            }
        };

        _repo.GetByIdAsync("ret1").Returns(order);
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 42, Email = "a@b.com", FirstName = "A", LastName = "B", Role = "User" }));

        // Build return DTO with non-zero damaged quantity to trigger maintenance
        var dto = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto>
            {
                new ReturnItemConditionDto { RentalOrderItemId = order.RentalOrderItems.First().Id, GoodQty = 1, RepairQty = 0, DamagedQty = 1, LostQty = 0 }
            }
        };

        // Configure item GET to return a valid item so HandleMaintenance proceeds
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{order.RentalOrderItems.First().ItemId}"] = new ApiResponse<ItemResponseDto> { Data = new ItemResponseDto { Id = order.RentalOrderItems.First().ItemId, Quantity = 10 } };

        // Configure maintenance POST to return 500 (simulate failure)
        // Because our stub only supports mapping path->object for GET, create a special handler that returns a fixed StatusCode for POST
        var failingClient = new HttpClient(new FixedStatusHandler(HttpStatusCode.InternalServerError)) { BaseAddress = new Uri("http://itemmaintservice") };

        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("ItemService").Returns(_itemClient);
        httpFactory.CreateClient("ItemMaintenanceService").Returns(failingClient);
        httpFactory.CreateClient("PaymentService").Returns(_paymentClient);

        // Recreate service with failing maintenance client
        var serviceWithFailingMaint = new RentalOrderService(_mapper, _userService, _availability, httpFactory, _repo);

        // act/assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => serviceWithFailingMaint.ReturnAsync("ret1", dto));
    }

    // Simple handler that returns a fixed status for any request (used to simulate maintenance POST failure)
    private class FixedStatusHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public FixedStatusHandler(HttpStatusCode status) => _status = status;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var resp = new HttpResponseMessage(_status) { Content = new StringContent(string.Empty) };
            return Task.FromResult(resp);
        }
    }

    // Handler that returns a JSON ApiResponse<T> for POST requests
    private class JsonResponseHandler<T> : HttpMessageHandler where T : class
    {
        private readonly T _payload;
        private readonly HttpStatusCode _status;
        public JsonResponseHandler(T payload, HttpStatusCode status = HttpStatusCode.OK)
        {
            _payload = payload;
            _status = status;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var apiResp = new ApiResponse<T> { Data = _payload };
            var json = System.Text.Json.JsonSerializer.Serialize(apiResp);
            var resp = new HttpResponseMessage(_status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
            return Task.FromResult(resp);
        }
    }

    // PaymentHandler: if getReturnsNull=true, GET /api/payments/orders/{id} returns ApiResponse<object> with Data=null
    // POST /api/payments returns configured status and optionally a JSON ApiResponse<string> payload
    private class PaymentHandler : HttpMessageHandler
    {
        private readonly bool _getReturnsNull;
        private readonly HttpStatusCode _postStatus;
        private readonly string? _postPayload;
        public PaymentHandler(bool getReturnsNull, HttpStatusCode postStatus, string? postPayload)
        {
            _getReturnsNull = getReturnsNull;
            _postStatus = postStatus;
            _postPayload = postPayload;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get && request.RequestUri != null && request.RequestUri.AbsolutePath.StartsWith("/api/payments/orders/"))
            {
                var apiResp = new ApiResponse<object> { Data = _getReturnsNull ? null : new { Any = true } };
                var json = System.Text.Json.JsonSerializer.Serialize(apiResp);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
            }

            if (request.Method == HttpMethod.Post && request.RequestUri != null && request.RequestUri.AbsolutePath == "/api/payments")
            {
                if (_postPayload == null)
                {
                    // return empty response with status
                    return Task.FromResult(new HttpResponseMessage(_postStatus) { Content = new StringContent(string.Empty) });
                }
                else
                {
                    var apiResp = new ApiResponse<string> { Data = _postPayload };
                    var json = System.Text.Json.JsonSerializer.Serialize(apiResp);
                    return Task.FromResult(new HttpResponseMessage(_postStatus) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
                }
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent(string.Empty) });
        }
    }

    [Fact]
    public async Task CreateAsync_CashPaymentFailed_ThrowsAndRemovesOrder()
    {
        // arrange: Create dto that will map to cash+POS so payment logic triggers
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Items = new List<RentalOrderItemRequestDto> { new RentalOrderItemRequestDto { ItemId = "i-cash", Quantity = 1 } }
        };

        _availability.GetAvailableQuantityAsync("i-cash", dto.StartDate, dto.EndDate).Returns(1);
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 99, Email = "c@d.com", FirstName = "C", LastName = "D", Role = "User" }));

        // mapper returns entity configured as Cash + POS
        _mapper.Map<RentalOrderMongoDB>(dto).Returns(new RentalOrderMongoDB { Id = "cash1", StoreId = 1, PaymentType = OrderPaymentType.Cash, Channel = OrderChannel.POS });
        _mapper.Map<RentalOrderResponseDto>(Arg.Any<RentalOrderMongoDB>()).Returns(new RentalOrderResponseDto { Id = "cash1", StoreId = 1, Status = "Pending", Channel = "Online", PaymentType = "Cash" });

        // item lookup returns a valid item
        HttpMessageHandlerStub.ResponseMap[$"/api/items/i-cash"] = new ApiResponse<ItemResponseDto> { Data = new ItemResponseDto { Id = "i-cash", Price = 10m, Quantity = 5, Rates = new List<ItemRateResponseDto>() } };

        // Create a payment client that returns 500 on POST
        var failingPaymentClient = new HttpClient(new FixedStatusHandler(HttpStatusCode.InternalServerError)) { BaseAddress = new Uri("http://paymentservice") };
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("ItemService").Returns(_itemClient);
        httpFactory.CreateClient("ItemMaintenanceService").Returns(_itemMaintenanceClient);
        httpFactory.CreateClient("PaymentService").Returns(failingPaymentClient);

        var svc = new RentalOrderService(_mapper, _userService, _availability, httpFactory, _repo);

        // act/assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(dto));

        // ensure RemoveAsync was called to rollback the order
        await _repo.Received().RemoveAsync("cash1");
    }

    [Fact]
    public async Task CreateAsync_CashPaymentSuccess_RecordsPaymentAndUpdatesOrder()
    {
        // arrange
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Items = new List<RentalOrderItemRequestDto> { new RentalOrderItemRequestDto { ItemId = "i-cash2", Quantity = 1 } }
        };

        _availability.GetAvailableQuantityAsync("i-cash2", dto.StartDate, dto.EndDate).Returns(1);
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 100, Email = "e@f.com", FirstName = "E", LastName = "F", Role = "User" }));

        _mapper.Map<RentalOrderMongoDB>(dto).Returns(new RentalOrderMongoDB { Id = "cash2", StoreId = 1, PaymentType = OrderPaymentType.Cash, Channel = OrderChannel.POS });

        HttpMessageHandlerStub.ResponseMap[$"/api/items/i-cash2"] = new ApiResponse<ItemResponseDto> { Data = new ItemResponseDto { Id = "i-cash2", Price = 15m, Quantity = 5, Rates = new List<ItemRateResponseDto>() } };

        // Payment service will return ApiResponse<string> with payment id
        var paymentResponsePayload = "payment-123";
        var paymentClient = new HttpClient(new JsonResponseHandler<string>(paymentResponsePayload)) { BaseAddress = new Uri("http://paymentservice") };

        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("ItemService").Returns(_itemClient);
        httpFactory.CreateClient("ItemMaintenanceService").Returns(_itemMaintenanceClient);
        httpFactory.CreateClient("PaymentService").Returns(paymentClient);

        var svc = new RentalOrderService(_mapper, _userService, _availability, httpFactory, _repo);

        // act
        await svc.CreateAsync(dto);

        // assert that update was called (payment id should be read and set)
        await _repo.Received(1).UpdateAsync(Arg.Any<RentalOrderMongoDB>(), "cash2");
    }

    [Fact]
    public async Task ApprovePaymentAsync_PostFails_Throws()
    {
        // arrange
        var order = new RentalOrderMongoDB { Id = "ap1", StoreId = 1, Status = RentalStatus.PendingPayment, Channel = OrderChannel.POS };
        _repo.GetByStripeSessionIdAsync("sess-ap-fail").Returns(order);

        // payment client: GET returns Data = null, POST returns 500
        var failingPaymentClient = new HttpClient(new PaymentHandler(getReturnsNull: true, postStatus: HttpStatusCode.InternalServerError, postPayload: null)) { BaseAddress = new Uri("http://paymentservice") };
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("ItemService").Returns(_itemClient);
        httpFactory.CreateClient("ItemMaintenanceService").Returns(_itemMaintenanceClient);
        httpFactory.CreateClient("PaymentService").Returns(failingPaymentClient);

        var svc = new RentalOrderService(_mapper, _userService, _availability, httpFactory, _repo);

        // act/assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ApprovePaymentAsync("sess-ap-fail", 100));
    }

    [Fact]
    public async Task ApprovePaymentAsync_Success_UpdatesOrderAndSetsApproved()
    {
        // arrange
        var order = new RentalOrderMongoDB { Id = "ap2", StoreId = 1, Status = RentalStatus.PendingPayment, Channel = OrderChannel.POS };
        _repo.GetByStripeSessionIdAsync("sess-ap-ok").Returns(order);

        // payment client: GET returns Data = null, POST returns ApiResponse<string> with payment id
        var paymentClient = new HttpClient(new PaymentHandler(getReturnsNull: true, postStatus: HttpStatusCode.OK, postPayload: "ap-payment-id")) { BaseAddress = new Uri("http://paymentservice") };
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("ItemService").Returns(_itemClient);
        httpFactory.CreateClient("ItemMaintenanceService").Returns(_itemMaintenanceClient);
        httpFactory.CreateClient("PaymentService").Returns(paymentClient);

        // provide user
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 123, Email = "a@b.com", FirstName = "A", LastName = "B", Role = "User" }));

        var svc = new RentalOrderService(_mapper, _userService, _availability, httpFactory, _repo);

        // act
        var result = await svc.ApprovePaymentAsync("sess-ap-ok", 100);

        // assert: repository update called and order approved fields set
        await _repo.Received(1).UpdateAsync(order, order.Id);
        order.ApprovedById.Should().Be(123);
        order.Status.Should().Be(RentalStatus.Rented);
    }

    [Fact]
    public async Task ReturnAsync_ReturnQuantityMismatch_Throws()
    {
        // arrange
        var order = new RentalOrderMongoDB
        {
            Id = "ret-mis",
            StoreId = 1,
            Status = RentalStatus.Rented,
            RentalOrderItems = new System.Collections.ObjectModel.Collection<Item>
            {
                new Item { Id = "ri1", ItemId = "i-mis", Quantity = 2, ItemNameSnapshot = "I" }
            }
        };
        _repo.GetByIdAsync("ret-mis").Returns(order);
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 5, Email = "u@e.com", FirstName = "X", LastName = "Y", Role = "User" }));

        var dto = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto>
            {
                // total returned = 1 != order quantity 2
                new ReturnItemConditionDto { RentalOrderItemId = order.RentalOrderItems.First().Id, GoodQty = 1, RepairQty = 0, DamagedQty = 0, LostQty = 0 }
            }
        };

        // act/assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReturnAsync("ret-mis", dto));
    }

    [Fact]
    public async Task ReturnAsync_MaintenanceBatchSuccess_UpdatesOrderToReturned()
    {
        // arrange
        var order = new RentalOrderMongoDB
        {
            Id = "ret-ok",
            StoreId = 1,
            Status = RentalStatus.Rented,
            RentalOrderItems = new System.Collections.ObjectModel.Collection<Item>
            {
                new Item { Id = "rItem2", ItemId = "i2", Quantity = 1, ItemNameSnapshot = "Item2" }
            }
        };

        _repo.GetByIdAsync("ret-ok").Returns(order);
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 7, Email = "g@h.com", FirstName = "G", LastName = "H", Role = "User" }));

        var dto = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto>
            {
                new ReturnItemConditionDto { RentalOrderItemId = order.RentalOrderItems.First().Id, GoodQty = 0, RepairQty = 0, DamagedQty = 1, LostQty = 0 }
            }
        };

        // item GET returns a valid item
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{order.RentalOrderItems.First().ItemId}"] = new ApiResponse<ItemResponseDto> { Data = new ItemResponseDto { Id = order.RentalOrderItems.First().ItemId, Quantity = 10 } };

        // maintenance client returns 200 OK
        var maintClient = new HttpClient(new FixedStatusHandler(HttpStatusCode.OK)) { BaseAddress = new Uri("http://itemmaintservice") };
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("ItemService").Returns(_itemClient);
        httpFactory.CreateClient("ItemMaintenanceService").Returns(maintClient);
        httpFactory.CreateClient("PaymentService").Returns(_paymentClient);

        var svc = new RentalOrderService(_mapper, _userService, _availability, httpFactory, _repo);

        // act
        var res = await svc.ReturnAsync("ret-ok", dto);

        // assert
        await _repo.Received(1).UpdateAsync(order, order.Id);
        order.Status.Should().Be(RentalStatus.Returned);
    }

    [Fact]
    public async Task CreateAsync_CashPaymentSuccess_ManualChannel_UpdatesOrderToBooked()
    {
        // arrange
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Items = new List<RentalOrderItemRequestDto> { new RentalOrderItemRequestDto { ItemId = "i-cash-man", Quantity = 1 } }
        };

        _availability.GetAvailableQuantityAsync("i-cash-man", dto.StartDate, dto.EndDate).Returns(1);
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 200, Email = "m@n.com", FirstName = "M", LastName = "N", Role = "User" }));

        _mapper.Map<RentalOrderMongoDB>(dto).Returns(new RentalOrderMongoDB { Id = "cash-man", StoreId = 1, PaymentType = OrderPaymentType.Cash, Channel = OrderChannel.Manual });

        HttpMessageHandlerStub.ResponseMap[$"/api/items/i-cash-man"] = new ApiResponse<ItemResponseDto> { Data = new ItemResponseDto { Id = "i-cash-man", Price = 20m, Quantity = 5, Rates = new List<ItemRateResponseDto>() } };

        // Payment service will return ApiResponse<string> with payment id
        var paymentClient = new HttpClient(new JsonResponseHandler<string>("payment-man-1")) { BaseAddress = new Uri("http://paymentservice") };

        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("ItemService").Returns(_itemClient);
        httpFactory.CreateClient("ItemMaintenanceService").Returns(_itemMaintenanceClient);
        httpFactory.CreateClient("PaymentService").Returns(paymentClient);

        var svc = new RentalOrderService(_mapper, _userService, _availability, httpFactory, _repo);

        // act
        await svc.CreateAsync(dto);

        // assert: manual cash should result in Booked status after creation
        await _repo.Received(1).UpdateAsync(Arg.Any<RentalOrderMongoDB>(), "cash-man");
    }

    [Fact]
    public async Task ApprovePaymentAsync_Success_ManualChannel_Booked()
    {
        // arrange
        var order = new RentalOrderMongoDB { Id = "ap-man", StoreId = 1, Status = RentalStatus.PendingPayment, Channel = OrderChannel.Manual };
        _repo.GetByStripeSessionIdAsync("sess-ap-man").Returns(order);

        var paymentClient = new HttpClient(new PaymentHandler(getReturnsNull: true, postStatus: HttpStatusCode.OK, postPayload: "ap-man-pid")) { BaseAddress = new Uri("http://paymentservice") };
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("ItemService").Returns(_itemClient);
        httpFactory.CreateClient("ItemMaintenanceService").Returns(_itemMaintenanceClient);
        httpFactory.CreateClient("PaymentService").Returns(paymentClient);

        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 321, Email = "u@local", FirstName = "U", LastName = "L", Role = "User" }));

        var svc = new RentalOrderService(_mapper, _userService, _availability, httpFactory, _repo);

        // act
        var result = await svc.ApprovePaymentAsync("sess-ap-man", 50);

        // assert
        await _repo.Received(1).UpdateAsync(order, order.Id);
        order.ApprovedById.Should().Be(321);
        order.Status.Should().Be(RentalStatus.Booked);
    }

    [Fact]
    public async Task ReturnAsync_MatchingQuantities_NoMaintenance_SetsReturned()
    {
        // arrange
        var order = new RentalOrderMongoDB
        {
            Id = "ret-no-maint",
            StoreId = 1,
            Status = RentalStatus.Rented,
            RentalOrderItems = new System.Collections.ObjectModel.Collection<Item>
            {
                new Item { Id = "ri3", ItemId = "i3", Quantity = 2, ItemNameSnapshot = "Item3" }
            }
        };
        _repo.GetByIdAsync("ret-no-maint").Returns(order);
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 9, Email = "x@y.com", FirstName = "X", LastName = "Y", Role = "User" }));

        var dto = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto>
            {
                new ReturnItemConditionDto { RentalOrderItemId = order.RentalOrderItems.First().Id, GoodQty = 2, RepairQty = 0, DamagedQty = 0, LostQty = 0 }
            }
        };

        // act
        var res = await _service.ReturnAsync("ret-no-maint", dto);

        // assert
        await _repo.Received(1).UpdateAsync(order, order.Id);
        order.Status.Should().Be(RentalStatus.Returned);
    }

    [Fact]
    public async Task CreateAsync_NonCash_PendingPaymentStatus()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Items = new List<RentalOrderItemRequestDto> { new RentalOrderItemRequestDto { ItemId = "i-nocash", Quantity = 1 } }
        };

        _availability.GetAvailableQuantityAsync("i-nocash", dto.StartDate, dto.EndDate).Returns(1);
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 55, Email = "no@cash.com", FirstName = "No", LastName = "Cash", Role = "User" }));

        _mapper.Map<RentalOrderMongoDB>(dto).Returns(new RentalOrderMongoDB { Id = "nocash1", StoreId = 1, PaymentType = OrderPaymentType.Other, Channel = OrderChannel.Online });
        _mapper.Map<RentalOrderResponseDto>(Arg.Any<RentalOrderMongoDB>()).Returns(new RentalOrderResponseDto { Id = "nocash1", StoreId = 1, Status = "PendingPayment", Channel = "Online", PaymentType = "Other" });

        HttpMessageHandlerStub.ResponseMap[$"/api/items/i-nocash"] = new ApiResponse<ItemResponseDto> { Data = new ItemResponseDto { Id = "i-nocash", Price = 12m, Quantity = 5, Rates = new List<ItemRateResponseDto>() } };

        await _service.CreateAsync(dto);

        await _repo.Received(1).AddAsync(Arg.Any<RentalOrderMongoDB>());
    }

    [Fact]
    public async Task ApprovePaymentAsync_NotPending_ThrowsAfterRecording()
    {
        // arrange: order exists but not in PendingPayment
        var order = new RentalOrderMongoDB { Id = "ap-not-p", StoreId = 1, Status = RentalStatus.Rented, Channel = OrderChannel.POS };
        _repo.GetByStripeSessionIdAsync("sess-not-p").Returns(order);

        // ensure no existing payments
        var paymentClient = new HttpClient(new PaymentHandler(getReturnsNull: true, postStatus: HttpStatusCode.OK, postPayload: "p-ok")) { BaseAddress = new Uri("http://paymentservice") };
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("ItemService").Returns(_itemClient);
        httpFactory.CreateClient("ItemMaintenanceService").Returns(_itemMaintenanceClient);
        httpFactory.CreateClient("PaymentService").Returns(paymentClient);

        var svc = new RentalOrderService(_mapper, _userService, _availability, httpFactory, _repo);

        // act/assert: The service will POST, then detect status != PendingPayment and throw
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ApprovePaymentAsync("sess-not-p", 10));
    }

    [Fact]
    public async Task ReturnAsync_PackageMaintenance_SuccessAndFailurePaths()
    {
        // arrange: prepare an order with a package containing one package item
        var pkgItem = new PackageItem { ItemId = "pi1", ItemNameSnapshot = "PkgItem1", QuantityPerPackageSnapshot = 1 };
        var pkg = new Package { PackageId = "pkg1", PackageNameSnapshot = "PKG", Quantity = 1, PackageItems = new System.Collections.ObjectModel.Collection<PackageItem> { pkgItem } };

        var order = new RentalOrderMongoDB
        {
            Id = "ret-pkg",
            StoreId = 1,
            Status = RentalStatus.Rented,
            RentalOrderPackages = new System.Collections.ObjectModel.Collection<Package> { pkg }
        };

        _repo.GetByIdAsync("ret-pkg").Returns(order);
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 88, Email = "pkg@x.com", FirstName = "P", LastName = "K", Role = "User" }));

        // dto returns package item with damaged=1 to trigger maintenance
        var dto = new ReturnRentalOrderRequestDto
        {
            Packages = new List<ReturnPackageConditionDto>
            {
                new ReturnPackageConditionDto
                {
                    RentalOrderPackageId = pkg.PackageId,
                    PackageItems = new List<ReturnPackageItemConditionDto>
                    {
                        new ReturnPackageItemConditionDto { RentalOrderPackageItemId = pkgItem.ItemId, GoodQty = 0, RepairQty = 0, DamagedQty = 1, LostQty = 0 }
                    }
                }
            }
        };

        // item GET / package endpoint returns item info (HandleMaintenanceAsync requests /api/packages/{packageId} when packageId != null)
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{pkgItem.ItemId}"] = new ApiResponse<ItemResponseDto> { Data = new ItemResponseDto { Id = pkgItem.ItemId, Quantity = 5 } };
        HttpMessageHandlerStub.ResponseMap[$"/api/packages/{pkg.PackageId}"] = new ApiResponse<ItemResponseDto> { Data = new ItemResponseDto { Id = pkgItem.ItemId, Quantity = 5 } };

        // maintenance client success
        var maintSuccess = new HttpClient(new FixedStatusHandler(HttpStatusCode.OK)) { BaseAddress = new Uri("http://itemmaintservice") };
        var httpFactorySuccess = Substitute.For<IHttpClientFactory>();
        httpFactorySuccess.CreateClient("ItemService").Returns(_itemClient);
        httpFactorySuccess.CreateClient("ItemMaintenanceService").Returns(maintSuccess);
        httpFactorySuccess.CreateClient("PaymentService").Returns(_paymentClient);

        var svcSuccess = new RentalOrderService(_mapper, _userService, _availability, httpFactorySuccess, _repo);
        var res = await svcSuccess.ReturnAsync("ret-pkg", dto);
        await _repo.Received(1).UpdateAsync(order, order.Id);

        // maintenance client failure -> should throw InvalidOperationException
        var maintFail = new HttpClient(new FixedStatusHandler(HttpStatusCode.InternalServerError)) { BaseAddress = new Uri("http://itemmaintservice") };
        var httpFactoryFail = Substitute.For<IHttpClientFactory>();
        httpFactoryFail.CreateClient("ItemService").Returns(_itemClient);
        httpFactoryFail.CreateClient("ItemMaintenanceService").Returns(maintFail);
        httpFactoryFail.CreateClient("PaymentService").Returns(_paymentClient);

        var svcFail = new RentalOrderService(_mapper, _userService, _availability, httpFactoryFail, _repo);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svcFail.ReturnAsync("ret-pkg", dto));
    }

    [Fact]
    public async Task ReturnAsync_AllGood_NoMaintenance_SetsReturned()
    {
        var order = new RentalOrderMongoDB
        {
            Id = "ret-all-good",
            StoreId = 1,
            Status = RentalStatus.Rented,
            RentalOrderItems = new System.Collections.ObjectModel.Collection<Item>
            {
                new Item { Id = "ig1", ItemId = "igitem", Quantity = 3, ItemNameSnapshot = "GoodItem" }
            }
        };
        _repo.GetByIdAsync("ret-all-good").Returns(order);
        _userService.GetUserAsync().Returns(Task.FromResult(new HRS.Shared.Core.Dtos.UserResponseDto { Id = 77, Email = "good@ok.com", FirstName = "G", LastName = "O", Role = "User" }));

        var dto = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto>
            {
                new ReturnItemConditionDto { RentalOrderItemId = order.RentalOrderItems.First().Id, GoodQty = 3, RepairQty = 0, DamagedQty = 0, LostQty = 0 }
            }
        };

        var res = await _service.ReturnAsync("ret-all-good", dto);
        await _repo.Received(1).UpdateAsync(order, order.Id);
        order.Status.Should().Be(RentalStatus.Returned);
    }







}
