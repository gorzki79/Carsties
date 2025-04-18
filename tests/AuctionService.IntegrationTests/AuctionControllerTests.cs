using System;
using System.Net;
using System.Net.Http.Json;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

[Collection("SharedCollection")]
public class AuctionControllerTests : IAsyncLifetime
{
    private CustomWebAppFactory _factory;
    private HttpClient _httpClient;

    private readonly string GT_ID = "afbee524-5972-4075-8800-7d1f9d7b0a0c";

    public AuctionControllerTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        DbHelper.ReinitDbForTests(db);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetAuctions_ReturnsAll3Auctions()
    {
        // Arrange

        // Act
        var response = await _httpClient.GetAsync("api/auctions");
        response.EnsureSuccessStatusCode();

        // Assert
        var auctions = await response.Content.ReadFromJsonAsync<List<AuctionDto>>();
        Assert.Equal(3, auctions.Count);

    }

    [Fact]
    public async Task GetAuctionById_WithValidId_ReturnsAuction()
    {
        // Arrange


        // Act
        var response = await _httpClient.GetAsync($"api/auctions/{GT_ID}");
        response.EnsureSuccessStatusCode();

        // Assert
        var auction = await response.Content.ReadFromJsonAsync<AuctionDto>();
        Assert.Equal(new Guid(GT_ID), auction.Id);
        Assert.Equal("Ford", auction.Make);
        Assert.Equal("GT", auction.Model);

    }

    [Fact]
    public async Task GetAuctionById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange

        // Act
        var response = await _httpClient.GetAsync($"api/auctions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    }

    [Fact]
    public async Task GetAuctionById_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange

        // Act
        var response = await _httpClient.GetAsync($"api/auctions/notaguid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    }

    [Fact]
    public async Task CreateAuction_WithNoAuth_ReturnsUnauthorized()
    {
        // Arrange
        var auction = new CreateAuctionDto
        {
            Make = "Test",
            Model = "Test",
            Color = "Test",
            Mileage = 100,
        };
        // Act
        var response = await _httpClient.PostAsync("api/auctions", JsonContent.Create(auction));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithValidCreateDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var auction = GetAuctionForCreate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // Act
        var response = await _httpClient.PostAsync("api/auctions", JsonContent.Create(auction));

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdAuction = await response.Content.ReadFromJsonAsync<AuctionDto>();
        Assert.Equal("bob", createdAuction.Seller);
        Assert.Equal("Test", createdAuction.Make);
        Assert.Equal("Test", createdAuction.Model);
        Assert.Equal("Test", createdAuction.Color);
        Assert.Equal(100, createdAuction.Mileage);
        Assert.Equal(1990, createdAuction.Year);
        Assert.Equal(10000, createdAuction.ReservePrice);
        Assert.Equal("https://test.com/test.jpg", createdAuction.ImageUrl);
    }

    [Fact]
    public async Task CreateAuction_WithInvalidCreateAuctionDto_ShouldReturnBadRequest()
    {
        // arrange
        var auction = GetAuctionForCreate();
        auction.Make = null;
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PostAsync("api/auctions", JsonContent.Create(auction));
        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndUser_ShouldReturnOk()
    {
        // arrange
        var auction = GetAuctionForCreate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PutAsync($"api/auctions/{GT_ID}", JsonContent.Create(auction));

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndInvalidUser_ShouldReturnForbidden()
    {
        // arrange 
        var auction = GetAuctionForCreate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("notbob"));

        // act
        var response = await _httpClient.PutAsync($"api/auctions/{GT_ID}", JsonContent.Create(auction));

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private CreateAuctionDto GetAuctionForCreate()
    {
        return new CreateAuctionDto
        {
            Make = "Test",
            Model = "Test",
            Color = "Test",
            Mileage = 100,
            Year = 1990,
            ReservePrice = 10000,
            ImageUrl = "https://test.com/test.jpg",
        };
    }
}
