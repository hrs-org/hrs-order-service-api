using FluentAssertions;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Interfaces;
using HRS.Shared.Core.Dtos;
using NSubstitute;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace HRS.Test.API.Services;

public class AvailabilityServiceTests
{
    private readonly IRentalOrderMongoDBRepository _rentalRepo;
    private readonly HttpClient _itemClient;
    private readonly HttpClient _itemMaintenanceClient;
    private readonly AvailabilityService _service;

    public AvailabilityServiceTests()
    {
        _rentalRepo = Substitute.For<IRentalOrderMongoDBRepository>();

        var itemHandler = new HttpMessageHandlerStub();
        _itemClient = new HttpClient(itemHandler)
        {
            BaseAddress = new Uri("http://itemservice")
        };

        var maintenanceHandler = new HttpMessageHandlerStub();
        _itemMaintenanceClient = new HttpClient(maintenanceHandler)
        {
            BaseAddress = new Uri("http://itemmaintservice")
        };

        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("ItemService").Returns(_itemClient);
        httpFactory.CreateClient("ItemMaintenanceService").Returns(_itemMaintenanceClient);

        _service = new AvailabilityService(_rentalRepo, httpFactory);
    }

    [Fact]
    public async Task ReturnsCorrectAvailability_WhenItemPassedDirectly()
    {
        var item = new ItemResponseDto { Id = "item1", Quantity = 10 };
        _rentalRepo.GetReservedQuantityAsync(item.Id, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(3);
        HttpMessageHandlerStub.ResponseMap[$"/api/item-maintenances/items/{item.Id}/repair/quantity"] =
            new ApiResponse<int> { Data = 2 };

        var available = await _service.GetAvailableQuantityAsync(item.Id, DateTime.Today, DateTime.Today, item);

        available.Should().Be(5); // 10 - (3+2)
    }

    [Fact]
    public async Task ReturnsCorrectAvailability_WhenItemFetchedFromApi()
    {
        var item = new ItemResponseDto { Id = "item2", Quantity = 8 };
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{item.Id}"] = new ApiResponse<ItemResponseDto> { Data = item };
        _rentalRepo.GetReservedQuantityAsync(item.Id, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(2);
        HttpMessageHandlerStub.ResponseMap[$"/api/item-maintenances/items/{item.Id}/repair/quantity"] =
            new ApiResponse<int> { Data = 1 };

        var available = await _service.GetAvailableQuantityAsync(item.Id, DateTime.Today, DateTime.Today);

        available.Should().Be(5); // 8 - (2+1)
    }

    [Fact]
    public async Task ReturnsCorrectAvailability_WhenParentItemFetched()
    {
        var item = new ItemResponseDto { Id = "item3", Quantity = 5 };
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{item.Id}"] = new ApiResponse<ItemResponseDto> { Data = null };
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{item.Id}/parent"] = new ApiResponse<ItemResponseDto> { Data = item };
        _rentalRepo.GetReservedQuantityAsync(item.Id, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(1);
        HttpMessageHandlerStub.ResponseMap[$"/api/item-maintenances/items/{item.Id}/repair/quantity"] =
            new ApiResponse<int> { Data = 1 };

        var available = await _service.GetAvailableQuantityAsync(item.Id, DateTime.Today, DateTime.Today);

        available.Should().Be(3); // 5 - (1+1)
    }

    [Fact]
    public async Task Throws_WhenItemNotFoundAnywhere()
    {
        var missingId = "missing";
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{missingId}"] = new ApiResponse<ItemResponseDto> { Data = null };
        HttpMessageHandlerStub.ResponseMap[$"/api/items/{missingId}/parent"] = new ApiResponse<ItemResponseDto> { Data = null };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetAvailableQuantityAsync(missingId, DateTime.Today, DateTime.Today));
    }
}

// Stub for HttpClient GetFromJsonAsync
internal class HttpMessageHandlerStub : HttpMessageHandler
{
    public static readonly Dictionary<string, object> ResponseMap = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        if (ResponseMap.TryGetValue(request.RequestUri!.PathAndQuery, out var responseObj))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(responseObj);
            var message = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(message);
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
