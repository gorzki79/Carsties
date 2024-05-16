using Polly;
using Polly.Extensions.Http;
using SearchService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient<AuctionSvcHttpClient>()
    .AddPolicyHandler(GetPolicy());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseAuthorization();
app.MapControllers();

app.Lifetime.ApplicationStarted.Register(async () => 
{
    try
    {
        await app.InitDb();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
});


app.Run();


static IAsyncPolicy<HttpResponseMessage> GetPolicy()
 => HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));