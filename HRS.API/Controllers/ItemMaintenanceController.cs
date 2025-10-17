using HRS.API.Contracts.DTOs;
using HRS.API.Contracts.DTOs.Maintenance;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRS.API.Controllers;

[ApiController]
[Route("api/item-maintenances")]
[Authorize(Roles = "Employee,Manager,Admin")]
public class ItemMaintenanceController : ControllerBase
{
    private readonly IItemMaintenanceService _itemMaintenanceService;

    public ItemMaintenanceController(IItemMaintenanceService itemMaintenanceService)
    {
        _itemMaintenanceService = itemMaintenanceService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemMaintenanceResponseDto>>> GetAll()
    {
        var result = await _itemMaintenanceService.GetAllAsync();
        return Ok(ApiResponse<List<ItemMaintenanceResponseDto>>.OkResponse(result.ToList()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemMaintenanceResponseDto>> GetById(int id)
    {
        var result = await _itemMaintenanceService.GetAsync(id);
        return Ok(ApiResponse<ItemMaintenanceResponseDto>.OkResponse(result));
    }

    [HttpPost("{id:int}/fix")]
    public async Task<ActionResult<ItemMaintenanceResponseDto>> MarkAsFixed(int id, [FromBody] ItemMaintenanceRequestDto request)
    {
        request.Id = id;
        var result = await _itemMaintenanceService.MarkAsFixedAsync(request);
        return Ok(ApiResponse<ItemMaintenanceResponseDto>.OkResponse(result, "Item maintenance marked as fixed successfully"));
    }
}
