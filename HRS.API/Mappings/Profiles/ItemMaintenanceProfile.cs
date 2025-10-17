using AutoMapper;
using HRS.API.Contracts.DTOs.Maintenance;
using HRS.Domain.Entities;

namespace HRS.API.Mappings.Profiles;

public class ItemMaintenanceProfile : Profile
{
    public ItemMaintenanceProfile()
    {
        CreateMap<ItemMaintenance, ItemMaintenanceResponseDto>()
            .ForMember(i => i.QuantityFixed, opt => opt.Ignore())
            .ForMember(i => i.Remarks, opt => opt.Ignore())
            .ForMember(i => i.RentalOrderId, opt => opt.Ignore())
            .ForMember(i => i.Type, opt => opt.MapFrom(src => src.Type.ToString()));
    }
}
