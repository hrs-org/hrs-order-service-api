using System.Data;
using AutoMapper;
using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;

namespace HRS.API.Services;

public class RentalOrderService : IRentalOrderService
{
    private const string OrderNotFound = "Rental order not found";
    private readonly IAvailabilityService _availabilityService;
    private readonly IItemMaintenanceRepository _itemMaintenanceRepository;
    private readonly IItemRateRepository _itemRateRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IMapper _mapper;
    private readonly IPackageRateRepository _packageRateRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRentalOrderRepository _rentalOrderRepository;
    private readonly IUserContextService _userContextService;

    public RentalOrderService(
        IMapper mapper,
        IUserContextService userContextService,
        IItemRepository itemRepository,
        IPackageRepository packageRepository,
        IRentalOrderRepository rentalOrderRepository,
        IItemRateRepository itemRateRepository,
        IPackageRateRepository packageRateRepository,
        IAvailabilityService availabilityService,
        IItemMaintenanceRepository itemMaintenanceRepository,
        IPaymentRepository paymentRepository)
    {
        _mapper = mapper;
        _userContextService = userContextService;
        _itemRepository = itemRepository;
        _packageRepository = packageRepository;
        _rentalOrderRepository = rentalOrderRepository;
        _itemRateRepository = itemRateRepository;
        _packageRateRepository = packageRateRepository;
        _availabilityService = availabilityService;
        _itemMaintenanceRepository = itemMaintenanceRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<RentalOrderResponseDto> GetAsync(int id)
    {
        var order = await _rentalOrderRepository.GetByIdWithDetailsAsync(id)
                    ?? throw new KeyNotFoundException(OrderNotFound);

        return _mapper.Map<RentalOrderResponseDto>(order);
    }

    public async Task<IEnumerable<RentalOrderListDto>> GetAllAsync()
    {
        var orders = await _rentalOrderRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<RentalOrderListDto>>(orders);
    }

    public async Task<IEnumerable<RentalOrderResponseDto>> GetByStasusesAsync(RentalStatus[] statuses)
    {
        var orders = await _rentalOrderRepository.GetByStatusesWithDetailsAsync(statuses);
        return _mapper.Map<IEnumerable<RentalOrderResponseDto>>(orders);
    }

    public async Task<RentalOrderResponseDto> CreateAsync(CreateRentalOrderRequestDto dto)
    {
        var user = await _userContextService.GetUserAsync();
        await using var tx = await _rentalOrderRepository.BeginTransactionAsync();

        try
        {
            if (dto.StartDate >= dto.EndDate)
                throw new InvalidOperationException("End date must be after start date.");

            var rentalDays = Math.Max(1, (dto.EndDate - dto.StartDate).Days);

            if (dto.Items is not null)
                foreach (var itemDto in dto.Items)
                {
                    var available = await _availabilityService.GetAvailableQuantityAsync(
                        itemDto.ItemId, dto.StartDate, dto.EndDate);

                    if (available < itemDto.Quantity)
                        throw new InvalidOperationException(
                            $"Item '{itemDto.ItemId}' not available. Requested {itemDto.Quantity}, available {available}.");
                }

            if (dto.Packages is not null)
                foreach (var pkgDto in dto.Packages)
                {
                    var pkg = await _packageRepository.GetByIdWithItemsAsync(pkgDto.PackageId)
                              ?? throw new KeyNotFoundException($"Package {pkgDto.PackageId} not found.");

                    foreach (var pi in pkg.PackageItems)
                    {
                        var required = pi.Quantity * pkgDto.Quantity;
                        var available = await _availabilityService.GetAvailableQuantityAsync(pi.ItemId, dto.StartDate, dto.EndDate);

                        if (available < required)
                            throw new InvalidOperationException(
                                $"Package '{pkg.Name}' unavailable — insufficient '{pi.Item?.Name}' (required {required}, available {available}).");
                    }
                }

            var entity = _mapper.Map<RentalOrder>(dto);
            entity.CreatedById = user.Id;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedById = user.Id;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.Status =
                entity is { PaymentType: OrderPaymentType.Cash, Channel: OrderChannel.POS }
                    ? RentalStatus.Rented
                    : entity is { PaymentType: OrderPaymentType.Cash, Channel: OrderChannel.Manual }
                        ? RentalStatus.Booked
                        : RentalStatus.PendingPayment;

            var totalAmount = 0m;

            if (dto.Items is not null)
                foreach (var itemDto in dto.Items)
                {
                    var item = await _itemRepository.GetByIdWithParentAsync(itemDto.ItemId)
                               ?? throw new KeyNotFoundException($"Item {itemDto.ItemId} not found.");

                    var rate = await _itemRateRepository.GetApplicableRateAsync(item.Parent?.Id ?? item.Id, rentalDays);
                    var dailyRate = rate?.DailyRate ?? item.Parent?.Price ?? item.Price;

                    entity.RentalOrderItems.Add(new RentalOrderItem
                    {
                        ItemId = item.Id,
                        ItemNameSnapshot = item.Name,
                        DailyRateSnapshot = dailyRate,
                        Quantity = itemDto.Quantity
                    });

                    totalAmount += dailyRate * itemDto.Quantity * rentalDays;
                }

            if (dto.Packages is not null)
                foreach (var pkgDto in dto.Packages)
                {
                    var pkg = await _packageRepository.GetByIdWithItemsAsync(pkgDto.PackageId)
                              ?? throw new KeyNotFoundException($"Package {pkgDto.PackageId} not found.");

                    var rate = await _packageRateRepository.GetApplicableRateAsync(pkg.Id, rentalDays);
                    var dailyRate = rate?.DailyRate ?? pkg.BasePrice;

                    var rentalPkg = new RentalOrderPackage
                    {
                        PackageId = pkg.Id,
                        PackageNameSnapshot = pkg.Name,
                        DailyRateSnapshot = dailyRate,
                        Quantity = pkgDto.Quantity
                    };

                    foreach (var pi in pkg.PackageItems)
                    {
                        var item = await _itemRepository.GetByIdWithChildrenAsync(pi.ItemId)
                                   ?? throw new KeyNotFoundException($"Item {pi.ItemId} not found.");

                        var finalItem = item;
                        var selectedItemId = pkgDto.SelectedItems?
                            .FirstOrDefault(si => si.PackageItemId == pi.Id)?.SelectedItemId;

                        if (item.Children.Count != 0)
                        {
                            if (!selectedItemId.HasValue)
                                throw new InvalidOperationException($"Variant required for item '{item.Name}' in package '{pkg.Name}'.");

                            var selectedChild = item.Children.FirstOrDefault(c => c.Id == selectedItemId.Value)
                                                ?? throw new InvalidOperationException($"Invalid variant selection for '{item.Name}'.");

                            finalItem = selectedChild;
                        }

                        rentalPkg.Items.Add(new RentalOrderPackageItem
                        {
                            ItemId = finalItem.Id,
                            ItemNameSnapshot = finalItem.Name,
                            QuantityPerPackageSnapshot = pi.Quantity
                        });
                    }

                    entity.RentalOrderPackages.Add(rentalPkg);
                    totalAmount += dailyRate * pkgDto.Quantity * rentalDays;
                }


            entity.TotalAmount = totalAmount;

            await _rentalOrderRepository.AddAsync(entity);
            await _rentalOrderRepository.SaveChangesAsync();

            if (entity.PaymentType == OrderPaymentType.Cash)
            {
                var payment = new Payment
                {
                    RentalOrderId = entity.Id,
                    Amount = entity.TotalAmount,
                    PaymentType = PaymentType.Cash,
                    PaymentDate = DateTime.UtcNow,
                    Status = PaymentStatus.Completed,
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                };
                await _paymentRepository.AddAsync(payment);
                await _paymentRepository.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return _mapper.Map<RentalOrderResponseDto>(entity);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task AssignStripeSessionIdAsync(int orderId, string sessionId)
    {
        var order = await _rentalOrderRepository.GetByIdAsync(orderId) ?? throw new KeyNotFoundException("Order not found");

        order.StripeSessionId = sessionId;
        await _rentalOrderRepository.SaveChangesAsync();
    }

    public async Task<RentalOrderResponseDto> ApprovePaymentAsync(string sessionId, long? amount)
    {
        var user = await _userContextService.GetUserAsync();

        await using var tx = await _rentalOrderRepository.BeginTransactionAsync();

        try
        {
            var order = await _rentalOrderRepository.GetByStripeSessionIdAsync(sessionId)
                        ?? throw new KeyNotFoundException(OrderNotFound);

            var existingPayments = await _paymentRepository
                .GetByRentalOrderIdAsync(order.Id);

            if (existingPayments != null)
                throw new DuplicateNameException("Payment has already been recorded for this order.");

            var payment = new Payment
            {
                RentalOrderId = order.Id,
                StripeSessionId = sessionId,
                Amount = (decimal)(amount ?? 0) / 100,
                PaymentType = PaymentType.Stripe,
                PaymentDate = DateTime.UtcNow,
                Status = PaymentStatus.Completed,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };
            await _paymentRepository.AddAsync(payment);

            if (order.Status != RentalStatus.PendingPayment)
                throw new InvalidOperationException("Only pending payment orders can be approved.");

            order.Status = order.Channel switch
            {
                OrderChannel.POS => RentalStatus.Rented,
                OrderChannel.Manual => RentalStatus.Booked,
                _ => RentalStatus.Pending
            };
            order.UpdatedById = user.Id;
            order.UpdatedAt = DateTime.UtcNow;

            if (order.Status is RentalStatus.Rented or RentalStatus.Booked)
            {
                order.ApprovedById = user.Id;
                order.ApprovedAt = DateTime.UtcNow;
            }

            _rentalOrderRepository.Update(order);
            await _rentalOrderRepository.SaveChangesAsync();
            await _paymentRepository.SaveChangesAsync();

            await tx.CommitAsync();
            return _mapper.Map<RentalOrderResponseDto>(order);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<RentalOrderResponseDto> ApproveAsync(int id)
    {
        var user = await _userContextService.GetUserAsync();

        await using var tx = await _rentalOrderRepository.BeginTransactionAsync();

        try
        {
            var order = await _rentalOrderRepository.GetByIdWithDetailsAsync(id)
                        ?? throw new KeyNotFoundException(OrderNotFound);

            if (order.Status != RentalStatus.Pending)
                throw new InvalidOperationException("Only pending orders can be approved.");

            order.Status = RentalStatus.Booked;
            order.ApprovedById = user.Id;
            order.ApprovedAt = DateTime.UtcNow;
            order.UpdatedById = user.Id;
            order.UpdatedAt = DateTime.UtcNow;

            _rentalOrderRepository.Update(order);
            await _rentalOrderRepository.SaveChangesAsync();

            await tx.CommitAsync();
            return _mapper.Map<RentalOrderResponseDto>(order);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<RentalOrderResponseDto> CancelAsync(int id)
    {
        var user = await _userContextService.GetUserAsync();

        await using var tx = await _rentalOrderRepository.BeginTransactionAsync();

        try
        {
            var order = await _rentalOrderRepository.GetByIdWithDetailsAsync(id)
                        ?? throw new KeyNotFoundException(OrderNotFound);

            if (order.Status != RentalStatus.Pending)
                throw new InvalidOperationException("Only pending orders can be approved.");

            order.Status = RentalStatus.Cancelled;
            order.ApprovedById = user.Id;
            order.ApprovedAt = DateTime.UtcNow;
            order.UpdatedById = user.Id;
            order.UpdatedAt = DateTime.UtcNow;

            _rentalOrderRepository.Update(order);
            await _rentalOrderRepository.SaveChangesAsync();

            await tx.CommitAsync();
            return _mapper.Map<RentalOrderResponseDto>(order);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<RentalOrderResponseDto> MarkAsRentedAsync(int id)
    {
        var user = await _userContextService.GetUserAsync();

        await using var tx = await _rentalOrderRepository.BeginTransactionAsync();

        try
        {
            var order = await _rentalOrderRepository.GetByIdWithDetailsAsync(id)
                        ?? throw new KeyNotFoundException(OrderNotFound);

            if (order.Status != RentalStatus.Booked)
                throw new InvalidOperationException("Only booked orders can be marked as rented.");

            order.Status = RentalStatus.Rented;
            order.UpdatedById = user.Id;
            order.UpdatedAt = DateTime.UtcNow;

            _rentalOrderRepository.Update(order);
            await _rentalOrderRepository.SaveChangesAsync();

            await tx.CommitAsync();
            return _mapper.Map<RentalOrderResponseDto>(order);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<RentalOrderResponseDto> ReturnAsync(int id, ReturnRentalOrderRequestDto dto)
    {
        var user = await _userContextService.GetUserAsync();
        await using var tx = await _rentalOrderRepository.BeginTransactionAsync();

        try
        {
            var order = await _rentalOrderRepository.GetByIdWithDetailsAsync(id)
                        ?? throw new KeyNotFoundException($"Rental order {id} not found.");

            if (order.Status != RentalStatus.Rented)
                throw new InvalidOperationException("Only rented orders can be returned.");

            ValidateReturnedQuantitiesAsync(order, dto);

            if (dto.Items != null)
                foreach (var itemCondition in dto.Items)
                {
                    var orderItem = order.RentalOrderItems
                                        .FirstOrDefault(x => x.Id == itemCondition.RentalOrderItemId)
                                    ?? throw new KeyNotFoundException($"RentalOrderItem {itemCondition.RentalOrderItemId} not found.");

                    orderItem.SetReturnConditions(
                        itemCondition.GoodQty,
                        itemCondition.RepairQty,
                        itemCondition.DamagedQty,
                        itemCondition.LostQty
                    );

                    await HandleMaintenanceAsync(orderItem.ItemId, order.Id, itemCondition, user.Id);
                }

            if (dto.Packages != null)
                foreach (var pkgDto in dto.Packages)
                {
                    var orderPackage = order.RentalOrderPackages
                                           .FirstOrDefault(x => x.Id == pkgDto.RentalOrderPackageId)
                                       ?? throw new KeyNotFoundException($"RentalOrderPackage {pkgDto.RentalOrderPackageId} not found.");

                    foreach (var pkgItemDto in pkgDto.PackageItems)
                    {
                        var orderPkgItem = orderPackage.Items
                                               .FirstOrDefault(x => x.Id == pkgItemDto.RentalOrderPackageItemId)
                                           ?? throw new KeyNotFoundException($"RentalOrderPackageItem {pkgItemDto.RentalOrderPackageItemId} not found.");

                        orderPkgItem.GoodQty = pkgItemDto.GoodQty;
                        orderPkgItem.RepairQty = pkgItemDto.RepairQty;
                        orderPkgItem.DamagedQty = pkgItemDto.DamagedQty;
                        orderPkgItem.LostQty = pkgItemDto.LostQty;

                        await HandleMaintenanceAsync(orderPkgItem.ItemId, order.Id, pkgItemDto, user.Id);
                    }
                }

            order.ItemsGoodCount = order.RentalOrderItems.Sum(i => i.GoodQty)
                                   + order.RentalOrderPackages.SelectMany(p => p.Items).Sum(i => i.GoodQty);

            order.ItemsIssueCount = order.RentalOrderItems.Count(i => i.HasIssues)
                                    + order.RentalOrderPackages.SelectMany(p => p.Items).Count(i => i.HasIssues);

            order.HasIssues = order.ItemsIssueCount > 0;
            order.Status = RentalStatus.Returned;
            order.ReturnedAt = DateTime.UtcNow;
            order.ReturnedById = user.Id;
            order.ReturnRemarks = dto.Remarks;
            order.UpdatedById = user.Id;
            order.UpdatedAt = DateTime.UtcNow;

            _rentalOrderRepository.Update(order);
            await _rentalOrderRepository.SaveChangesAsync();
            await tx.CommitAsync();

            return _mapper.Map<RentalOrderResponseDto>(order);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }


    public async Task<RentalOrderResponseDto> CloseAsync(int id)
    {
        var user = await _userContextService.GetUserAsync();

        var order = await _rentalOrderRepository.GetByIdWithDetailsAsync(id)
                    ?? throw new KeyNotFoundException(OrderNotFound);

        if (order.Status != RentalStatus.Returned)
            throw new InvalidOperationException("Only returned orders can be closed.");

        order.Status = RentalStatus.Completed;
        order.ClosedById = user.Id;
        order.ClosedAt = DateTime.UtcNow;
        order.UpdatedById = user.Id;
        order.UpdatedAt = DateTime.UtcNow;

        _rentalOrderRepository.Update(order);
        await _rentalOrderRepository.SaveChangesAsync();

        return _mapper.Map<RentalOrderResponseDto>(order);
    }

    private async Task HandleMaintenanceAsync(int? itemId, int orderId, object dto, int userId)
    {
        if (itemId == null || itemId <= 0) return;

        // Extract quantities from DTO
        int repairQty, damagedQty, lostQty;
        switch (dto)
        {
            case ReturnItemConditionDto itemDto:
                repairQty = itemDto.RepairQty;
                damagedQty = itemDto.DamagedQty;
                lostQty = itemDto.LostQty;
                break;

            case ReturnPackageItemConditionDto pkgDto:
                repairQty = pkgDto.RepairQty;
                damagedQty = pkgDto.DamagedQty;
                lostQty = pkgDto.LostQty;
                break;

            default:
                return;
        }

        if (repairQty + damagedQty + lostQty == 0)
            return; // nothing to do

        var item = await _itemRepository.GetByIdAsync(itemId.Value);
        if (item == null)
            return;

        // --- REPAIR ---
        if (repairQty > 0)
            await _itemMaintenanceRepository.AddAsync(new ItemMaintenance
            {
                ItemId = item.Id,
                RentalOrderId = orderId,
                Type = ItemMaintenanceType.Repair,
                Quantity = repairQty,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
                Remarks = "Auto-generated repair record on return"
            });

        // --- BROKEN ---
        if (damagedQty > 0)
        {
            await _itemMaintenanceRepository.AddAsync(new ItemMaintenance
            {
                ItemId = item.Id,
                RentalOrderId = orderId,
                Type = ItemMaintenanceType.Broken,
                Quantity = damagedQty,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
                Remarks = "Auto-generated broken record on return"
            });

            // Decrease item quantity permanently
            item.Quantity = Math.Max(0, item.Quantity - damagedQty);
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedById = userId;
            _itemRepository.Update(item);
        }

        // --- LOST ---
        if (lostQty > 0)
        {
            await _itemMaintenanceRepository.AddAsync(new ItemMaintenance
            {
                ItemId = item.Id,
                RentalOrderId = orderId,
                Type = ItemMaintenanceType.Lost,
                Quantity = lostQty,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
                Remarks = "Auto-generated lost record on return"
            });

            // Decrease item quantity permanently
            item.Quantity = Math.Max(0, item.Quantity - lostQty);
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedById = userId;
            _itemRepository.Update(item);
        }
    }


    private static void ValidateReturnedQuantitiesAsync(RentalOrder order, ReturnRentalOrderRequestDto dto)
    {
        foreach (var itemDto in dto.Items ?? Enumerable.Empty<ReturnItemConditionDto>())
        {
            var orderItem = order.RentalOrderItems.FirstOrDefault(x => x.Id == itemDto.RentalOrderItemId);
            if (orderItem == null)
                throw new InvalidOperationException($"Item with ID {itemDto.RentalOrderItemId} not found in this order.");

            var totalReturned = itemDto.GoodQty + itemDto.RepairQty + itemDto.DamagedQty + itemDto.LostQty;
            if (totalReturned != orderItem.Quantity)
                throw new InvalidOperationException(
                    $"Returned quantity mismatch for item '{orderItem.ItemNameSnapshot}'. " +
                    $"Expected {orderItem.Quantity}, got {totalReturned}.");
        }

        foreach (var pkgDto in dto.Packages ?? Enumerable.Empty<ReturnPackageConditionDto>())
        {
            var orderPackage = order.RentalOrderPackages.FirstOrDefault(x => x.Id == pkgDto.RentalOrderPackageId);
            if (orderPackage == null)
                throw new InvalidOperationException($"Package {pkgDto.RentalOrderPackageId} not found.");

            foreach (var pkgItemDto in pkgDto.PackageItems)
            {
                var pkgItem = orderPackage.Items.FirstOrDefault(x => x.Id == pkgItemDto.RentalOrderPackageItemId);
                if (pkgItem == null)
                    throw new InvalidOperationException($"Package item {pkgItemDto.RentalOrderPackageItemId} not found.");

                var totalReturned = pkgItemDto.GoodQty + pkgItemDto.RepairQty + pkgItemDto.DamagedQty + pkgItemDto.LostQty;
                if (totalReturned != pkgItem.QuantityPerPackageSnapshot)
                    throw new InvalidOperationException(
                        $"Returned quantity mismatch for package item '{pkgItem.ItemNameSnapshot}'. " +
                        $"Expected {pkgItem.QuantityPerPackageSnapshot}, got {totalReturned}.");
            }
        }
    }
}
