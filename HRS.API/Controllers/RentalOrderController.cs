using HRS.API.Contracts.DTOs;
using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRS.API.Controllers;

[ApiController]
[Route("api/orders")]
public class RentalOrderController : ControllerBase
{
    private readonly IRentalOrderService _rentalOrderService;

    public RentalOrderController(IRentalOrderService rentalOrderService)
    {
        _rentalOrderService = rentalOrderService;
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<RentalOrderResponseDto>> GetById(string id)
    {
        var result = await _rentalOrderService.GetAsync(id);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result));
    }

    [HttpGet]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<IEnumerable<RentalOrderListDto>>> GetAll()
    {
        var result = await _rentalOrderService.GetAllAsync();
        return Ok(ApiResponse<List<RentalOrderListDto>>.OkResponse(result.ToList()));
    }

    [HttpGet("bookings")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<IEnumerable<RentalOrderResponseDto>>> GetAllBookings()
    {
        var bookingStatuses = new[] { RentalStatus.Pending, RentalStatus.Booked, RentalStatus.Cancelled, RentalStatus.PendingPayment };
        var result = await _rentalOrderService.GetByStatusesAsync(bookingStatuses);
        return Ok(ApiResponse<List<RentalOrderResponseDto>>.OkResponse(result.ToList()));
    }

    [HttpGet("rents")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<IEnumerable<RentalOrderResponseDto>>> GetAllRents()
    {
        var rentStatuses = new[] { RentalStatus.Rented };

        var result = await _rentalOrderService.GetByStatusesAsync(rentStatuses);
        return Ok(ApiResponse<List<RentalOrderResponseDto>>.OkResponse(result.ToList()));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<RentalOrderResponseDto>> Create([FromBody] CreateRentalOrderRequestDto dto)
    {
        var result = await _rentalOrderService.CreateAsync(dto);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order created successfully"));
    }

    [HttpPut("{id}/approve-payment")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> ApprovePayment([FromBody] ApprovePaymentRequest request)
    {
        var id = request.SessionId;
        var amount = request.Amount;
        var result = await _rentalOrderService.ApprovePaymentAsync(id, amount);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order approved successfully"));
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> Approve(string id)
    {
        var result = await _rentalOrderService.ApproveAsync(id);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order approved successfully"));
    }

    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> CancelOrder(string id)
    {
        var result = await _rentalOrderService.CancelAsync(id);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order cancelled successfully"));
    }

    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> MarkAsRented(string id)
    {
        var result = await _rentalOrderService.MarkAsRentedAsync(id);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order marked as rented successfully"));
    }

    [HttpPut("{id}/return")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> Return(string id, [FromBody] ReturnRentalOrderRequestDto dto)
    {
        var result = await _rentalOrderService.ReturnAsync(id, dto);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order returned successfully"));
    }

    [HttpPut("{id}/close")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> Close(string id)
    {
        var result = await _rentalOrderService.CloseAsync(id);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order closed successfully"));
    }

    [HttpPost("/api/orders/assign-stripe-sessionid/{orderId}")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> AssignStripeSessionId([FromBody] AssignStripeSessionRequest request)
    {
        var id = request.OrderId;
        var sessionId = request.SessionId;
        {
            await _rentalOrderService.AssignStripeSessionIdAsync(id, sessionId);
            return Ok(ApiResponse<string>.OkResponse("Stripe Session ID assigned successfully"));
        }

    }


}
