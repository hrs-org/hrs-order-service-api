using AutoMapper;
using HRS.API.Contracts.DTOs.Package;
using HRS.Domain.Entities;

namespace HRS.API.Mappings.Profiles;

public class PackageProfile : Profile
{
    public PackageProfile()
    {
        // Map requests → entity
        CreateMap<AddPackageRequestDto, Package>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.PackageItems, opt => opt.MapFrom(s => s.Items ?? new List<PackageItemRequestDto>()))
            .ForMember(d => d.PackageRates, opt => opt.MapFrom(s => s.Rates ?? new List<PackageRateRequestDto>()))
            .ForMember(d => d.CreatedById, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedById, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdatePackageRequestDto, Package>()
            .ForMember(d => d.PackageItems, opt => opt.MapFrom(s => s.Items))
            .ForMember(d => d.PackageRates, opt => opt.MapFrom(s => s.Rates))
            .ForMember(d => d.CreatedById, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedById, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore());

        CreateMap<PackageItemRequestDto, PackageItem>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.PackageId, opt => opt.Ignore())
            .ForMember(d => d.Package, opt => opt.Ignore())
            .ForMember(d => d.Item, opt => opt.Ignore());

        CreateMap<PackageRateRequestDto, PackageRate>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.PackageId, opt => opt.Ignore())
            .ForMember(d => d.Package, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.CreatedById, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedById, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore());

        // Map entity → response
        CreateMap<Package, PackageResponseDto>()
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.PackageItems))
            .ForMember(d => d.Rates, opt => opt.MapFrom(s => s.PackageRates));

        CreateMap<PackageItem, PackageItemResponseDto>()
            .ForMember(d => d.ItemName, opt => opt.MapFrom(s => s.Item!.Name));

        CreateMap<PackageRate, PackageRateResponseDto>();
    }
}
