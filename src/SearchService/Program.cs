using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchService;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<AuctionSvcHttpClient>()
    .AddPolicyHandler(GetPolicy());

builder.Services.AddMassTransit(x => 
{
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
    
     x.UsingRabbitMq((context,config) => 
     {
        config.ReceiveEndpoint("search-auction-created", e => 
          {
               e.UseMessageRetry(r => r.Interval(5, 5));
               e.ConfigureConsumer<AuctionCreatedConsumer>(context);
          });
          config.ConfigureEndpoints(context);
     });
});

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