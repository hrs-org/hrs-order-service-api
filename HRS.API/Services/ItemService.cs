using AutoMapper;
using HRS.API.Contracts.DTOs.Item;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;

namespace HRS.API.Services;

public class ItemService : IItemService
{
    private const string ItemNotFound = "Item not found";
    private readonly IItemRateRepository _itemRateRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IMapper _mapper;
    private readonly IUserContextService _userContextService;

    public ItemService(IMapper mapper, IUserContextService userContextService, IItemRepository itemRepository, IItemRateRepository itemRateRepository)
    {
        _mapper = mapper;
        _userContextService = userContextService;
        _itemRepository = itemRepository;
        _itemRateRepository = itemRateRepository;
    }

    public async Task<ItemResponseDto> GetItemAsync(int id)
    {
        var item = await _itemRepository.GetByIdWithChildrenAsync(id) ?? throw new KeyNotFoundException(ItemNotFound);
        return _mapper.Map<ItemResponseDto>(item);
    }

    public async Task<IEnumerable<ItemResponseDto>> GetRootItemsAsync()
    {
        var items = await _itemRepository.GetRootItemsAsync();
        return _mapper.Map<IEnumerable<ItemResponseDto>>(items);
    }

    public async Task<ItemResponseDto> CreateAsync(AddItemRequestDto dto)
    {
        var user = await _userContextService.GetUserAsync();

        await using var tx = await _itemRepository.BeginTransactionAsync();

        try
        {
            var entity = _mapper.Map<Item>(dto);

            entity.CreatedById = user.Id;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedById = user.Id;
            entity.UpdatedAt = DateTime.UtcNow;

            if (entity.Children.Count > 0)
            {
                entity.Quantity = entity.Children.Count > 0 ? entity.Children.Sum(c => c.Quantity) : 0;
                foreach (var child in entity.Children)
                {
                    child.CreatedById = user.Id;
                    child.CreatedAt = DateTime.UtcNow;
                    child.UpdatedById = user.Id;
                    child.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (entity.Rates.Count > 0)
                foreach (var rate in entity.Rates)
                {
                    rate.CreatedById = user.Id;
                    rate.CreatedAt = DateTime.UtcNow;
                    rate.UpdatedById = user.Id;
                    rate.UpdatedAt = DateTime.UtcNow;
                }

            await _itemRepository.AddAsync(entity);
            await _itemRepository.SaveChangesAsync();

            await SyncItemRatesAsync(entity, dto.Rates, user.Id);
            await _itemRateRepository.SaveChangesAsync();

            await tx.CommitAsync();

            return _mapper.Map<ItemResponseDto>(entity);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ItemResponseDto> UpdateAsync(UpdateItemRequestDto dto)
    {
        if (!dto.Id.HasValue) throw new KeyNotFoundException(ItemNotFound);

        var user = await _userContextService.GetUserAsync();

        await using var tx = await _itemRepository.BeginTransactionAsync();

        try
        {
            var item = await _itemRepository.GetByIdWithChildrenAsync(dto.Id.Value)
                       ?? throw new KeyNotFoundException(ItemNotFound);

            item.Name = dto.Name;
            item.Description = dto.Description;
            item.Price = dto.Price;
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedById = user.Id;

            var existingChild = dto.Children?.Where(c => c.Id is not null)
                .ToDictionary(c => c.Id ?? 0) ?? [];
            var newChildren = dto.Children?.Where(c => c.Id is null).ToList() ?? [];

            // Update existing children
            foreach (var child in item.Children.ToList())
                if (existingChild.TryGetValue(child.Id, out var dtoChild))
                {
                    child.Name = dtoChild.Name;
                    child.Description = dtoChild.Description;
                    child.Quantity = dtoChild.Quantity;
                    child.Price = dtoChild.Price;
                    child.UpdatedAt = DateTime.UtcNow;
                    child.UpdatedById = user.Id;

                    existingChild.Remove(child.Id);
                }
                else
                {
                    _itemRepository.Remove(child);
                }

            // Add new children
            foreach (var dtoChild in newChildren)
            {
                var newChild = new Item
                {
                    Name = dtoChild.Name,
                    Description = dtoChild.Description,
                    Quantity = dtoChild.Quantity,
                    Price = dtoChild.Price,
                    ParentId = item.Id,
                    CreatedById = user.Id,
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedById = user.Id,
                    UpdatedAt = DateTime.UtcNow
                };
                item.Children.Add(newChild);
            }

            // Recalculate parent quantity
            item.Quantity = item.Children.Count > 0 ? item.Children.Sum(c => c.Quantity) : 0;

            await SyncItemRatesAsync(item, dto.Rates, user.Id);

            await _itemRepository.SaveChangesAsync();
            await _itemRateRepository.SaveChangesAsync();

            await tx.CommitAsync();

            return _mapper.Map<ItemResponseDto>(item);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _itemRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException(ItemNotFound);
        await _itemRepository.RemoveItem(item);
    }

    public async Task<decimal> GetItemRateAsync(int itemId, int rentalDays)
    {
        var rate = await _itemRateRepository.GetApplicableRateAsync(itemId, rentalDays);
        if (rate == null)
            throw new InvalidOperationException("No applicable rate found for this item");

        return rate.DailyRate;
    }

    private async Task SyncItemRatesAsync(Item item, ICollection<ItemRateRequestDto>? rates, int userId)
    {
        // Load existing rates for this item
        var existingRates = (await _itemRateRepository.GetRatesByItemIdAsync(item.Id, false)).ToList();

        if (rates == null || rates.Count == 0)
        {
            // If no incoming rates, remove all existing rates
            if (existingRates.Count > 0)
                _itemRateRepository.RemoveRange(existingRates);
            return;
        }

        // Map incoming rates (no IDs, just values)
        foreach (var dto in rates)
        {
            var match = existingRates.FirstOrDefault(r => r.MinDays == dto.MinDays);
            if (match != null)
            {
                // Update existing
                match.DailyRate = dto.DailyRate;
                match.IsActive = dto.IsActive;
                match.UpdatedAt = DateTime.UtcNow;
                match.UpdatedById = userId;
            }
            else
            {
                // Create new
                var newRate = new ItemRate
                {
                    ItemId = item.Id,
                    MinDays = dto.MinDays,
                    DailyRate = dto.DailyRate,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                };
                await _itemRateRepository.AddAsync(newRate);
            }
        }

        // Remove any obsolete rates not in the new list
        var toRemove = existingRates
            .Where(r => rates.All(dto => dto.MinDays != r.MinDays))
            .ToList();

        if (toRemove.Count > 0)
            _itemRateRepository.RemoveRange(toRemove);
    }

    public async Task<IEnumerable<ItemResponseDto>> SearchItemsAsync(string? keyword)
    {
        var items = await _itemRepository.SearchAsync(keyword);
        return _mapper.Map<IEnumerable<ItemResponseDto>>(items);
    }
}
