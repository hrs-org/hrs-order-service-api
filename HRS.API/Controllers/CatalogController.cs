using HRS.API.Contracts.DTOs;
using HRS.API.Contracts.DTOs.Catalog;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRS.API.Controllers;

[ApiController]
[Route("api/catalogs")]
[Authorize]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    public async Task<ActionResult<CatalogResponseDto>> GetAvailability([FromQuery] GetCatalogRequestDto request)
    {
        var result = await _catalogService.GetStoreAvailabilityAsync(request.StartDate, request.EndDate);
        return Ok(ApiResponse<CatalogResponseDto>.OkResponse(result));
    }
}
