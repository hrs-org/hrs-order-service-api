using System.Data;
using AutoMapper;
using FluentAssertions;
using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;

namespace HRS.Test.API.Services;

public class RentalOrderServiceTests
{
    private readonly IAvailabilityService _availabilityService = Substitute.For<IAvailabilityService>();
    private readonly IItemMaintenanceRepository _itemMaintenanceRepository = Substitute.For<IItemMaintenanceRepository>();
    private readonly IItemRateRepository _itemRateRepository = Substitute.For<IItemRateRepository>();
    private readonly IItemRepository _itemRepository = Substitute.For<IItemRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IPackageRateRepository _packageRateRepository = Substitute.For<IPackageRateRepository>();
    private readonly IPackageRepository _packageRepository = Substitute.For<IPackageRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IRentalOrderRepository _rentalOrderRepository = Substitute.For<IRentalOrderRepository>();
    private readonly RentalOrderService _service;
    private readonly IUserContextService _userContextService = Substitute.For<IUserContextService>();

    public RentalOrderServiceTests()
    {
        _service = new RentalOrderService(
            _mapper,
            _userContextService,
            _itemRepository,
            _packageRepository,
            _rentalOrderRepository,
            _itemRateRepository,
            _packageRateRepository,
            _availabilityService,
            _itemMaintenanceRepository,
            _paymentRepository
        );
    }

    [Fact]
    public async Task GetAsync_WhenOrderExists_ReturnsMappedDto()
    {
        var order = new RentalOrder { Id = 1 };
        var dto = new RentalOrderResponseDto
        {
            Id = 1,
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _rentalOrderRepository.GetByIdWithDetailsAsync(1).Returns(order);
        _mapper.Map<RentalOrderResponseDto>(order).Returns(dto);
        var result = await _service.GetAsync(1);
        result.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetAsync_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        _rentalOrderRepository.GetByIdWithDetailsAsync(1).Returns((RentalOrder)null!);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetAsync(1));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var orders = new List<RentalOrder> { new() { Id = 1 }, new() { Id = 2 } };
        var dtos = new List<RentalOrderListDto> { new() { Id = 1 }, new() { Id = 2 } };
        _rentalOrderRepository.GetAllAsync().Returns(orders);
        _mapper.Map<IEnumerable<RentalOrderListDto>>(orders).Returns(dtos);
        var result = await _service.GetAllAsync();
        result.Should().BeEquivalentTo(dtos);
    }

