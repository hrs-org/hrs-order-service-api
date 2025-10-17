using HRS.API.Contracts.DTOs;
using HRS.API.Contracts.DTOs.Package;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRS.API.Controllers;

[ApiController]
[Route("api/packages")]
public class PackageController : ControllerBase
{
    private readonly IPackageService _packageService;

    public PackageController(IPackageService packageService)
    {
        _packageService = packageService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<IEnumerable<PackageResponseDto>>> GetPackagesAsync()
    {
        var res = await _packageService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<PackageResponseDto>>.OkResponse(res, "Packages retrieved successfully"));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<PackageResponseDto>> GetPackageAsync(int id)
    {
        var res = await _packageService.GetByIdAsync(id);
        return Ok(ApiResponse<PackageResponseDto>.OkResponse(res, "Package retrieved successfully"));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<PackageResponseDto>>> CreatePackageAsync([FromBody] AddPackageRequestDto request)
    {
        var created = await _packageService.CreateAsync(request);
        return Ok(ApiResponse<PackageResponseDto>.OkResponse(created, "Package created successfully"));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> UpdatePackageAsync(int id, [FromBody] UpdatePackageRequestDto request)
    {
        request.Id = id;
        var updated = await _packageService.UpdateAsync(request);
        return Ok(ApiResponse<PackageResponseDto>.OkResponse(updated, "Package updated successfully"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> DeletePackageAsync(int id)
    {
        await _packageService.DeleteAsync(id);
        return Ok(ApiResponse<string>.OkResponse(null, $"Package {id} deleted successfully"));
    }
}
