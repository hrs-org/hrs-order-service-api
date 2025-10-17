using AutoMapper;
using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.Domain.Entities;

namespace HRS.API.Mappings.Profiles;

public class RentalOrderProfile : Profile
{
    public RentalOrderProfile()
    {
        // Create → Entity (request DTO → entity)
        CreateMap<CreateRentalOrderRequestDto, RentalOrder>()
            .ForMember(dest => dest.RentalOrderItems, opt => opt.Ignore())
            .ForMember(dest => dest.RentalOrderPackages, opt => opt.Ignore())
            .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedById, opt => opt.Ignore());

        CreateMap<RentalOrderItemRequestDto, RentalOrderItem>()
            .ForMember(dest => dest.ItemNameSnapshot, opt => opt.Ignore())
            .ForMember(dest => dest.DailyRateSnapshot, opt => opt.Ignore())
            .ForMember(dest => dest.RentalOrderId, opt => opt.Ignore());

        CreateMap<RentalOrderPackageRequestDto, RentalOrderPackage>()
            .ForMember(dest => dest.PackageNameSnapshot, opt => opt.Ignore())
            .ForMember(dest => dest.DailyRateSnapshot, opt => opt.Ignore())
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ForMember(dest => dest.RentalOrderId, opt => opt.Ignore());

        // Entity → Response DTO (detail view)
        CreateMap<RentalOrder, RentalOrderResponseDto>()
            .ForMember(dest => dest.GuestName,
                opt => opt.MapFrom(src => src.Customer != null ? src.Customer.FirstName : src.GuestName))
            .ForMember(dest => dest.GuestEmail,
                opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Email : src.GuestEmail))
            .ForMember(dest => dest.GuestPhone,
                opt => opt.MapFrom(src => src.Customer != null ? "-" : src.GuestPhone))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Channel, opt => opt.MapFrom(src => src.Channel.ToString()))
            .ForMember(dest => dest.PaymentType, opt => opt.MapFrom(src => src.PaymentType.ToString()))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.RentalOrderItems))
            .ForMember(dest => dest.Packages, opt => opt.MapFrom(src => src.RentalOrderPackages));

        CreateMap<RentalOrderItem, RentalOrderItemDto>()
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId))
            .ForMember(dest => dest.ItemNameSnapshot, opt => opt.MapFrom(src => src.ItemNameSnapshot))
            .ForMember(dest => dest.DailyRateSnapshot, opt => opt.MapFrom(src => src.DailyRateSnapshot))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));

        CreateMap<RentalOrderPackage, RentalOrderPackageDto>()
            .ForMember(dest => dest.PackageId, opt => opt.MapFrom(src => src.PackageId))
            .ForMember(dest => dest.PackageNameSnapshot, opt => opt.MapFrom(src => src.PackageNameSnapshot))
            .ForMember(dest => dest.DailyRateSnapshot, opt => opt.MapFrom(src => src.DailyRateSnapshot))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        CreateMap<RentalOrderPackageItem, RentalOrderPackageItemDto>()
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId))
            .ForMember(dest => dest.ItemNameSnapshot, opt => opt.MapFrom(src => src.ItemNameSnapshot))
            .ForMember(dest => dest.QuantityPerPackageSnapshot, opt => opt.MapFrom(src => src.QuantityPerPackageSnapshot));

        // Entity → List DTO (summary view)
        CreateMap<RentalOrder, RentalOrderListDto>()
            .ForMember(dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.Customer != null ? src.Customer.FirstName : src.GuestName))
            .ForMember(dest => dest.CustomerPhone,
                opt => opt.MapFrom(src => src.Customer != null ? "-" : src.GuestPhone));
    }
}