    [Fact]
    public async Task AssignStripeSessionIdAsync_WhenOrderExists_SetsSessionId()
    {
        var order = new RentalOrder { Id = 1 };
        _rentalOrderRepository.GetByIdAsync(1).Returns(order);
        await _service.AssignStripeSessionIdAsync(1, "sess_123");
        order.StripeSessionId.Should().Be("sess_123");
        await _rentalOrderRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task AssignStripeSessionIdAsync_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        _rentalOrderRepository.GetByIdAsync(1).Returns((RentalOrder)null!);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AssignStripeSessionIdAsync(1, "sess_123"));
    }

    [Fact]
    public async Task ApprovePaymentAsync_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        _userContextService.GetUserAsync().Returns(new User { Id = 1 });
        _rentalOrderRepository.GetByStripeSessionIdAsync("sess_123").Returns((RentalOrder)null!);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ApprovePaymentAsync("sess_123", 1000));
    }

    [Fact]
    public async Task ApprovePaymentAsync_WhenAlreadyPaid_ThrowsDuplicateNameException()
    {
        var user = new User { Id = 1 };
        var order = new RentalOrder { Id = 1, Status = RentalStatus.PendingPayment };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.GetByStripeSessionIdAsync("sess_123").Returns(order);
        _paymentRepository.GetByRentalOrderIdAsync(Arg.Any<int>()).Returns(new Payment());
        await Assert.ThrowsAsync<DuplicateNameException>(() => _service.ApprovePaymentAsync("sess_123", 1000));
    }

    [Fact]
    public async Task ApprovePaymentAsync_WhenOrderStatusNotPendingPayment_ThrowsInvalidOperationException()
    {
        var user = new User { Id = 1 };
        var order = new RentalOrder { Id = 1, Status = RentalStatus.Booked };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.GetByStripeSessionIdAsync("sess_123").Returns(order);
        _paymentRepository.GetByRentalOrderIdAsync(Arg.Any<int>()).Returns(null! as Payment);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApprovePaymentAsync("sess_123", 1000));
    }

    [Fact]
    public async Task GetByStasusesAsync_ReturnsMappedDtos()
    {
        var statuses = new[] { RentalStatus.Pending, RentalStatus.Booked };
        var orders = new List<RentalOrder> { new() { Id = 1 }, new() { Id = 2 } };
        var dtos = new List<RentalOrderResponseDto>
        {
            new()
            {
                Id = 1,
                Status = null!,
                Channel = null!,
                PaymentType = null!
            },
            new()
            {
                Id = 2,
                Status = null!,
                Channel = null!,
                PaymentType = null!
            }
        };
        _rentalOrderRepository.GetByStatusesWithDetailsAsync(statuses).Returns(orders);
        _mapper.Map<IEnumerable<RentalOrderResponseDto>>(orders).Returns(dtos);
        var result = await _service.GetByStasusesAsync(statuses);
        result.Should().BeEquivalentTo(dtos);
    }

    [Fact]
    public async Task CreateAsync_WhenStartDateAfterEndDate_ThrowsInvalidOperationException()
    {
        var dto = new CreateRentalOrderRequestDto { StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(-1) };
        _userContextService.GetUserAsync().Returns(new User { Id = 1 });
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WhenItemNotAvailable_ThrowsInvalidOperationException()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Items = new List<RentalOrderItemRequestDto> { new() { ItemId = 1, Quantity = 2 } }
        };
        _userContextService.GetUserAsync().Returns(new User { Id = 1 });
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _availabilityService.GetAvailableQuantityAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WhenPackageNotAvailable_ThrowsInvalidOperationException()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Packages = new List<RentalOrderPackageRequestDto> { new() { PackageId = 1, Quantity = 2 } }
        };
        var pkg = new Package
        { Id = 1, Name = "Pkg", PackageItems = new List<PackageItem> { new() { ItemId = 2, Quantity = 2, Item = new Item { Id = 2, Name = "Item2" } } } };
        _userContextService.GetUserAsync().Returns(new User { Id = 1 });
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _packageRepository.GetByIdWithItemsAsync(1).Returns(pkg);
        _availabilityService.GetAvailableQuantityAsync(2, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WhenItemNotFound_ThrowsKeyNotFoundException()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Items = new List<RentalOrderItemRequestDto> { new() { ItemId = 1, Quantity = 1 } }
        };
        _userContextService.GetUserAsync().Returns(new User { Id = 1 });
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _availabilityService.GetAvailableQuantityAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(1);
        _itemRepository.GetByIdWithParentAsync(1).Returns((Item)null!);
        await Assert.ThrowsAsync<NullReferenceException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WhenPackageNotFound_ThrowsKeyNotFoundException()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Packages = new List<RentalOrderPackageRequestDto> { new() { PackageId = 1, Quantity = 1 } }
        };
        _userContextService.GetUserAsync().Returns(new User { Id = 1 });
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _packageRepository.GetByIdWithItemsAsync(1).Returns((Package)null!);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WhenVariantRequiredButNotSelected_ThrowsInvalidOperationException()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Packages = new List<RentalOrderPackageRequestDto>
                { new() { PackageId = 1, Quantity = 1, SelectedItems = new List<RentalOrderPackageItemRequestDto>() } }
        };
        var child = new Item { Id = 2, Name = "Child" };
        var item = new Item { Id = 1, Name = "Parent", Children = new List<Item> { child } };
        var pkg = new Package { Id = 1, Name = "Pkg", PackageItems = new List<PackageItem> { new() { Id = 10, ItemId = 1, Quantity = 1, Item = item } } };
        _userContextService.GetUserAsync().Returns(new User { Id = 1 });
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _packageRepository.GetByIdWithItemsAsync(1).Returns(pkg);
        _itemRepository.GetByIdWithChildrenAsync(1).Returns(item);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WhenVariantSelectedButInvalid_ThrowsInvalidOperationException()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Packages = new List<RentalOrderPackageRequestDto>
            {
                new()
                {
                    PackageId = 1, Quantity = 1,
                    SelectedItems = new List<RentalOrderPackageItemRequestDto> { new() { PackageItemId = 10, SelectedItemId = 99 } }
                }
            }
        };
        var child = new Item { Id = 2, Name = "Child" };
        var item = new Item { Id = 1, Name = "Parent", Children = new List<Item> { child } };
        var pkg = new Package { Id = 1, Name = "Pkg", PackageItems = new List<PackageItem> { new() { Id = 10, ItemId = 1, Quantity = 1, Item = item } } };
        _userContextService.GetUserAsync().Returns(new User { Id = 1 });
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _packageRepository.GetByIdWithItemsAsync(1).Returns(pkg);
        _itemRepository.GetByIdWithChildrenAsync(1).Returns(item);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WithItemsOnly_SuccessfullyCreatesOrder()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(2),
            Items = new List<RentalOrderItemRequestDto> { new() { ItemId = 1, Quantity = 2 } },
            PaymentType = OrderPaymentType.Other,
            Channel = OrderChannel.Online
        };
        var user = new User { Id = 1 };
        var item = new Item { Id = 1, Name = "Item1", Price = 10m };
        var rate = new ItemRate { DailyRate = 15m };
        var entity = new RentalOrder();
        var responseDto = new RentalOrderResponseDto
        {
            Status = "PendingPayment",
            Channel = "Online",
            PaymentType = "Other"
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _availabilityService.GetAvailableQuantityAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(2);
        _itemRepository.GetByIdWithParentAsync(1).Returns(item);
        _itemRateRepository.GetApplicableRateAsync(1, 2).Returns(rate);
        _mapper.Map<RentalOrder>(dto).Returns(entity);
        _mapper.Map<RentalOrderResponseDto>(entity).Returns(responseDto);
        await _service.CreateAsync(dto);
        await _rentalOrderRepository.Received(1).AddAsync(entity);
        await _rentalOrderRepository.Received(1).SaveChangesAsync();
        await _rentalOrderRepository.Received(1).BeginTransactionAsync();
    }

    [Fact]
    public async Task CreateAsync_WithPackagesAndVariants_SuccessfullyCreatesOrder()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(3),
            Packages = new List<RentalOrderPackageRequestDto>
            {
                new()
                {
                    PackageId = 1,
                    Quantity = 1,
                    SelectedItems = new List<RentalOrderPackageItemRequestDto> { new() { PackageItemId = 10, SelectedItemId = 2 } }
                }
            },
            PaymentType = OrderPaymentType.Other,
            Channel = OrderChannel.Online
        };
        var user = new User { Id = 1 };
        var child = new Item { Id = 2, Name = "Child" };
        var item = new Item { Id = 1, Name = "Parent", Children = new List<Item> { child } };
        var pkg = new Package
        {
            Id = 1,
            Name = "Pkg",
            BasePrice = 20m,
            PackageItems = new List<PackageItem> { new() { Id = 10, ItemId = 1, Quantity = 1, Item = item } }
        };
        var rate = new PackageRate { DailyRate = 25m };
        var entity = new RentalOrder();
        var responseDto = new RentalOrderResponseDto
        {
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _packageRepository.GetByIdWithItemsAsync(1).Returns(pkg);
        _packageRateRepository.GetApplicableRateAsync(1, 3).Returns(rate);
        _itemRepository.GetByIdWithChildrenAsync(1).Returns(item);
        _availabilityService.GetAvailableQuantityAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(1);
        _mapper.Map<RentalOrder>(dto).Returns(entity);
        _mapper.Map<RentalOrderResponseDto>(entity).Returns(responseDto);
        await _service.CreateAsync(dto);
        await _rentalOrderRepository.Received(1).AddAsync(entity);
        await _rentalOrderRepository.Received(1).SaveChangesAsync();
        await _rentalOrderRepository.Received(1).BeginTransactionAsync();
    }

    [Fact]
    public async Task CreateAsync_WithCashPayment_CreatesPaymentRecord()
    {
        var dto = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Items = new List<RentalOrderItemRequestDto> { new() { ItemId = 1, Quantity = 1 } },
            PaymentType = OrderPaymentType.Cash,
            Channel = OrderChannel.POS
        };
        var user = new User { Id = 1 };
        var item = new Item { Id = 1, Name = "Item1", Price = 10m };
        var rate = new ItemRate { DailyRate = 10m };
        var entity = new RentalOrder { PaymentType = OrderPaymentType.Cash, Channel = OrderChannel.POS, Id = 123 };
        var responseDto = new RentalOrderResponseDto
        {
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _availabilityService.GetAvailableQuantityAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(1);
        _itemRepository.GetByIdWithParentAsync(1).Returns(item);
        _itemRateRepository.GetApplicableRateAsync(1, 1).Returns(rate);
        _mapper.Map<RentalOrder>(dto).Returns(entity);
        _mapper.Map<RentalOrderResponseDto>(entity).Returns(responseDto);
        await _service.CreateAsync(dto);
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p => p.RentalOrderId == entity.Id && p.PaymentType == PaymentType.Cash));
        await _paymentRepository.Received(1).SaveChangesAsync();
    }

    [Theory]
    [InlineData(OrderChannel.POS, RentalStatus.Rented)]
    [InlineData(OrderChannel.Manual, RentalStatus.Booked)]
    public async Task ApprovePaymentAsync_UpdatesOrderStatusAndFields(OrderChannel channel, RentalStatus expectedStatus)
    {
        var user = new User { Id = 42 };
        var order = new RentalOrder
        {
            Id = 100,
            Status = RentalStatus.PendingPayment,
            Channel = channel
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByStripeSessionIdAsync("sess_abc").Returns(order);
        _paymentRepository.GetByRentalOrderIdAsync(order.Id).Returns((Payment)null!);
        var responseDto = new RentalOrderResponseDto
        {
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _mapper.Map<RentalOrderResponseDto>(order).Returns(responseDto);

        await _service.ApprovePaymentAsync("sess_abc", 12345);

        order.Status.Should().Be(expectedStatus);
        order.UpdatedById.Should().Be(user.Id);
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        order.ApprovedById.Should().Be(user.Id);
        order.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _rentalOrderRepository.Received(1).Update(order);
        await _rentalOrderRepository.Received(1).SaveChangesAsync();
        await _paymentRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ReturnAsync_WhenOrderIsRented_UpdatesStatusAndFields()
    {
        var user = new User { Id = 5 };
        var order = new RentalOrder
        {
            Id = 50,
            Status = RentalStatus.Rented,
            RentalOrderItems = new List<RentalOrderItem> { new() { Id = 1, Quantity = 2 } },
            RentalOrderPackages = new List<RentalOrderPackage>()
        };
        var dto = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto> { new() { RentalOrderItemId = 1, GoodQty = 2, RepairQty = 0, DamagedQty = 0, LostQty = 0 } },
            Packages = new List<ReturnPackageConditionDto>()
        };
        var responseDto = new RentalOrderResponseDto
        {
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByIdWithDetailsAsync(50).Returns(order);
        _mapper.Map<RentalOrderResponseDto>(order).Returns(responseDto);
        await _service.ReturnAsync(50, dto);
        order.Status.Should().Be(RentalStatus.Returned);
        order.ReturnedById.Should().Be(user.Id);
        order.ReturnedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        order.UpdatedById.Should().Be(user.Id);
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _rentalOrderRepository.Received(1).Update(order);
        await _rentalOrderRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ReturnAsync_WhenOrderNotRented_ThrowsInvalidOperationException()
    {
        var user = new User { Id = 5 };
        var order = new RentalOrder { Id = 50, Status = RentalStatus.Booked };
        var dto = new ReturnRentalOrderRequestDto();
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByIdWithDetailsAsync(50).Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReturnAsync(50, dto));
    }

    [Fact]
    public async Task ReturnAsync_WhenItemNotFound_ThrowsKeyNotFoundException()
    {
        var user = new User { Id = 5 };
        var order = new RentalOrder
        {
            Id = 50,
            Status = RentalStatus.Rented,
            RentalOrderItems = new List<RentalOrderItem>(),
            RentalOrderPackages = new List<RentalOrderPackage>()
        };
        var dto = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto> { new() { RentalOrderItemId = 1, GoodQty = 1, RepairQty = 0, DamagedQty = 0, LostQty = 0 } }
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByIdWithDetailsAsync(50).Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReturnAsync(50, dto));
    }

    [Fact]
    public async Task ReturnAsync_WhenReturnedQuantityMismatch_ThrowsInvalidOperationException()
    {
        var user = new User { Id = 5 };
        var order = new RentalOrder
        {
            Id = 50,
            Status = RentalStatus.Rented,
            RentalOrderItems = new List<RentalOrderItem> { new() { Id = 1, Quantity = 2 } },
            RentalOrderPackages = new List<RentalOrderPackage>()
        };
        var dto = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto> { new() { RentalOrderItemId = 1, GoodQty = 1, RepairQty = 0, DamagedQty = 0, LostQty = 0 } }
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByIdWithDetailsAsync(50).Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReturnAsync(50, dto));
    }

    [Fact]
    public async Task ApproveAsync_WhenOrderIsPending_ApprovesOrder()
    {
        var user = new User { Id = 1 };
        var order = new RentalOrder { Id = 10, Status = RentalStatus.Pending };
        var responseDto = new RentalOrderResponseDto
        {
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByIdWithDetailsAsync(10).Returns(order);
        _mapper.Map<RentalOrderResponseDto>(order).Returns(responseDto);
        await _service.ApproveAsync(10);
        order.Status.Should().Be(RentalStatus.Booked);
        order.ApprovedById.Should().Be(user.Id);
        order.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        order.UpdatedById.Should().Be(user.Id);
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _rentalOrderRepository.Received(1).Update(order);
        await _rentalOrderRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ApproveAsync_WhenOrderNotPending_ThrowsInvalidOperationException()
    {
        var user = new User { Id = 1 };
        var order = new RentalOrder { Id = 10, Status = RentalStatus.Booked };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByIdWithDetailsAsync(10).Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApproveAsync(10));
    }

    [Fact]
    public async Task CancelAsync_WhenOrderIsPending_CancelsOrder()
    {
        var user = new User { Id = 2 };
        var order = new RentalOrder { Id = 20, Status = RentalStatus.Pending };
        var responseDto = new RentalOrderResponseDto
        {
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByIdWithDetailsAsync(20).Returns(order);
        _mapper.Map<RentalOrderResponseDto>(order).Returns(responseDto);
        await _service.CancelAsync(20);
        order.Status.Should().Be(RentalStatus.Cancelled);
        order.ApprovedById.Should().Be(user.Id);
        order.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        order.UpdatedById.Should().Be(user.Id);
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _rentalOrderRepository.Received(1).Update(order);
        await _rentalOrderRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CancelAsync_WhenOrderNotPending_ThrowsInvalidOperationException()
    {
        var user = new User { Id = 2 };
        var order = new RentalOrder { Id = 20, Status = RentalStatus.Booked };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByIdWithDetailsAsync(20).Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CancelAsync(20));
    }

    [Fact]
    public async Task MarkAsRentedAsync_WhenOrderIsBooked_MarksAsRented()
    {
        var user = new User { Id = 3 };
        var order = new RentalOrder { Id = 30, Status = RentalStatus.Booked };
        var responseDto = new RentalOrderResponseDto
        {
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByIdWithDetailsAsync(30).Returns(order);
        _mapper.Map<RentalOrderResponseDto>(order).Returns(responseDto);
        await _service.MarkAsRentedAsync(30);
        order.Status.Should().Be(RentalStatus.Rented);
        order.UpdatedById.Should().Be(user.Id);
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _rentalOrderRepository.Received(1).Update(order);
        await _rentalOrderRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task MarkAsRentedAsync_WhenOrderNotBooked_ThrowsInvalidOperationException()
    {
        var user = new User { Id = 3 };
        var order = new RentalOrder { Id = 30, Status = RentalStatus.Pending };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByIdWithDetailsAsync(30).Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.MarkAsRentedAsync(30));
    }

    [Fact]
    public async Task CloseAsync_WhenOrderIsReturned_ClosesOrder()
    {
        var user = new User { Id = 4 };
        var order = new RentalOrder { Id = 40, Status = RentalStatus.Returned };
        var responseDto = new RentalOrderResponseDto
        {
            Status = null!,
            Channel = null!,
            PaymentType = null!
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.GetByIdWithDetailsAsync(40).Returns(order);
        _mapper.Map<RentalOrderResponseDto>(order).Returns(responseDto);
        await _service.CloseAsync(40);
        order.Status.Should().Be(RentalStatus.Completed);
        order.ClosedById.Should().Be(user.Id);
        order.ClosedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        order.UpdatedById.Should().Be(user.Id);
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _rentalOrderRepository.Received(1).Update(order);
        await _rentalOrderRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CloseAsync_WhenOrderNotReturned_ThrowsInvalidOperationException()
    {
        var user = new User { Id = 4 };
        var order = new RentalOrder { Id = 40, Status = RentalStatus.Booked };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.GetByIdWithDetailsAsync(40).Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CloseAsync(40));
    }

    [Fact]
    public async Task ReturnAsync_WhenPackageItemNotFound_ThrowsKeyNotFoundException()
    {
        var user = new User { Id = 6 };
        var order = new RentalOrder
        {
            Id = 60,
            Status = RentalStatus.Rented,
            RentalOrderItems = new List<RentalOrderItem>(),
            RentalOrderPackages = new List<RentalOrderPackage>
            {
                new()
                {
                    Id = 100,
                    Items = new List<RentalOrderPackageItem>()
                }
            }
        };
        var dto = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto>(),
            Packages = new List<ReturnPackageConditionDto>
            {
                new()
                {
                    RentalOrderPackageId = 100,
                    PackageItems = new List<ReturnPackageItemConditionDto>
                    {
                        new() { RentalOrderPackageItemId = 999, GoodQty = 1, RepairQty = 0, DamagedQty = 0, LostQty = 0 }
                    }
                }
            }
        };
        _userContextService.GetUserAsync().Returns(user);
        _rentalOrderRepository.BeginTransactionAsync().Returns(Substitute.For<IDbContextTransaction>());
        _rentalOrderRepository.GetByIdWithDetailsAsync(60).Returns(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReturnAsync(60, dto));
    }
}
