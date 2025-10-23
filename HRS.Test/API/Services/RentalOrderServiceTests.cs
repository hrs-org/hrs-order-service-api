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
        _repo.GetByIdAsync("missing").Returns((RentalOrderMongoDB)null);
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
    public async Task AssignStripeSessionIdAsync_UpdatesOrder()
    {
        var order = new RentalOrderMongoDB { Id = "o1", StoreId = 1 };
        _repo.GetByIdAsync("o1").Returns(order);

        await _service.AssignStripeSessionIdAsync("o1", "sess123");

        order.StripeSessionId.Should().Be("sess123");
        await _repo.Received(1).UpdateAsync(order, "o1");
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







}
