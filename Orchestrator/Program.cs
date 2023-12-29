using Core.RabbitMq.BusConfiguration;
using MassTransit;
using MassTransit.AspNetCoreIntegration;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.Logging;
using Microsoft.EntityFrameworkCore;

using Orchestrator.Presistance;

using Orchestrator.StateMachine.Order;

using System;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



#region MassTransit
builder.Services.AddDbContext<OrchSagaDbContext>((provider, dbContextBuilder) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    dbContextBuilder.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(OrchSagaDbContext).Assembly.FullName);
        sqlOptions.MigrationsHistoryTable($"__{nameof(OrchSagaDbContext)}");
    });
});

builder.Services.AddMassTransit(cfg =>
{
    cfg.AddSagaStateMachine<OrderStateMachine, OrderStateData>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Pessimistic; // or use Optimistic, which requires RowVersion
                    r.ExistingDbContext<OrchSagaDbContext>();

                });

    cfg.AddBus(provider => RabbitMqBus.ConfigureBusWebApi(provider, builder.Configuration));
});
//builder.Services.AddMassTransitHostedService(true);
#endregion





var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
