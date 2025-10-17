using HRS.API.Contracts.DTOs.Item;

namespace HRS.API.Services.Interfaces;

public interface IItemService
{
    Task<ItemResponseDto> GetItemAsync(int id);
    Task<IEnumerable<ItemResponseDto>> GetRootItemsAsync();
    Task<ItemResponseDto> CreateAsync(AddItemRequestDto dto);
    Task<ItemResponseDto> UpdateAsync(UpdateItemRequestDto dto);
    Task DeleteAsync(int id);
    Task<decimal> GetItemRateAsync(int itemId, int rentalDays);
    Task<IEnumerable<ItemResponseDto>> SearchItemsAsync(string? keyword);
}
