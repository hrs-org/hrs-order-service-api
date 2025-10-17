using HRS.API.Contracts.DTOs;
using HRS.API.Contracts.DTOs.Item;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRS.API.Controllers;

[ApiController]
[Route("api/items")]
public class ItemController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemController(IItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin, Manager")]
    public async Task<ActionResult<List<ItemResponseDto>>> GetItemsAsync()
    {
        var res = await _itemService.GetRootItemsAsync();
        return Ok(ApiResponse<List<ItemResponseDto>>.OkResponse(res.ToList()));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin, Manager")]
    public async Task<ActionResult<ItemResponseDto>> GetItemAsync(int id)
    {
        var res = await _itemService.GetItemAsync(id);
        return Ok(ApiResponse<ItemResponseDto>.OkResponse(res));
    }

    [HttpPost]
    [Authorize(Roles = "Admin, Manager")]
    public async Task<ActionResult> CreateItemAsync([FromBody] AddItemRequestDto request)
    {
        var createdItem = await _itemService.CreateAsync(request);

        return CreatedAtAction(
            "GetItem",
            new { id = createdItem.Id },
            ApiResponse<ItemResponseDto>.OkResponse(createdItem)
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin, Manager")]
    public async Task<ActionResult> UpdateItemAsync(int id, [FromBody] UpdateItemRequestDto request)
    {
        request.Id = id;
        await _itemService.UpdateAsync(request);
        return Ok(ApiResponse<object>.OkResponse(null, "Item updated successfully"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin, Manager")]
    public async Task<ActionResult> DeleteItemAsync(int id)
    {
        await _itemService.DeleteAsync(id);
        return Ok(ApiResponse<object>.OkResponse(null, "Item deleted successfully"));
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ItemResponseDto>>> SearchItemsAsync([FromQuery] string? keyword)
    {
        var res = await _itemService.SearchItemsAsync(keyword);
        return Ok(ApiResponse<List<ItemResponseDto>>.OkResponse(res.ToList()));
    }
}
