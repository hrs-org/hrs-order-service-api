using AutoMapper;
using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using MongoDB.Bson;

namespace HRS.API.Mappings.Profiles;

public class RentalOrderProfile : Profile
{
    public RentalOrderProfile()
    {
        CreateMap<RentalOrderMongoDB, RentalOrderResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Channel, opt => opt.MapFrom(src => src.Channel.ToString()))
            .ForMember(dest => dest.PaymentType, opt => opt.MapFrom(src => src.PaymentType.ToString()))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.RentalOrderItems))
            .ForMember(dest => dest.Packages, opt => opt.MapFrom(src => src.RentalOrderPackages));
        CreateMap<Item, RentalOrderItemDto>()
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId));
        CreateMap<Package, RentalOrderPackageDto>()
            .ForMember(dest => dest.PackageId, opt => opt.MapFrom(src => src.PackageId))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.PackageItems));
        CreateMap<PackageItem, RentalOrderPackageItemDto>()
           .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId));
        CreateMap<RentalOrderMongoDB, RentalOrderListDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.GuestName))
                .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.GuestPhone))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Channel, opt => opt.MapFrom(src => src.Channel))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.ApprovedAt, opt => opt.MapFrom(src => src.ApprovedAt))
                .ForMember(dest => dest.ReturnedAt, opt => opt.MapFrom(src => src.ReturnedAt))
                .ForMember(dest => dest.ClosedAt, opt => opt.MapFrom(src => src.ClosedAt))
                .ForMember(dest => dest.StoreId, opt => opt.MapFrom(src => src.StoreId));
        CreateMap<CreateRentalOrderRequestDto, RentalOrderMongoDB>()
                .ForMember(dest => dest.StoreId, opt => opt.MapFrom(src => src.StoreId))
                .ForMember(dest => dest.GuestName, opt => opt.MapFrom(src => src.GuestName ?? string.Empty))
                .ForMember(dest => dest.GuestPhone, opt => opt.MapFrom(src => src.GuestPhone ?? string.Empty))
                .ForMember(dest => dest.GuestEmail, opt => opt.MapFrom(src => src.GuestEmail ?? string.Empty))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.RentalOrderItems, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.RentalOrderPackages, opt => opt.MapFrom(src => src.Packages));

        // ─────────────────────────────
        // RentalOrderItemRequestDto → Item
        // ─────────────────────────────
        CreateMap<RentalOrderItemRequestDto, Item>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => ObjectId.GenerateNewId().ToString()))
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId ?? string.Empty))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));

        // ─────────────────────────────
        // RentalOrderPackageRequestDto → Package
        // ─────────────────────────────
        CreateMap<RentalOrderPackageRequestDto, Package>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => ObjectId.GenerateNewId().ToString()))
            .ForMember(dest => dest.PackageId, opt => opt.MapFrom(src => src.PackageId ?? string.Empty))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));

        // ─────────────────────────────
        // RentalOrderPackageItemRequestDto → PackageItem
        // ─────────────────────────────
        CreateMap<RentalOrderPackageItemRequestDto, PackageItem>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.PackageItemId ?? ObjectId.GenerateNewId().ToString()))
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.SelectedItemId ?? string.Empty));

        CreateMap<RentalOrderResponseDto, RentalOrderMongoDB>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Channel, opt => opt.MapFrom(src => src.Channel.ToString()))
            .ForMember(dest => dest.PaymentType, opt => opt.MapFrom(src => src.PaymentType.ToString()))
            .ForMember(dest => dest.RentalOrderItems, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.RentalOrderPackages, opt => opt.MapFrom(src => src.Packages));
        CreateMap<RentalOrderItemDto, Item>()
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId));
        CreateMap<RentalOrderPackageDto, Package>()
            .ForMember(dest => dest.PackageId, opt => opt.MapFrom(src => src.PackageId))
            .ForMember(dest => dest.PackageItems, opt => opt.MapFrom(src => src.Items));
        CreateMap<RentalOrderPackageItemDto, PackageItem>()
           .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId));


    }

    private static int? SafeToNullableInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return null;
        }
    }


}
