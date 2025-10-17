using AutoMapper;
using HRS.API.Contracts.DTOs.Package;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;

namespace HRS.API.Services;

public class PackageService : IPackageService
{
    private const string PackageNotFound = "Package not found";
    private readonly IMapper _mapper;
    private readonly IPackageRateRepository _packageRateRepository;

    private readonly IPackageRepository _packageRepository;
    private readonly IUserContextService _userContextService;

    public PackageService(
        IMapper mapper,
        IUserContextService userContextService,
        IPackageRepository packageRepository,
        IPackageRateRepository packageRateRepository)
    {
        _mapper = mapper;
        _userContextService = userContextService;
        _packageRepository = packageRepository;
        _packageRateRepository = packageRateRepository;
    }

    public async Task<IEnumerable<PackageResponseDto>> GetAllAsync()
    {
        var packages = await _packageRepository.GetAllWithDetailsAsync();
        return _mapper.Map<IEnumerable<PackageResponseDto>>(packages);
    }

    public async Task<PackageResponseDto> GetByIdAsync(int id)
    {
        var package = await _packageRepository.GetByIdWithDetailsAsync(id)
                      ?? throw new KeyNotFoundException(PackageNotFound);
        return _mapper.Map<PackageResponseDto>(package);
    }

    public async Task<PackageResponseDto> CreateAsync(AddPackageRequestDto dto)
    {
        var user = await _userContextService.GetUserAsync();
        await using var trx = await _packageRepository.BeginTransactionAsync();

        try
        {
            var entity = _mapper.Map<Package>(dto);
            entity.CreatedById = user.Id;
            entity.UpdatedById = user.Id;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            InitializePackageRates(entity, user.Id);

            await _packageRepository.AddAsync(entity);
            await _packageRepository.SaveChangesAsync();

            await trx.CommitAsync();

            return _mapper.Map<PackageResponseDto>(entity);
        }
        catch
        {
            await trx.RollbackAsync();
            throw;
        }
    }

    public async Task<PackageResponseDto> UpdateAsync(UpdatePackageRequestDto dto)
    {
        var user = await _userContextService.GetUserAsync();
        await using var trx = await _packageRepository.BeginTransactionAsync();

        try
        {
            var package = await _packageRepository.GetByIdWithDetailsAsync(dto.Id)
                          ?? throw new KeyNotFoundException(PackageNotFound);

            package.Name = dto.Name;
            package.Description = dto.Description;
            package.BasePrice = dto.BasePrice;
            package.UpdatedById = user.Id;
            package.UpdatedAt = DateTime.UtcNow;

            SyncPackageItemsAsync(package, dto.Items);
            await SyncPackageRatesAsync(package, dto.Rates, user.Id);

            await _packageRepository.SaveChangesAsync();
            await trx.CommitAsync();

            return _mapper.Map<PackageResponseDto>(package);
        }
        catch
        {
            await trx.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id)
                      ?? throw new KeyNotFoundException(PackageNotFound);

        _packageRepository.Remove(package);
        await _packageRepository.SaveChangesAsync();
    }

    private static void SyncPackageItemsAsync(Package package, ICollection<PackageItemRequestDto>? items)
    {
        if (items == null || items.Count == 0)
        {
            package.PackageItems.Clear();
            return;
        }

        // Load existing
        var existing = package.PackageItems.ToList();

        // Update or Add
        foreach (var dto in items)
        {
            var match = existing.FirstOrDefault(pi => pi.ItemId == dto.ItemId);
            if (match != null)
            {
                match.Quantity = dto.Quantity;
                existing.Remove(match);
            }
            else
            {
                var newItem = new PackageItem
                {
                    PackageId = package.Id,
                    ItemId = dto.ItemId,
                    Quantity = dto.Quantity
                };
                package.PackageItems.Add(newItem);
            }
        }

        // Remove old ones not in request
        foreach (var leftover in existing)
            package.PackageItems.Remove(leftover);
    }

    private async Task SyncPackageRatesAsync(Package package, ICollection<PackageRateRequestDto>? rates, int userId)
    {
        if (rates == null || rates.Count == 0)
        {
            package.PackageRates.Clear();
            return;
        }

        var existingRates = (await _packageRateRepository.GetRatesByPackageIdAsync(package.Id)).ToList();

        foreach (var dto in rates)
        {
            var match = existingRates.FirstOrDefault(r => r.MinDays == dto.MinDays);
            if (match != null)
            {
                match.DailyRate = dto.DailyRate;
                match.IsActive = dto.IsActive;
                match.UpdatedAt = DateTime.UtcNow;
                match.UpdatedById = userId;
            }
            else
            {
                var newRate = new PackageRate
                {
                    PackageId = package.Id,
                    MinDays = dto.MinDays,
                    DailyRate = dto.DailyRate,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                };
                await _packageRateRepository.AddAsync(newRate);
            }
        }

        // Remove obsolete ones
        var toRemove = existingRates
            .Where(r => rates.All(dto => dto.MinDays != r.MinDays))
            .ToList();

        if (toRemove.Count > 0)
            _packageRateRepository.RemoveRange(toRemove);
    }

    private static void InitializePackageRates(Package package, int userId)
    {
        foreach (var rate in package.PackageRates)
        {
            rate.CreatedById = userId;
            rate.CreatedAt = DateTime.UtcNow;
            rate.IsActive = true;
        }
    }
}
