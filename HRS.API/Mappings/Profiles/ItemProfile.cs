using AutoMapper;
using HRS.API.Contracts.DTOs.Item;
using HRS.Domain.Entities;

namespace HRS.API.Mappings.Profiles;

public class ItemProfile : Profile
{
    public ItemProfile()
    {
        CreateMap<ItemRateRequestDto, ItemRate>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Item, opt => opt.Ignore())
            .ForMember(d => d.ItemId, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.CreatedById, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedById, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore());

        CreateMap<AddItemRequestDto, Item>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ParentId, opt => opt.Ignore())
            .ForMember(d => d.Parent, opt => opt.Ignore())
            .ForMember(d => d.Children, opt => opt.MapFrom(s => s.Children))
            .ForMember(d => d.Rates, opt => opt.MapFrom(s => s.Rates))
            .ForMember(d => d.CreatedById, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedById, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateItemRequestDto, Item>()
            .ForMember(d => d.ParentId, opt => opt.Ignore())
            .ForMember(d => d.Parent, opt => opt.Ignore())
            .ForMember(d => d.Children, opt => opt.MapFrom(s => s.Children))
            .ForMember(d => d.Rates, opt => opt.MapFrom(s => s.Rates))
            .ForMember(d => d.CreatedById, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedById, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore());

        CreateMap<ItemRate, ItemRateResponseDto>();

        CreateMap<Item, ItemResponseDto>()
            .ForMember(d => d.Children, opt => opt.MapFrom(s => s.Children))
            .ForMember(d => d.Rates, opt => opt.MapFrom(s => s.Rates));
    }
}
