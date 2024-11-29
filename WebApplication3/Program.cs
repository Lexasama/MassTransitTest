using System.Reflection;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var services = builder.Services;

services.AddTransient<ICreatePassService, CreatePassService>();
builder.Services.AddMassTransit(x =>
{
    x.AddDelayedMessageScheduler();

    x.SetKebabCaseEndpointNameFormatter();

    // By default, sagas are in-memory, but should be changed to a durable
    // saga repository.
    x.SetInMemorySagaRepositoryProvider();

    var entryAssembly = Assembly.GetEntryAssembly();

    x.AddConsumers(entryAssembly);
    x.AddConsumer<CreatePassConsumer>();
    x.AddSagaStateMachines(entryAssembly);
    x.AddSagas(entryAssembly);
    x.AddActivities(entryAssembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseDelayedMessageScheduler();

        cfg.ConfigureEndpoints(context);
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/passes",
        async (CreatePassRequest request, [FromServices] ICreatePassService service) =>
        {
            await service.CreatePassAsync(request);
        })
    .WithName("CreatePass")
    .WithOpenApi();

app.Run();


public record CreatePassRequest(string Firstname);

public interface ICreatePassService
{
    Task CreatePassAsync(CreatePassRequest request);
}

public class CreatePassService : ICreatePassService
{
    private readonly IBus _bus;

    public CreatePassService(IBus bus)
    {
        _bus = bus;
    }

    public async Task CreatePassAsync(CreatePassRequest request)
    {
        await _bus.Publish(new CreatePass { Firstname = request.Firstname });
    }
}

public sealed class CreatePassConsumer : IConsumer<CreatePass>
{
    private ILogger<CreatePassConsumer> _logger;

    public CreatePassConsumer(ILogger<CreatePassConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<CreatePass> context)
    {
        _logger.LogInformation("Received Text: {Text}", context.Message.Firstname);

        return Task.CompletedTask;
    }
}


public record CreatePass()
{
    public string Firstname { get; init; }
}