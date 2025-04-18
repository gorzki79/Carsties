using System;
using AuctionService.Controllers;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AuctionService.RequestHelpers;
using AuctionService.UnitTests.Utils;
using AutoFixture;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuctionService.UnitTests;

public class AuctionControllerTests
{
    private readonly Mock<IAuctionRepository> _auctionRepository;
    private readonly Mock<IPublishEndpoint> _publishEndpoint;
    private readonly Fixture _fixture;
    private readonly IMapper _mapper;
    private readonly AuctionsController _controller;

    public AuctionControllerTests()
    {
        _fixture = new Fixture();

        _auctionRepository = new Mock<IAuctionRepository>();
        _publishEndpoint = new Mock<IPublishEndpoint>();

        var mockMapper = new MapperConfiguration(mc =>
        {
            mc.AddMaps(typeof(MappingProfiles).Assembly);
        }).CreateMapper().ConfigurationProvider;
        _mapper = new Mapper(mockMapper);
        _controller = new AuctionsController(_auctionRepository.Object, _mapper, _publishEndpoint.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = Helpers.GetClaimsPrincipal()
                }
            }
        };
    }

    [Fact]
    public async Task GetAuctions_WithNoParams_ReturnsOkResult()
    {
        // Arrange
        var auctions = _fixture.CreateMany<AuctionDto>(10).ToList();
        _auctionRepository
            .Setup(repo => repo.GetAuctionsAsync(null))
            .ReturnsAsync(auctions);

        // Act
        var result = await _controller.GetAllAuctions(null);

        // Assert
        Assert.Equal(auctions.Count, result.Value.Count);
        Assert.IsType<ActionResult<List<AuctionDto>>>(result);
        Assert.Equal(auctions, result.Value);
    }

    [Fact]
    public async Task GetAuctions_WithDateParam_ReturnsOkResult()
    {
        // Arrange
        var date = DateTime.UtcNow.ToString();
        var auctions = _fixture.CreateMany<AuctionDto>(3).ToList();
        _auctionRepository.Setup(repo => repo.GetAuctionsAsync(date))
            .ReturnsAsync(auctions);

        // Act
        var result = await _controller.GetAllAuctions(date);

        // Assert
        Assert.Equal(auctions.Count, result.Value.Count);
        Assert.Equal(auctions, result.Value);
    }

    [Fact]
    public async Task GetAuctionById_WithValidId_ReturnsAuction()
    {
        // Arrange
        var auction = _fixture.Create<AuctionDto>();
        _auctionRepository.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(auction);

        // Act
        var result = await _controller.GetAuctionById(auction.Id);

        // Assert
        Assert.Equal(auction, result.Value);
        Assert.IsType<ActionResult<AuctionDto>>(result);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _auctionRepository.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((AuctionDto)null);

        // Act
        var result = await _controller.GetAuctionById(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateAuction_WithValidCreateAuctionDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var auction = _fixture.Create<CreateAuctionDto>();
        _auctionRepository.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
        _auctionRepository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _controller.CreateAuction(auction);
        var createdResult = result.Result as CreatedAtActionResult;

        // Assert
        Assert.NotNull(createdResult);
        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(AuctionsController.GetAuctionById), createdResult.ActionName);
    }

    [Fact]
    public async Task CreateAuction_WithInvalidCreateAuctionDto_ReturnsBadRequest()
    {
        // Arrange  
        var auction = _fixture.Build<CreateAuctionDto>().Create();
        auction.Make = "test";
        auction.Model = "test model";

        _controller.ModelState.AddModelError("Make", "Required");

        // Act  
        var result = await _controller.CreateAuction(auction);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);

    }

    [Fact]
    public async Task CreateAuction_FailedSave_Returns400BadRequest()
    {
        // Arrange
        var auction = _fixture.Create<CreateAuctionDto>();
        _auctionRepository.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
        _auctionRepository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(false);

        // Act
        var result = await _controller.CreateAuction(auction);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAuction_WithUpdateAuctionDto_ReturnsOkResponse()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().Without(a => a.Item).Create();
        auction.Item = _fixture.Build<Item>().Without(i => i.Auction).Create();
        auction.Seller = "test";
        var updateDto = _fixture.Create<UpdateAuctionDto>();
        _auctionRepository.Setup(repo => repo.GetAuctionEntityByIdAsync(auction.Id))
            .ReturnsAsync(auction);
        _auctionRepository.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateAuction(auction.Id, updateDto);

        // Assert   
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidUser_Returns403Forbid()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().Without(a => a.Item).Create();
        auction.Seller = "not test";
        var updateDto = _fixture.Create<UpdateAuctionDto>();
        _auctionRepository.Setup(repo => repo.GetAuctionEntityByIdAsync(auction.Id))
            .ReturnsAsync(auction);

        // Act
        var result = await _controller.UpdateAuction(auction.Id, updateDto);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidGuid_ReturnsNotFound()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().Without(a => a.Item).Create();
        var updateDto = _fixture.Create<UpdateAuctionDto>();
        _auctionRepository.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        // Act
        var result = await _controller.UpdateAuction(auction.Id, updateDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithValidUser_ReturnsOkResponse()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().Without(a => a.Item).Create();
        auction.Seller = "test";
        _auctionRepository.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(auction);
        _auctionRepository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);


        // Act
        var result = await _controller.DeleteAuction(auction.Id);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidGuid_Returns404Response()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().Without(a => a.Item).Create();
        _auctionRepository.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        // Act
        var result = await _controller.DeleteAuction(auction.Id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidUser_Returns403Response()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().Without(a => a.Item).Create();
        auction.Seller = "not test";
        _auctionRepository.Setup(repo => repo.GetAuctionEntityByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(auction);

        // Act
        var result = await _controller.DeleteAuction(auction.Id);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}
