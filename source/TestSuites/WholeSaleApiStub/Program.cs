using Azure.Messaging.ServiceBus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(serviceProvider => new ServiceBusClient(serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>("ServiceBusConnectionString")));

builder.Services.AddSingleton<ServiceBusSender>(serviceProvider =>
    serviceProvider.GetRequiredService<ServiceBusClient>().CreateSender(serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>("ServiceBusTopicName")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
