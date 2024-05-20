using AuctionService;
using AuctionService.Data;
using AuctionService.Entities;
using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(options =>
{
     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddMassTransit(x => 
{
     x.AddEntityFrameworkOutbox<AuctionDbContext>(o => 
     {
          o.QueryDelay = TimeSpan.FromSeconds(10);
          o.UsePostgres();
          o.UseBusOutbox();
     });

     x.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>();
     x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));

     x.UsingRabbitMq((context,config) => 
     {
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

try
{
     DbInitializer.InitDb(app);
}
catch (Exception ex)
{
     Console.WriteLine(ex.Message);
}

app.Run();
