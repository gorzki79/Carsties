using MongoDB.Entities;

namespace SearchService;

public class AuctionSvcHttpClient
{
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;

    public AuctionSvcHttpClient(HttpClient httpClient, IConfiguration configuration)
    {
        this.httpClient = httpClient;
        this.configuration = configuration;
    }

    public async Task<List<Item>> GetItemsForSearchDb()
    { 
        var lastUpdated = await DB.Find<Item, string>()
            .Sort(x => x.Descending(x => x.UpdatedAt))
            .Project(x => x.UpdatedAt.ToString())
            .ExecuteFirstAsync();

        return await this.httpClient.GetFromJsonAsync<List<Item>>(this.configuration["AuctionServiceUrl"] 
            + "/api/auctions?date=" + lastUpdated);
    }
}
