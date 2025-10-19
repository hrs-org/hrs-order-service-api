using System.Data;
using AutoMapper;
using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using HRS.Shared.Core.Dtos;
using HRS.Shared.Core.Interfaces;
using Stripe.BillingPortal;

namespace HRS.API.Services;

public class RentalOrderService : IRentalOrderService
{
    private const string OrderNotFound = "Rental order not found";
    private readonly IAvailabilityService _availabilityService;

    private readonly HttpClient _itemClient;
    private readonly HttpClient _itemMaintenanceClient;
    private readonly HttpClient _paymentClient;
    private readonly IMapper _mapper;
    private readonly IRentalOrderRepository _rentalOrderRepository;
    private readonly IUserContextService _userContextService;
    private readonly ICrudMongoDBRepository<RentalOrderItemMongoDB> _rentalOrderItemRepository;
    private readonly ICrudMongoDBRepository<RentalOrderPackageMongoDB> _rentalOrderPackageRepository;

    public RentalOrderService(
        IMapper mapper,
        IUserContextService userContextService,
        IRentalOrderRepository rentalOrderRepository,
        IAvailabilityService availabilityService,
        IHttpClientFactory httpClientFactory,
        IRentalOrderItemMongoDBRepository rentalOrderItemRepository,
        ICrudMongoDBRepository<RentalOrderPackageMongoDB> rentalOrderPackageRepository
    )
    {
        _mapper = mapper;
        _userContextService = userContextService;
        _rentalOrderRepository = rentalOrderRepository;
        _availabilityService = availabilityService;
        _itemMaintenanceClient = httpClientFactory.CreateClient("ItemMaintenanceService");
        _paymentClient = httpClientFactory.CreateClient("PaymentService");
        _itemClient = httpClientFactory.CreateClient("ItemService");
        _rentalOrderItemRepository = rentalOrderItemRepository;
        _rentalOrderPackageRepository = rentalOrderPackageRepository;
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

    public async Task<IEnumerable<RentalOrderResponseDto>> GetByStatusesAsync(RentalStatus[] statuses)
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
                    // var pkg = await _packageRepository.GetByIdWithItemsAsync(pkgDto.PackageId)
                    //           ?? throw new KeyNotFoundException($"Package {pkgDto.PackageId} not found.");
                    var pkgResponse = await _itemClient.GetFromJsonAsync<PackageResponseDto>($"/api/package/getbyidwithitems/{pkgDto.PackageId}"); // Adjust the endpoint as necessary
                    if (pkgResponse == null || pkgResponse.Items == null)
                        throw new KeyNotFoundException($"Package {pkgDto.PackageId} not found.");
                    var pkg = pkgResponse;
                    foreach (var pi in pkg.Items)
                    {
                        var required = pi.Quantity * pkgDto.Quantity;
                        var available = await _availabilityService.GetAvailableQuantityAsync(pi.ItemId, dto.StartDate, dto.EndDate);

                        if (available < required)
                            throw new InvalidOperationException(
                                $"Package '{pkg.Name}' unavailable — insufficient '{pi.ItemName}' (required {required}, available {available}).");
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
                    // var item = await _itemRepository.GetByIdWithParentAsync(itemDto.ItemId)
                    //            ?? throw new KeyNotFoundException($"Item {itemDto.ItemId} not found.");
                    var itemResponse = await _itemClient.GetFromJsonAsync<ItemResponseDto>($"/api/item/getbyidwithparent/{itemDto.ItemId}"); // Adjust the endpoint as necessary
                    var item = itemResponse;
                    if (item == null)
                        throw new KeyNotFoundException($"Item {itemDto.ItemId} not found.");
                    // ******** need to create new massage to get parent with children id ********
                    var parentResponse = await _itemClient.GetFromJsonAsync<ItemResponseDto>($"/api/item/getparentbychildentid/{item.Id}"); // Adjust the endpoint as necessary
                    var ParentId = parentResponse?.Id;
                    // var rate = await _itemRateRepository.GetApplicableRateAsync(item.Parent?.Id ?? item.Id, rentalDays);
                    var rateResponse = await _itemClient.GetFromJsonAsync<ItemRateResponseDto>($"/api/itemrate/getapplicablerate/{(ParentId.HasValue ? ParentId.Value : item.Id)}/{rentalDays}"); // Adjust the endpoint as necessary
                    var rate = rateResponse;
                    // var dailyRate = rate?.DailyRate ?? item.Parent?.Price ?? item.Price;
                    var dailyRate = rate?.DailyRate ?? parentResponse?.Price ?? item.Price;

                    entity.RentalOrderItems.Add(new RentalOrderItemMongoDB
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

                    // var pkg = await _packageRepository.GetByIdWithItemsAsync(pkgDto.PackageId)
                    //           ?? throw new KeyNotFoundException($"Package {pkgDto.PackageId} not found.");
                    var pkg = await _itemClient.GetFromJsonAsync<PackageResponseDto>($"/api/package/getbyidwithitems/{pkgDto.PackageId}"); // Adjust the endpoint as necessary
                    if (pkg == null || pkg.Items == null)
                        throw new KeyNotFoundException($"Package {pkgDto.PackageId} not found.");
                    // var rate = await _packageRateRepository.GetApplicableRateAsync(pkg.Id, rentalDays);
                    var rate = await _itemClient.GetFromJsonAsync<PackageRateResponseDto>($"/api/packagerate/getapplicablerate/{pkg.Id}/{rentalDays}"); // Adjust the endpoint as necessary
                    var dailyRate = rate?.DailyRate ?? pkg.BasePrice;

                    var rentalPkg = new RentalOrderPackageMongoDB
                    {
                        PackageId = pkg.Id,
                        PackageNameSnapshot = pkg.Name,
                        DailyRateSnapshot = dailyRate,
                        Quantity = pkgDto.Quantity
                    };

                    foreach (var pi in pkg.Items)
                    {

                        // var item = await _itemRepository.GetByIdWithChildrenAsync(pi.ItemId)
                        //    ?? throw new KeyNotFoundException($"Item {pi.ItemId} not found.");
                        var itemResponse = await _itemClient.GetFromJsonAsync<ItemResponseDto>($"/api/item/getbyidwithchildren/{pi.ItemId}"); // Adjust the endpoint as necessary
                        var item = itemResponse;
                        var finalItem = item;
                        var selectedItemId = pkgDto.SelectedItems?
                            .FirstOrDefault(si => si.PackageItemId == pi.ItemId)?.SelectedItemId;

                        if (item?.Children?.Count != 0)
                        {
                            if (!selectedItemId.HasValue)
                                throw new InvalidOperationException($"Variant required for item '{item?.Name}' in package '{pkg.Name}'.");

                            var selectedChild = item?.Children?.FirstOrDefault(c => c.Id == selectedItemId.Value)
                                                ?? throw new InvalidOperationException($"Invalid variant selection for '{item?.Name}'.");

                            finalItem = selectedChild;
                        }
                        if (finalItem == null)
                            throw new KeyNotFoundException($"Item {pi.ItemId} not found.");
                        rentalPkg.Items.Add(new RentalOrderPackageItemMongoDB
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
                var paymentId = await _paymentClient.PostAsJsonAsync("/api/payment", new
                {
                    OrderId = entity.Id,
                    Amount = entity.TotalAmount,
                    SessionId = "User-Cash-Payment",
                    PaymentType = 1, // Cash
                    Status = 1, // Completed
                });
                if (!paymentId.IsSuccessStatusCode)
                    throw new InvalidOperationException("Failed to record cash payment.");
                entity.PaymentId = await paymentId.Content.ReadAsStringAsync();
                // var payment = new Payment
                // {
                //     RentalOrderId = entity.Id,
                //     Amount = entity.TotalAmount,
                //     PaymentType = PaymentType.Cash,
                //     PaymentDate = DateTime.UtcNow,
                //     Status = PaymentStatus.Completed,
                //     CreatedBy = user,
                //     CreatedAt = DateTime.UtcNow
                // };
                // await _paymentRepository.AddAsync(payment);
                // await _paymentRepository.SaveChangesAsync();

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


    // add controller for this method
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

            // var existingPayments = await _paymentRepository.GetByRentalOrderIdAsync(order.Id);
            //******** need to add payment respond dto ******** ringt now use local dto instead ********
            var response = await _paymentClient.GetFromJsonAsync<object>($"/api/payments/orders/{order.Id}"); // Adjust the endpoint as necessary
            var existingPayments = response;
            if (existingPayments != null)
                throw new DuplicateNameException("Payment has already been recorded for this order.");

            // var payment = new Payment
            // {
            //     RentalOrderId = order.Id,
            //     StripeSessionId = sessionId,
            //     Amount = (decimal)(amount ?? 0) / 100,
                // PaymentType = PaymentType.Stripe,
            //     PaymentDate = DateTime.UtcNow,
                // Status = PaymentStatus.Completed,
            //     CreatedBy = user,
            //     CreatedAt = DateTime.UtcNow
            // };
            // await _paymentRepository.AddAsync(payment);
            var paymentId = await _paymentClient.PostAsJsonAsync("/api/payment", new
                {
                    OrderId = order.Id,
                    Amount = (decimal)(amount ?? 0) / 100,
                    SessionId = sessionId,
                    PaymentType = 0, // stripe
                    Status = 1, // Completed
                });
                if (!paymentId.IsSuccessStatusCode)
                    throw new InvalidOperationException("Failed to record stripe payment.");
                order.PaymentId = await paymentId.Content.ReadAsStringAsync();



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
            // change status in rentalorderitem and package
            await _rentalOrderItemRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
            await _rentalOrderPackageRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
            await _rentalOrderRepository.SaveChangesAsync();
            // await _paymentRepository.SaveChangesAsync();

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
            await _rentalOrderItemRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
            await _rentalOrderPackageRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
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
            await _rentalOrderItemRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
            await _rentalOrderPackageRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
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
            await _rentalOrderItemRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
            await _rentalOrderPackageRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
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
                                        .FirstOrDefault(x => x.ItemId == itemCondition.RentalOrderItemId)
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
                                           .FirstOrDefault(x => x.PackageId == pkgDto.RentalOrderPackageId)
                                       ?? throw new KeyNotFoundException($"RentalOrderPackage {pkgDto.RentalOrderPackageId} not found.");

                    foreach (var pkgItemDto in pkgDto.PackageItems)
                    {
                        var orderPkgItem = orderPackage.Items
                                               .FirstOrDefault(x => x.ItemId == pkgItemDto.RentalOrderPackageItemId)
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
            await _rentalOrderItemRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
            await _rentalOrderPackageRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
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
        await _rentalOrderItemRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
        await _rentalOrderPackageRepository.UpdateStatusByOrderIdAsync(order.Id, order.Status);
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

        // var item = await _itemRepository.GetByIdAsync(itemId.Value);
        var itemResponse = await _itemClient.GetFromJsonAsync<ItemResponseDto>($"/api/item/getbyid/{itemId.Value}"); // Adjust the endpoint as necessary
        var item = itemResponse;

        if (item == null)
            return;

        // --- REPAIR ---
        if (repairQty > 0)
        {
            //********* Check with shen again**********
            var UpdateMaintenance = await _itemMaintenanceClient.PostAsJsonAsync("/api/itemmaintenance", new
            {
                ItemId = item.Id,
                RentalOrderId = orderId,
                Type = 0, // Repair
                Quantity = repairQty,
                Remarks = "Auto-generated repair record on return"
            });
            if (!UpdateMaintenance.IsSuccessStatusCode)
                throw new InvalidOperationException("Failed to record item maintenance.");
        }

            // await _itemMaintenanceRepository.AddAsync(new ItemMaintenance
            // {
            //     ItemId = item.Id,
            //     RentalOrderId = orderId,
            //     Type = ItemMaintenanceType.Repair,
            //     Quantity = repairQty,
            //     CreatedById = userId,
            //     CreatedAt = DateTime.UtcNow,
            //     Remarks = "Auto-generated repair record on return"
            // });

        // --- BROKEN ---
        if (damagedQty > 0)
        {
            // await _itemMaintenanceRepository.AddAsync(new ItemMaintenance
            // {
            //     ItemId = item.Id,
            //     RentalOrderId = orderId,
            //     Type = ItemMaintenanceType.Broken,
            //     Quantity = damagedQty,
            //     CreatedById = userId,
            //     CreatedAt = DateTime.UtcNow,
            //     Remarks = "Auto-generated broken record on return"
            // });
            var UpdateMaintenance = await _itemMaintenanceClient.PostAsJsonAsync("/api/itemmaintenance", new
            {
                ItemId = item.Id,
                RentalOrderId = orderId,
                Type = 1, // Broken
                Quantity = damagedQty,
                Remarks = "Auto-generated broken record on return"
            });
            if (!UpdateMaintenance.IsSuccessStatusCode)
                throw new InvalidOperationException("Failed to record item maintenance.");
            // Decrease item quantity permanently
            item.Quantity = Math.Max(0, item.Quantity - damagedQty);
            // item.UpdatedAt = DateTime.UtcNow;   update on item side
            // item.UpdatedById = userId;
            var itemUpdateResponse = await _itemClient.PutAsJsonAsync($"/api/item/{item.Id}", item); // Adjust the endpoint as necessary
            if (!itemUpdateResponse.IsSuccessStatusCode)
                throw new InvalidOperationException("Failed to update item quantity.");
            // _itemRepository.Update(item);
        }

        // --- LOST ---
        if (lostQty > 0)
        {
            // await _itemMaintenanceRepository.AddAsync(new ItemMaintenance
            // {
            //     ItemId = item.Id,
            //     RentalOrderId = orderId,
            //     Type = ItemMaintenanceType.Lost,
            //     Quantity = lostQty,
            //     CreatedById = userId,
            //     CreatedAt = DateTime.UtcNow,
            //     Remarks = "Auto-generated lost record on return"
            // });
            var UpdateMaintenance = await _itemMaintenanceClient.PostAsJsonAsync("/api/itemmaintenance", new
            {
                ItemId = item.Id,
                RentalOrderId = orderId,
                Type = 2, // Lost
                Quantity = lostQty,
                Remarks = "Auto-generated lost record on return"
            });
            if (!UpdateMaintenance.IsSuccessStatusCode)
                throw new InvalidOperationException("Failed to record item maintenance.");

            // Decrease item quantity permanently
            item.Quantity = Math.Max(0, item.Quantity - lostQty);
            // item.UpdatedAt = DateTime.UtcNow;   update on item side
            // item.UpdatedById = userId;
            var itemUpdateResponse = await _itemClient.PutAsJsonAsync($"/api/item/{item.Id}", item); // Adjust the endpoint as necessary
            if (!itemUpdateResponse.IsSuccessStatusCode)
                throw new InvalidOperationException("Failed to update item quantity.");
            // _itemRepository.Update(item);
        }
    }


    private static void ValidateReturnedQuantitiesAsync(RentalOrder order, ReturnRentalOrderRequestDto dto)
    {
        foreach (var itemDto in dto.Items ?? Enumerable.Empty<ReturnItemConditionDto>())
        {
            var orderItem = order.RentalOrderItems.FirstOrDefault(x => x.ItemId == itemDto.RentalOrderItemId);
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
            var orderPackage = order.RentalOrderPackages.FirstOrDefault(x => x.PackageId == pkgDto.RentalOrderPackageId);
            if (orderPackage == null)
                throw new InvalidOperationException($"Package {pkgDto.RentalOrderPackageId} not found.");

            foreach (var pkgItemDto in pkgDto.PackageItems)
            {
                var pkgItem = orderPackage.Items.FirstOrDefault(x => x.ItemId == pkgItemDto.RentalOrderPackageItemId);
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
