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
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Channel, opt => opt.MapFrom(src => src.Channel.ToString()))
                .ForMember(dest => dest.PaymentType, opt => opt.MapFrom(src => src.PaymentType.ToString()))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.RentalOrderItems))
                .ForMember(dest => dest.Packages, opt => opt.MapFrom(src => src.RentalOrderPackages))
                .ForMember(dest => dest.StoreId, opt => opt.MapFrom(src => src.StoreId));
        CreateMap<Item, RentalOrderItemDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId))
                .ForMember(dest => dest.ItemNameSnapshot, opt => opt.MapFrom(src => src.ItemNameSnapshot))
                .ForMember(dest => dest.DailyRateSnapshot, opt => opt.MapFrom(src => src.DailyRateSnapshot))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));
        CreateMap<Package, RentalOrderPackageDto>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(dest => dest.PackageId, opt => opt.MapFrom(src => src.PackageId))
               .ForMember(dest => dest.PackageNameSnapshot, opt => opt.MapFrom(src => src.PackageNameSnapshot))
               .ForMember(dest => dest.DailyRateSnapshot, opt => opt.MapFrom(src => src.DailyRateSnapshot))
               .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
               .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.PackageItems));
        CreateMap<PackageItem, RentalOrderPackageItemDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId))
                .ForMember(dest => dest.ItemNameSnapshot, opt => opt.MapFrom(src => src.ItemNameSnapshot))
                .ForMember(dest => dest.QuantityPerPackageSnapshot, opt => opt.MapFrom(src => src.QuantityPerPackageSnapshot));

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
                // .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status = RentalStatus.Pending))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.RentalOrderItems, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.RentalOrderPackages, opt => opt.MapFrom(src => src.Packages));
        // .ForAllOtherMembers(opt => opt.Ignore());

        // ─────────────────────────────
        // RentalOrderItemRequestDto → Item
        // ─────────────────────────────
        CreateMap<RentalOrderItemRequestDto, Item>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => ObjectId.GenerateNewId().ToString()))
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId ?? string.Empty))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));
        // .ForAllOtherMembers(opt => opt.Ignore());

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
        // .ForMember(dest => dest.QuantityPerPackageSnapshot, opt => opt.Ignore());


        // Create → Entity (request DTO → entity)
        // CreateMap<CreateRentalOrderRequestDto, RentalOrderMongoDB>()
        //     .ForMember(dest => dest.RentalOrderItems, opt => opt.Ignore())
        //     .ForMember(dest => dest.RentalOrderPackages, opt => opt.Ignore())
        //     .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
        //     .ForMember(dest => dest.Status, opt => opt.Ignore())
        //     .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
        //     .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
        //     .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
        //     .ForMember(dest => dest.UpdatedById, opt => opt.Ignore());

        // CreateMap<RentalOrderItemRequestDto, RentalOrderItem>()
        //     .ForMember(dest => dest.ItemNameSnapshot, opt => opt.Ignore())
        //     .ForMember(dest => dest.DailyRateSnapshot, opt => opt.Ignore())
        //     .ForMember(dest => dest.RentalOrderId, opt => opt.Ignore());

        // CreateMap<RentalOrderPackageRequestDto, RentalOrderPackage>()
        //     .ForMember(dest => dest.PackageNameSnapshot, opt => opt.Ignore())
        //     .ForMember(dest => dest.DailyRateSnapshot, opt => opt.Ignore())
        //     .ForMember(dest => dest.Items, opt => opt.Ignore())
        //     .ForMember(dest => dest.RentalOrderId, opt => opt.Ignore());

        // Entity → Response DTO (detail view)
        // CreateMap<RentalOrderMongoDB, RentalOrderResponseDto>()


        // CreateMap<RentalOrderItem, RentalOrderItemDto>()
        //     .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId))
        //     .ForMember(dest => dest.ItemNameSnapshot, opt => opt.MapFrom(src => src.ItemNameSnapshot))
        //     .ForMember(dest => dest.DailyRateSnapshot, opt => opt.MapFrom(src => src.DailyRateSnapshot))
        //     .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));

        // CreateMap<RentalOrderPackage, RentalOrderPackageDto>()
        //     .ForMember(dest => dest.PackageId, opt => opt.MapFrom(src => src.PackageId))
        //     .ForMember(dest => dest.PackageNameSnapshot, opt => opt.MapFrom(src => src.PackageNameSnapshot))
        //     .ForMember(dest => dest.DailyRateSnapshot, opt => opt.MapFrom(src => src.DailyRateSnapshot))
        //     .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
        //     .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        // CreateMap<RentalOrderPackageItem, RentalOrderPackageItemDto>()
        //     .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId))
        //     .ForMember(dest => dest.ItemNameSnapshot, opt => opt.MapFrom(src => src.ItemNameSnapshot))
        //     .ForMember(dest => dest.QuantityPerPackageSnapshot, opt => opt.MapFrom(src => src.QuantityPerPackageSnapshot));

        // Entity → List DTO (summary view)
        // CreateMap<RentalOrder, RentalOrderListDto>()
        // .ForMember(dest => dest.CustomerName,
        //     opt => opt.MapFrom(src => src.Customer != null ? src.Customer.FirstName : src.GuestName))
        // .ForMember(dest => dest.CustomerPhone,
        //     opt => opt.MapFrom(src => src.Customer != null ? "-" : src.GuestPhone));
    }
}
