using System.Collections.ObjectModel;
using System.Data;
using AutoMapper;
using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using HRS.Shared.Core.Dtos;
using HRS.Shared.Core.Enums;
using HRS.Shared.Core.Interfaces;
using Microsoft.VisualBasic;
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
    private readonly IUserContextService _userContextService;
    private readonly IRentalOrderMongoDBRepository _rentalOrderMongoDBRepository;

    public RentalOrderService(
        IMapper mapper,
        IUserContextService userContextService,
        IAvailabilityService availabilityService,
        IHttpClientFactory httpClientFactory,
        IRentalOrderMongoDBRepository rentalOrderMongoDBRepository
    )
    {
        _mapper = mapper;
        _userContextService = userContextService;
        _availabilityService = availabilityService;
        _itemMaintenanceClient = httpClientFactory.CreateClient("ItemMaintenanceService");
        _paymentClient = httpClientFactory.CreateClient("PaymentService");
        _itemClient = httpClientFactory.CreateClient("ItemService");
        _rentalOrderMongoDBRepository = rentalOrderMongoDBRepository;
    }

    public async Task<RentalOrderResponseDto> GetAsync(string id)
    {
        var order = await _rentalOrderMongoDBRepository.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException(OrderNotFound);

        return _mapper.Map<RentalOrderResponseDto>(order);
    }

    public async Task<IEnumerable<RentalOrderListDto>> GetAllAsync()
    {
        var orders = await _rentalOrderMongoDBRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<RentalOrderListDto>>(orders);
    }

    public async Task<IEnumerable<RentalOrderResponseDto>> GetByStatusesAsync(RentalStatus[] statuses)
    {
        var storeId = _userContextService.GetStoreId();
        var orders = await _rentalOrderMongoDBRepository.GetByStatusesAndStoreId(statuses, storeId);
        return _mapper.Map<IEnumerable<RentalOrderResponseDto>>(orders);
    }

    public async Task<RentalOrderResponseDto> CreateAsync(CreateRentalOrderRequestDto dto)
    {
        var user = await _userContextService.GetUserAsync();

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
                var pkgResponse = await _itemClient.GetFromJsonAsync<ApiResponse<PackageResponseDto>>($"/api/packages/{pkgDto.PackageId}"); // Adjust the endpoint as necessary
                if (pkgResponse == null || pkgResponse.Data == null || pkgResponse.Data.Items == null)
                    throw new KeyNotFoundException($"Package {pkgDto.PackageId} not found.");
                var pkg = pkgResponse.Data;
                foreach (var pi in pkg.Items)
                {
                    var required = pi.Quantity * pkgDto.Quantity;
                    var available = await _availabilityService.GetAvailableQuantityAsync(pi.ItemId, dto.StartDate, dto.EndDate);

                    if (available < required)
                        throw new InvalidOperationException(
                            $"Package '{pkg.Name}' unavailable — insufficient '{pi.ItemName}' (required {required}, available {available}).");
                }
            }

        var entity = _mapper.Map<RentalOrderMongoDB>(dto);
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
        entity.RentalOrderPackages = new Collection<Package>();
        entity.RentalOrderItems = new Collection<Item>();
        var totalAmount = 0m;

        if (dto.Items is not null)
            foreach (var itemDto in dto.Items)
            {
                var itemResponse = await _itemClient.GetFromJsonAsync<ApiResponse<ItemResponseDto>>($"/api/items/{itemDto.ItemId}"); // Adjust the endpoint as necessary
                var item = itemResponse?.Data;
                if (item == null)
                    throw new KeyNotFoundException($"Item {itemDto.ItemId} not found.");
                var ParentId = item.ParentId;
                var applicableRate = null as ItemRateResponseDto;
                var parentResponse = null as ApiResponse<ItemResponseDto>;
                if (ParentId != item.Id)
                {
                    parentResponse = await _itemClient.GetFromJsonAsync<ApiResponse<ItemResponseDto>>($"/api/items/{ParentId}"); // Adjust the endpoint as necessary
                    if (parentResponse == null || parentResponse.Data == null)
                        throw new KeyNotFoundException($"Parent item {ParentId} not found.");


                    foreach (var dummyRate in parentResponse.Data.Rates!)
                    {
                        if (dummyRate.MinDays <= rentalDays)
                        {
                            applicableRate = dummyRate;
                        }

                    }

                }
                else
                {
                    foreach (var dummyRate in item.Rates!)
                    {
                        if (dummyRate.MinDays <= rentalDays)
                        {
                            applicableRate = dummyRate;
                        }

                    }
                }
                var rate = applicableRate;
                var dailyRate = rate?.DailyRate ?? parentResponse?.Data?.Price ?? item.Price;

                entity.RentalOrderItems.Add(new Item
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

                var pkg = await _itemClient.GetFromJsonAsync<ApiResponse<PackageResponseDto>>($"/api/packages/{pkgDto.PackageId}"); // Adjust the endpoint as necessary
                if (pkg == null || pkg.Data == null || pkg.Data.Items == null)
                    throw new KeyNotFoundException($"Package {pkgDto.PackageId} not found.");

                var rate = null as PackageRateResponseDto;
                foreach (var dummyRate in pkg.Data.Rates!)
                {
                    if (dummyRate.MinDays <= rentalDays)
                    {
                        rate = dummyRate;
                    }

                }
                var dailyRate = rate?.DailyRate ?? pkg.Data.BasePrice;

                var rentalPkg = new Package
                {
                    PackageId = pkg.Data.Id,
                    PackageNameSnapshot = pkg.Data.Name,
                    DailyRateSnapshot = dailyRate,
                    Quantity = pkgDto.Quantity
                };
                if (pkgDto.SelectedItems != null)
                {

                    foreach (var pi in pkgDto.SelectedItems)
                    {
                        var finalItem = null as ItemResponseDto;
                        foreach (var pkgItemDto in pkg.Data.Items)
                        {

                            var itemResponse = await _itemClient.GetFromJsonAsync<ApiResponse<ItemResponseDto>>($"/api/items/{pkgItemDto.ItemId}"); // Adjust the endpoint as necessary
                            if (itemResponse == null || itemResponse.Data == null)
                                throw new KeyNotFoundException($"Item {pkgItemDto.ItemId} not found.");
                            var item = itemResponse.Data;

                            finalItem = item?.Children?.FirstOrDefault(c => c.Id == pi.SelectedItemId);
                            if (finalItem != null)
                                rentalPkg.PackageItems.Add(new PackageItem
                                {
                                    ItemId = finalItem.Id,
                                    ItemNameSnapshot = finalItem.Name,
                                    QuantityPerPackageSnapshot = pkgDto.Quantity
                                }
                            );
                        }
                    }
                }

                entity.RentalOrderPackages.Add(rentalPkg);
                totalAmount += dailyRate * (pkgDto?.Quantity ?? 1) * rentalDays;
            }


        entity.TotalAmount = totalAmount;

        await _rentalOrderMongoDBRepository.AddAsync(entity);


        if (entity.PaymentType == OrderPaymentType.Cash)
        {
            var paymentId = await _paymentClient.PostAsJsonAsync("/api/payments", new
            {
                OrderId = entity.Id,
                Amount = entity.TotalAmount,
                SessionId = "User-Cash-Payment",
                PaymentType = 1, // Cash
                Status = 1, // Completed
            });
            if (!paymentId.IsSuccessStatusCode)
            {
                await _rentalOrderMongoDBRepository.RemoveAsync(entity.Id);
                throw new InvalidOperationException("Failed to record cash payment.");
            }

            var payment = await paymentId.Content.ReadFromJsonAsync<ApiResponse<string>>();
            entity.PaymentId = payment?.Data;

        }

        await _rentalOrderMongoDBRepository.UpdateAsync(entity, entity.Id);
        return _mapper.Map<RentalOrderResponseDto>(entity);

    }


    // add controller for this method
    public async Task AssignStripeSessionIdAsync(string orderId, string sessionId)
    {
        var order = await _rentalOrderMongoDBRepository.GetByIdAsync(orderId) ?? throw new KeyNotFoundException("Order not found");

        order.StripeSessionId = sessionId;
        await _rentalOrderMongoDBRepository.UpdateAsync(order, order.Id);
    }

    public async Task<RentalOrderResponseDto> ApprovePaymentAsync(string sessionId, int? amount)
    {
        var user = await _userContextService.GetUserAsync();

        var order = await _rentalOrderMongoDBRepository.GetByStripeSessionIdAsync(sessionId)
                    ?? throw new KeyNotFoundException(OrderNotFound);

        var response = await _paymentClient.GetFromJsonAsync<ApiResponse<object>>($"/api/payments/orders/{order.Id}"); // Adjust the endpoint as necessary
        var existingPayments = response?.Data;
        if (existingPayments != null)
            throw new DuplicateNameException("Payment has already been recorded for this order.");

        var paymentId = await _paymentClient.PostAsJsonAsync("/api/payments", new
        {
            OrderId = order.Id,
            Amount = amount ?? 0 ,
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

        await _rentalOrderMongoDBRepository.UpdateAsync(order, order.Id);

        return _mapper.Map<RentalOrderResponseDto>(order);

    }

    public async Task<RentalOrderResponseDto> ApproveAsync(string id)
    {
        var user = await _userContextService.GetUserAsync();

        var order = await _rentalOrderMongoDBRepository.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException(OrderNotFound);

        if (order.Status != RentalStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be approved.");

        order.Status = RentalStatus.Booked;
        order.ApprovedById = user.Id;
        order.ApprovedAt = DateTime.UtcNow;
        order.UpdatedById = user.Id;
        order.UpdatedAt = DateTime.UtcNow;

        await _rentalOrderMongoDBRepository.UpdateAsync(order, order.Id);
        return _mapper.Map<RentalOrderResponseDto>(order);

    }

    public async Task<RentalOrderResponseDto> CancelAsync(string id)
    {
        var user = await _userContextService.GetUserAsync();

        var order = await _rentalOrderMongoDBRepository.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException(OrderNotFound);

        if (order.Status != RentalStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be approved.");

        order.Status = RentalStatus.Cancelled;
        order.ApprovedById = user.Id;
        order.ApprovedAt = DateTime.UtcNow;
        order.UpdatedById = user.Id;
        order.UpdatedAt = DateTime.UtcNow;

        await _rentalOrderMongoDBRepository.UpdateAsync(order, order.Id);

        return _mapper.Map<RentalOrderResponseDto>(order);

    }

    public async Task<RentalOrderResponseDto> MarkAsRentedAsync(string id)
    {
        var user = await _userContextService.GetUserAsync();
        var order = await _rentalOrderMongoDBRepository.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException(OrderNotFound);

        if (order.Status != RentalStatus.Booked)
            throw new InvalidOperationException("Only booked orders can be marked as rented.");

        order.Status = RentalStatus.Rented;
        order.UpdatedById = user.Id;
        order.UpdatedAt = DateTime.UtcNow;

        await _rentalOrderMongoDBRepository.UpdateAsync(order, order.Id);
        return _mapper.Map<RentalOrderResponseDto>(order);

    }

    public async Task<RentalOrderResponseDto> ReturnAsync(string id, ReturnRentalOrderRequestDto dto)
    {
        var user = await _userContextService.GetUserAsync();
        var order = await _rentalOrderMongoDBRepository.GetByIdAsync(id)
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

                await HandleMaintenanceAsync(orderItem.ItemId, order.Id, itemCondition, user.Id, order.StoreId);
            }

        if (dto.Packages != null)
            foreach (var pkgDto in dto.Packages)
            {
                var orderPackage = order.RentalOrderPackages
                                       .FirstOrDefault(x => x.PackageId == pkgDto.RentalOrderPackageId)
                                   ?? throw new KeyNotFoundException($"RentalOrderPackage {pkgDto.RentalOrderPackageId} not found.");

                foreach (var pkgItemDto in pkgDto.PackageItems)
                {
                    var orderPkgItem = orderPackage.PackageItems
                                           .FirstOrDefault(x => x.ItemId == pkgItemDto.RentalOrderPackageItemId)
                                       ?? throw new KeyNotFoundException($"RentalOrderPackageItem {pkgItemDto.RentalOrderPackageItemId} not found.");

                    orderPkgItem.GoodQty = pkgItemDto.GoodQty;
                    orderPkgItem.RepairQty = pkgItemDto.RepairQty;
                    orderPkgItem.DamagedQty = pkgItemDto.DamagedQty;
                    orderPkgItem.LostQty = pkgItemDto.LostQty;

                    await HandleMaintenanceAsync(orderPkgItem.ItemId, order.Id, pkgItemDto, user.Id, order.StoreId);
                }
            }

        order.ItemsGoodCount = order.RentalOrderItems.Sum(i => i.GoodQty)
                               + order.RentalOrderPackages.SelectMany(p => p.PackageItems).Sum(i => i.GoodQty);

        order.ItemsIssueCount = order.RentalOrderItems.Count(i => i.HasIssues)
                                + order.RentalOrderPackages.SelectMany(p => p.PackageItems).Count(i => i.HasIssues);

        order.HasIssues = order.ItemsIssueCount > 0;
        order.Status = RentalStatus.Returned;
        order.ReturnedAt = DateTime.UtcNow;
        order.ReturnedById = user.Id;
        order.ReturnRemarks = dto.Remarks;
        order.UpdatedById = user.Id;
        order.UpdatedAt = DateTime.UtcNow;

        await _rentalOrderMongoDBRepository.UpdateAsync(order, order.Id);

        return _mapper.Map<RentalOrderResponseDto>(order);

    }


    public async Task<RentalOrderResponseDto> CloseAsync(string id)
    {
        var user = await _userContextService.GetUserAsync();
        var order = await _rentalOrderMongoDBRepository.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException(OrderNotFound);

        if (order.Status != RentalStatus.Returned)
            throw new InvalidOperationException("Only returned orders can be closed.");

        order.Status = RentalStatus.Completed;
        order.ClosedById = user.Id;
        order.ClosedAt = DateTime.UtcNow;
        order.UpdatedById = user.Id;
        order.UpdatedAt = DateTime.UtcNow;

        await _rentalOrderMongoDBRepository.UpdateAsync(order, order.Id);
        return _mapper.Map<RentalOrderResponseDto>(order);
    }

    private async Task HandleMaintenanceAsync(string? itemId, string orderId, object dto, int userId, int storeId)
    {
        if (itemId == null) return;

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

        var itemResponse = await _itemClient.GetFromJsonAsync<ApiResponse<ItemResponseDto>>($"/api/items/{itemId}"); // Adjust the endpoint as necessary
        var item = itemResponse;

        if (item == null || item.Data == null)
            return;

        // --- REPAIR ---
        var entities = new List<object>();
        if (repairQty > 0)
        {
            entities.Add(new { ItemId = item.Data.Id, RentalOrderId = orderId, Type = 0, Quantity = repairQty, Remarks = "Auto-generated repair record on return" });
        }
        // --- BROKEN ---
        if (damagedQty > 0)
        {
            entities.Add(new { ItemId = item.Data.Id, RentalOrderId = orderId, Type = 3, Quantity = damagedQty, Remarks = "Auto-generated broken record on return" });
            item.Data.Quantity = Math.Max(0, item.Data.Quantity - damagedQty);
        }

        // --- LOST ---
        if (lostQty > 0)
        {
            entities.Add(new { ItemId = item.Data.Id, RentalOrderId = orderId, Type = 2, Quantity = lostQty, Remarks = "Auto-generated lost record on return" });
            item.Data.Quantity = Math.Max(0, item.Data.Quantity - lostQty);
        }

        if (entities.Count <= 0) return;
        var batchPayload = new { Entries = entities };
        var resp = await _itemMaintenanceClient.PostAsJsonAsync("/api/item-maintenances/batch", batchPayload);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to record item maintenance batch.");
    }


    private static void ValidateReturnedQuantitiesAsync(RentalOrderMongoDB order, ReturnRentalOrderRequestDto dto)
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
                var pkgItem = orderPackage.PackageItems.FirstOrDefault(x => x.ItemId == pkgItemDto.RentalOrderPackageItemId);
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
