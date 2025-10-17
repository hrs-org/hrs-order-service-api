using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.Domain.Enums;

namespace HRS.API.Services.Interfaces;

public interface IRentalOrderService
{
    Task<RentalOrderResponseDto> GetAsync(int id);
    Task<IEnumerable<RentalOrderListDto>> GetAllAsync();
    Task<IEnumerable<RentalOrderResponseDto>> GetByStasusesAsync(RentalStatus[] statuses);
    Task<RentalOrderResponseDto> CreateAsync(CreateRentalOrderRequestDto dto);
    Task<RentalOrderResponseDto> ApproveAsync(int id);
    Task<RentalOrderResponseDto> CancelAsync(int id);
    Task AssignStripeSessionIdAsync(int orderId, string sessionId);
    Task<RentalOrderResponseDto> ApprovePaymentAsync(string sessionId, long? amount);
    Task<RentalOrderResponseDto> MarkAsRentedAsync(int id);
    Task<RentalOrderResponseDto> ReturnAsync(int id, ReturnRentalOrderRequestDto dto);
    Task<RentalOrderResponseDto> CloseAsync(int id);
}
