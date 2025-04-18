using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;

//using Contracts;
//using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionRepository auctionRepository;
    private readonly IMapper mapper;
    private readonly IPublishEndpoint publishEndpoint;

    public AuctionsController(IAuctionRepository auctionRepository, IMapper mapper, IPublishEndpoint publishEndpoint)
    {
        this.auctionRepository = auctionRepository;
        this.mapper = mapper;
        this.publishEndpoint = publishEndpoint;
    }

    [HttpGet()]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
    {
        return await auctionRepository.GetAuctionsAsync(date);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        var auction = await auctionRepository.GetAuctionByIdAsync(id);

        if (auction is null)
            return NotFound();

        return auction;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
    {
        var auction = this.mapper.Map<Auction>(auctionDto);
        auction.Seller = this.User.Identity.Name;

        auctionRepository.AddAuction(auction);

        var newAuction = this.mapper.Map<AuctionDto>(auction);
        await this.publishEndpoint.Publish(this.mapper.Map<AuctionCreated>(newAuction));

        var result = await auctionRepository.SaveChangesAsync();

        if (!result)
        {
            return BadRequest("Could not save changes to the DB");
        }

        return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, newAuction);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto auctionDto)
    {
        var auction = await auctionRepository.GetAuctionEntityByIdAsync(id);

        if (auction is null) return NotFound();

        if (auction.Seller != this.User.Identity.Name)
            return Forbid();

        auction.Item.Make = auctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = auctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = auctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = auctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = auctionDto.Year ?? auction.Item.Year;

        await this.publishEndpoint.Publish(this.mapper.Map<AuctionUpdated>(auction));

        var result = await auctionRepository.SaveChangesAsync();

        if (result) return Ok();

        return BadRequest("Problem saving changes.");
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await auctionRepository.GetAuctionEntityByIdAsync(id);

        if (auction is null) return NotFound();

        if (auction.Seller != this.User.Identity.Name)
            return Forbid();

        auctionRepository.RemoveAuction(auction);

        await this.publishEndpoint.Publish(new AuctionDeleted { Id = auction.Id.ToString() });

        var result = await auctionRepository.SaveChangesAsync();

        if (result) return Ok();

        return BadRequest("Could not update DB.");
    }
}
