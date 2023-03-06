using Azure.Messaging.ServiceBus;
using WholeSaleApiStub;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(serviceProvider => new ServiceBusClient(serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>("ServiceBusConnectionString")));

builder.Services.AddSingleton<ServiceBusSender>(serviceProvider =>
    serviceProvider.GetRequiredService<ServiceBusClient>().CreateSender(serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>("ServiceBusTopicName")));

builder.Services.AddApiVersioning();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var startup = new Startup(builder.Configuration);

startup.ConfigureServices(builder.Services);

startup.Configure(app);

app.Run();

public partial class Program { }
