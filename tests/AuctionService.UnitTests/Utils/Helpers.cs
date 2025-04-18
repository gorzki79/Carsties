using System.Security.Claims;

namespace AuctionService.UnitTests.Utils;

public class Helpers
{
    public static ClaimsPrincipal GetClaimsPrincipal()
    {
        var identity = new ClaimsIdentity(new[] {
            new Claim("username", "test"),
            new Claim(ClaimTypes.Name, "test"),
            new Claim(ClaimTypes.Email, "test@test.com")
        }, "testing");

        return new ClaimsPrincipal(identity);
    }
}
