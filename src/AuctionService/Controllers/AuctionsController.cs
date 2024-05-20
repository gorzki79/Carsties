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
    private readonly AuctionDbContext auctionDbContext;
    private readonly IMapper mapper;
    private readonly IPublishEndpoint publishEndpoint;

    public AuctionsController(AuctionDbContext auctionDbContext, IMapper mapper, IPublishEndpoint publishEndpoint)
    {
        this.auctionDbContext = auctionDbContext;
        this.mapper = mapper;
        this.publishEndpoint = publishEndpoint;
    }

    [HttpGet()]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
    {
        var query = auctionDbContext.Auctions.OrderBy(a => a.Item.Make).AsQueryable();

        if (!string.IsNullOrEmpty(date))
        {
            query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
        }

        return await query.ProjectTo<AuctionDto>(this.mapper.ConfigurationProvider).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        var auction = await auctionDbContext.Auctions
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == id);
        
        if (auction is null)
            return NotFound();

        return this.mapper.Map<AuctionDto>(auction);
    }

    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
    {
        var auction = this.mapper.Map<Auction>(auctionDto);
        auction.Seller = "test";

        this.auctionDbContext.Auctions.Add(auction);

        var newAuction = this.mapper.Map<AuctionDto>(auction);
        await this.publishEndpoint.Publish(this.mapper.Map<AuctionCreated>(newAuction));

        var result = await this.auctionDbContext.SaveChangesAsync() > 0;

        if (!result)
        {
            return BadRequest("Could not save changes to the DB");
        }

        return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, newAuction);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto auctionDto)
    {
        var auction = await this.auctionDbContext.Auctions.Include(x => x.Item)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction is null) return NotFound();

        //TODO: check seller == username
        auction.Item.Make = auctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = auctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = auctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = auctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = auctionDto.Year ?? auction.Item.Year;

        await this.publishEndpoint.Publish(this.mapper.Map<AuctionUpdated>(auction));

        var result = await this.auctionDbContext.SaveChangesAsync() > 0;

        if (result) return Ok();

        return BadRequest("Problem saving changes.");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await this.auctionDbContext.Auctions.FindAsync(id);

        if (auction is null) return NotFound();

        //TODO: check seller == username

        this.auctionDbContext.Auctions.Remove(auction);

        await this.publishEndpoint.Publish(new AuctionDeleted { Id = auction.Id.ToString() });

        var result = await this.auctionDbContext.SaveChangesAsync() > 0;

        if (result) return Ok();

        return BadRequest("Could not update DB.");
    }
}
