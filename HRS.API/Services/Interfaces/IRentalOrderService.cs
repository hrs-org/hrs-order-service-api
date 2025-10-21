using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.Domain.Entities;
using HRS.Domain.Enums;

namespace HRS.API.Services.Interfaces;

public interface IRentalOrderService
{
    Task<RentalOrderResponseDto> GetAsync(string id);
    Task<IEnumerable<RentalOrderListDto>> GetAllAsync();
    Task<IEnumerable<RentalOrderResponseDto>> GetByStatusesAsync(RentalStatus[] statuses, int storeId);
    Task<RentalOrderResponseDto> CreateAsync(CreateRentalOrderRequestDto dto);
    Task<RentalOrderResponseDto> ApproveAsync(string id);
    Task<RentalOrderResponseDto> CancelAsync(string id);
    Task AssignStripeSessionIdAsync(string orderId, string sessionId);
    Task<RentalOrderResponseDto> ApprovePaymentAsync(string sessionId, long? amount);
    Task<RentalOrderResponseDto> MarkAsRentedAsync(string id);
    Task<RentalOrderResponseDto> ReturnAsync(string id, ReturnRentalOrderRequestDto dto);
    Task<RentalOrderResponseDto> CloseAsync(string id);

}
