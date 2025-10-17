using AutoMapper;
using FluentAssertions;
using HRS.API.Contracts.DTOs.Item;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using NSubstitute;

namespace HRS.Test.API.Services;

public class ItemServiceTests
{
    private readonly IItemRateRepository _itemRateRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IMapper _mapper;
    private readonly ItemService _service;
    private readonly IUserContextService _userContextService;

    public ItemServiceTests()
    {
        _itemRepository = Substitute.For<IItemRepository>();
        _itemRateRepository = Substitute.For<IItemRateRepository>();
        _mapper = Substitute.For<IMapper>();
        _userContextService = Substitute.For<IUserContextService>();
        _service = new ItemService(_mapper, _userContextService, _itemRepository, _itemRateRepository);
    }

    [Fact]
    public async Task GetItemAsync_WhenItemExists_ReturnsMappedDto()
    {
        // Arrange
        var user = new User { Id = 1 };
        var item = new Item
        {
            Id = 1,
            Name = "Tent",
            Description = "This is tent",
            Quantity = 14,
            Price = 10,
            CreatedBy = user,
            Children =
            [
                new Item
                {
                    Id = 2,
                    Name = "Size XL",
                    Description = "This is tent size XL",
                    Quantity = 14,
                    Price = 0,
                    ParentId = 1,
                    CreatedBy = user
                }
            ]
        };
        var dto = new ItemResponseDto
        {
            Id = 1,
            Name = "Tent",
            Description = "This is tent",
            Quantity = 14,
            Price = 10,
            Children =
            [
                new ItemResponseDto
                {
                    Id = 2,
                    Name = "Size XL",
                    Description = "This is tent size XL",
                    Quantity = 14,
                    Price = 0
                }
            ]
        };
        _itemRepository.GetByIdWithChildrenAsync(1).Returns(item);
        _mapper.Map<ItemResponseDto>(item).Returns(dto);

        // Act
        var result = await _service.GetItemAsync(1);

        // Assert
        result.Should().BeEquivalentTo(dto);
        result.Id.Should().Be(1);
        result.Name.Should().Be("Tent");
        result.Description.Should().Be("This is tent");
        result.Quantity.Should().Be(14);
        result.Price.Should().Be(10);
        result.Children.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetItemAsync_WhenItemNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _ = _itemRepository.GetByIdWithChildrenAsync(1)!.Returns((Item)null!);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetItemAsync(1));
    }

    [Fact]
    public async Task GetItemsAsync_ReturnsMappedDtos()
    {
        // Arrange
        var user = new User { Id = 1 };
        var items = new List<Item>
        {
            new() { Id = 1, Name = "Item1", Description = "Desc1", CreatedBy = user },
            new() { Id = 2, Name = "Item2", Description = "Desc2", CreatedBy = user }
        };
        var dtos = new List<ItemResponseDto> { new() { Id = 1 }, new() { Id = 2 } };
        _itemRepository.GetRootItemsAsync().Returns(items);
        _mapper.Map<IEnumerable<ItemResponseDto>>(items).Returns(dtos);

        // Act
        var result = await _service.GetRootItemsAsync();

        // Assert
        result.Should().BeEquivalentTo(dtos);
    }

    [Fact]
    public async Task CreateItemAsync_WithChildren_SetsQuantityAndSaves()
    {
        // Arrange
        var addDto = new AddItemRequestDto
        {
            Name = "Shoes",
            Description = "This is shoes",
            Quantity = 5,
            Price = 10,
            Children = new List<AddItemRequestDto>
            {
                new() { Name = "Size 8", Description = "This is Size 8", Quantity = 2, Price = 10.5m },
                new() { Name = "Size 10", Description = "This is Size 10", Quantity = 3, Price = 10.5m }
            }
        };
        var entityUser = new User { Id = 42 };
        var entity = new Item
        {
            Name = "Shoes",
            Description = "This is shoes",
            Quantity = 5,
            Price = 10,
            CreatedBy = entityUser,
            Children = new List<Item>
            {
                new() { Name = "Size 8", Description = "This is Size 8", Quantity = 2, CreatedBy = entityUser },
                new() { Name = "Size 10", Description = "This is Size 10", Quantity = 3, CreatedBy = entityUser }
            }
        };
        var responseDto = new ItemResponseDto
        {
            Name = "Shoes",
            Description = "This is shoes",
            Quantity = 5,
            Price = 10,
            Children = new List<ItemResponseDto>
            {
                new() { Name = "Size 8", Quantity = 2 },
                new() { Name = "Size 10", Quantity = 3 }
            }
        };
        var user = new User { Id = 42 };
        _mapper.Map<Item>(addDto).Returns(entity);
        _mapper.Map<ItemResponseDto>(entity).Returns(responseDto);
        _userContextService.GetUserAsync().Returns(user);

        // Act
        var result = await _service.CreateAsync(addDto);

        // Assert
        await _itemRepository.Received(1).AddAsync(entity);
        await _itemRepository.Received(1).SaveChangesAsync();
        result.Should().BeEquivalentTo(responseDto);
        result.Name.Should().Be(addDto.Name);
        result.Description.Should().Be(addDto.Description);
        result.Quantity.Should().Be(addDto.Quantity);
        result.Price.Should().Be(addDto.Price);
        result.Children.Should().HaveCount(addDto.Children.Count);
    }

    [Fact]
    public async Task UpdateItemAsync_UpdatesFieldsAndChildren()
    {
        // Arrange
        var existingUser = new User { Id = 99 };
        var existing = new Item
        {
            Id = 1,
            Name = "Old",
            Description = "OldDesc",
            Quantity = 1,
            Price = 10,
            CreatedBy = existingUser,
            Children = new List<Item>
            {
                new() { Id = 2, Name = "Child", Quantity = 1, Price = 5, Description = "desc", CreatedBy = existingUser },
                new() { Id = 3, Name = "Child2", Quantity = 1, Price = 10, Description = "desc", CreatedBy = existingUser }
            }
        };
        var dto = new UpdateItemRequestDto
        {
            Id = 1,
            Name = "New",
            Description = "NewDesc",
            Quantity = 2,
            Price = 20,
            Children = new List<UpdateItemRequestDto>
            {
                new() { Id = 2, Name = "ChildUpdated", Description = "desc2", Quantity = 2, Price = 6 },
                new() { Name = "NewChild", Description = "desc3", Quantity = 3, Price = 7 }
            }
        };
        var user = new User { Id = 99 };
        _itemRepository.GetByIdWithChildrenAsync(1).Returns(existing);
        _userContextService.GetUserAsync().Returns(user);

        // Act
        await _service.UpdateAsync(dto);

        // Assert
        await _itemRepository.Received(1).SaveChangesAsync();
        _itemRepository.Received(1).Remove(Arg.Any<Item>());
    }

    [Fact]
    public async Task UpdateItemAsync_WhenItemNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _itemRepository.GetByIdWithChildrenAsync(1).Returns((Item)null!);
        var dto = new UpdateItemRequestDto { Id = 1, Name = "Test", Description = "This is Test", Quantity = 2, Price = 10.5m };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(dto));
    }

    [Fact]
    public async Task UpdateItemAsync_WhenDtoDoesntHaveId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new UpdateItemRequestDto { Id = 0, Name = "Test", Description = "This is Test", Quantity = 2, Price = 10.5m };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(dto));
    }

    [Fact]
    public async Task DeleteItemAsync_RemovesAndSaves()
    {
        // Arrange
        var user = new User { Id = 1 };
        var item = new Item { Id = 1, Name = "Test", Description = "Test", CreatedBy = user };
        _itemRepository.GetByIdAsync(1).Returns(item);

        // Act
        await _service.DeleteAsync(1);

        // Assert
        await _itemRepository.Received(1).RemoveItem(item);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenItemNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _itemRepository.GetByIdAsync(1).Returns((Item)null!);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(1));
    }

    [Fact]
    public async Task GetItemRateAsync_WhenRateExists_ReturnsDailyRate()
    {
        // Arrange
        var rate = new ItemRate { Id = 1, ItemId = 1, MinDays = 1, DailyRate = 123.45m, IsActive = true };
        _itemRateRepository.GetApplicableRateAsync(1, 5).Returns(rate);

        // Act
        var result = await _service.GetItemRateAsync(1, 5);

        // Assert
        result.Should().Be(123.45m);
    }

    [Fact]
    public async Task GetItemRateAsync_WhenNoRate_ThrowsInvalidOperationException()
    {
        // Arrange
        _itemRateRepository.GetApplicableRateAsync(1, 5).Returns((ItemRate)null!);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetItemRateAsync(1, 5));
    }

    [Fact]
    public async Task CreateItemAsync_WithRates_SavesRatesAndItem()
    {
        // Arrange
        var addDto = new AddItemRequestDto
        {
            Name = "Tent",
            Description = "A tent",
            Quantity = 2,
            Price = 100,
            Rates = new List<ItemRateRequestDto>
            {
                new() { MinDays = 1, DailyRate = 10, IsActive = true },
                new() { MinDays = 3, DailyRate = 8, IsActive = true }
            }
        };
        var user = new User { Id = 5 };
        var entity = new Item
        {
            Name = "Tent",
            Description = "A tent",
            Quantity = 2,
            Price = 100,
            CreatedBy = user,
            Rates = new List<ItemRate>
            {
                new() { MinDays = 1, DailyRate = 10, IsActive = true },
                new() { MinDays = 3, DailyRate = 8, IsActive = true }
            }
        };
        var responseDto = new ItemResponseDto { Name = "Tent", Description = "A tent", Quantity = 2, Price = 100 };
        _mapper.Map<Item>(addDto).Returns(entity);
        _mapper.Map<ItemResponseDto>(entity).Returns(responseDto);
        _userContextService.GetUserAsync().Returns(user);
        _itemRateRepository.GetRatesByItemIdAsync(Arg.Any<int>()).Returns(new List<ItemRate>());

        // Act
        var result = await _service.CreateAsync(addDto);

        // Assert
        await _itemRepository.Received(1).AddAsync(entity);
        await _itemRepository.Received(1).SaveChangesAsync();
        await _itemRateRepository.Received(1).AddAsync(Arg.Is<ItemRate>(r => r.MinDays == 1 && r.DailyRate == 10));
        await _itemRateRepository.Received(1).AddAsync(Arg.Is<ItemRate>(r => r.MinDays == 3 && r.DailyRate == 8));
        await _itemRateRepository.Received(1).SaveChangesAsync();
        result.Should().BeEquivalentTo(responseDto);
    }

    [Fact]
    public async Task UpdateItemAsync_WithRates_UpdatesRatesAndItem()
    {
        // Arrange
        var user = new User { Id = 7 };
        var existing = new Item
        {
            Id = 1,
            Name = "Old",
            Description = "OldDesc",
            Quantity = 1,
            Price = 10,
            CreatedBy = user,
            Children = new List<Item>(),
            Rates = new List<ItemRate> { new() { Id = 1, ItemId = 1, MinDays = 1, DailyRate = 10, IsActive = true } }
        };
        var dto = new UpdateItemRequestDto
        {
            Id = 1,
            Name = "New",
            Description = "NewDesc",
            Quantity = 2,
            Price = 20,
            Rates = new List<ItemRateRequestDto>
            {
                new() { MinDays = 1, DailyRate = 15, IsActive = true },
                new() { MinDays = 5, DailyRate = 7, IsActive = true }
            }
        };
        _itemRepository.GetByIdWithChildrenAsync(1).Returns(existing);
        _userContextService.GetUserAsync().Returns(user);
        _itemRateRepository.GetRatesByItemIdAsync(1).Returns(existing.Rates);
        _mapper.Map<ItemResponseDto>(existing).Returns(new ItemResponseDto { Id = 1, Name = "New", Description = "NewDesc", Quantity = 2, Price = 20 });

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        await _itemRepository.Received(1).SaveChangesAsync();
        await _itemRateRepository.Received(1).AddAsync(Arg.Is<ItemRate>(r => r.MinDays == 5 && r.DailyRate == 7));
        await _itemRateRepository.Received(1).SaveChangesAsync();
        result.Name.Should().Be("New");
        result.Description.Should().Be("NewDesc");
        result.Price.Should().Be(20);
    }

    [Fact]
    public async Task SearchItemsAsync_WithKeyword_ReturnsMatchingItems()
    {
        // Arrange
        var items = new List<Item> { new() { Id = 1, Name = "Trekking Pole" } };
        _itemRepository.SearchAsync(Arg.Any<string?>()).Returns(items);
        _mapper.Map<IEnumerable<ItemResponseDto>>(items).Returns(new List<ItemResponseDto> { new() { Id = 1, Name = "Trekking Pole" } });

        // Act
        var result = await _service.SearchItemsAsync("Trekking Pole");

        // Assert
        result.Should().ContainSingle(i => i.Name == "Trekking Pole");
    }

    [Fact]
    public async Task SearchItemsAsync_WithNoKeyword_ReturnsAllItems()
    {
        // Arrange
        var items = new List<Item>
        {
            new() { Id = 1, Name = "Trekking Pole" },
            new() { Id = 2, Name = "Tent" }
        };
        _itemRepository.SearchAsync(Arg.Any<string?>()).Returns(items);
        _mapper.Map<IEnumerable<ItemResponseDto>>(items).Returns(new List<ItemResponseDto>
        {
            new() { Id = 1, Name = "Trekking Pole" },
            new() { Id = 2, Name = "Tent" }
        });

        // Act
        var result = await _service.SearchItemsAsync(null);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchItemsAsync_WithNoResults_ReturnsEmpty()
    {
        // Arrange
        var items = new List<Item>();
        _itemRepository.SearchAsync(Arg.Any<string?>()).Returns(items);
        _mapper.Map<IEnumerable<ItemResponseDto>>(items).Returns(new List<ItemResponseDto>());

        // Act
        var result = await _service.SearchItemsAsync("NonExistentGear");

        // Assert
        result.Should().BeEmpty();
    }
}
