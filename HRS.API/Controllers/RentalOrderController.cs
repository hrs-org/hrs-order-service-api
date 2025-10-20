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

    [HttpGet("{id:int}")]
    // [Authorize]
    public async Task<ActionResult<RentalOrderResponseDto>> GetById(int id)
    {
        var result = await _rentalOrderService.GetAsync(id);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result));
    }

    [HttpGet]
    // [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<IEnumerable<RentalOrderListDto>>> GetAll()
    {
        var result = await _rentalOrderService.GetAllAsync();
        return Ok(ApiResponse<List<RentalOrderListDto>>.OkResponse(result.ToList()));
    }

    [HttpGet("bookings")]
    // [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<IEnumerable<RentalOrderResponseDto>>> GetAllBookings()
    {
        var bookingStatuses = new[] { RentalStatus.Pending, RentalStatus.Booked, RentalStatus.Cancelled, RentalStatus.PendingPayment };

        var result = await _rentalOrderService.GetByStatusesAsync(bookingStatuses);
        return Ok(ApiResponse<List<RentalOrderResponseDto>>.OkResponse(result.ToList()));
    }

    [HttpGet("rents")]
    // [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<IEnumerable<RentalOrderResponseDto>>> GetAllRents()
    {
        var rentStatuses = new[] { RentalStatus.Rented };

        var result = await _rentalOrderService.GetByStatusesAsync(rentStatuses);
        return Ok(ApiResponse<List<RentalOrderResponseDto>>.OkResponse(result.ToList()));
    }

    [HttpPost]
    // [Authorize]
    public async Task<ActionResult<RentalOrderResponseDto>> Create([FromBody] CreateRentalOrderRequestDto dto)
    {
        var result = await _rentalOrderService.CreateAsync(dto);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order created successfully"));
    }

    [HttpPut("{id:int}/approve-payment")]
    // [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> ApprovePayment(int id)
    {
        var result = await _rentalOrderService.ApproveAsync(id);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order approved successfully"));
    }

    [HttpPut("{id:int}/approve")]
    // [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> Approve(int id)
    {
        var result = await _rentalOrderService.ApproveAsync(id);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order approved successfully"));
    }

    [HttpPut("{id:int}/cancel")]
    // [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> CancelOrder(int id)
    {
        var result = await _rentalOrderService.CancelAsync(id);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order cancelled successfully"));
    }

    [HttpPut("{id:int}/confirm")]
    // [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> MarkAsRented(int id)
    {
        var result = await _rentalOrderService.MarkAsRentedAsync(id);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order marked as rented successfully"));
    }

    [HttpPut("{id:int}/return")]
    // [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> Return(int id, [FromBody] ReturnRentalOrderRequestDto dto)
    {
        var result = await _rentalOrderService.ReturnAsync(id, dto);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order returned successfully"));
    }

    [HttpPut("{id:int}/close")]
    // [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> Close(int id)
    {
        var result = await _rentalOrderService.CloseAsync(id);
        return Ok(ApiResponse<RentalOrderResponseDto>.OkResponse(result, "Order closed successfully"));
    }

    [HttpPost("/api/orders/assign-stripe-sessionid")]
    // [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> AssignStripeSessionId([FromBody] AssignStripeSessionRequest request)
    {
        var id = request.OrderId;
        var sessionId = request.SessionId;
        {
            await _rentalOrderService.AssignStripeSessionIdAsync(id, sessionId);
            return Ok(ApiResponse<string>.OkResponse("Stripe Session ID assigned successfully"));
        }

    }

    [HttpPost("/api/orders/testdb")]
    // [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<ActionResult<RentalOrderResponseDto>> TestDatabase( int CustomerId , string GuestName , string GuestEmail , string GuestPhone,int totalAmount)
    {
        var newdata = await _rentalOrderService.testcreatDB( CustomerId , GuestName , GuestEmail , GuestPhone , totalAmount);
        return Ok(ApiResponse<RentalOrder>.OkResponse(newdata, "Test successful"));

    }
}
