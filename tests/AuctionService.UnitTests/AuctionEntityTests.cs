using AuctionService.Entities;

namespace AuctionService.UnitTests;

public class AuctionEntityTests
{
    [Fact]
    public void HasReservePrice_ReservePriceIsGreaterThanZero_ReturnsTrue()
    {
        // Arrange
        var auction = new Auction { Id = Guid.NewGuid(), ReservePrice = 100 };

        // Act
        var result = auction.HasReservePrice();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasReservePrice_ReservePriceIsZero_ReturnsFalse()
    {
        // Arrange
        var auction = new Auction { Id = Guid.NewGuid(), ReservePrice = 0 };

        // Act
        var result = auction.HasReservePrice();

        // Assert
        Assert.False(result);
    }
}