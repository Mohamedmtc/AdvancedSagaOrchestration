using MassTransit.Logging;
using MassTransit;


using OrderService;
using System.Runtime.InteropServices;
using System.Reflection.PortableExecutable;

using Core.RabbitMq.BusConfiguration;
using MassTransit.AspNetCoreIntegration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();


#region MassTransit
builder.Services.AddMassTransit(cfg =>
{
    cfg.AddBus(provider => RabbitMqBus.ConfigureBusWebApi(provider, builder.Configuration));
});
//builder.Services.AddMassTransitHostedService();

#endregion





var app = builder.Build();

app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});

app.UseStatusCodePages();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
