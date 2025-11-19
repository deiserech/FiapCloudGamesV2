using System.Text.Json.Serialization;
using FiapCloudGames.Users.Api.BackgroundServices;
using FiapCloudGames.Users.Api.Extensions;
using FiapCloudGames.Users.Api.Middlewares;
using FiapCloudGames.Users.Application.Interfaces.Services;
using FiapCloudGames.Users.Application.Services;
using FiapCloudGames.Users.Domain.Interfaces.Repositories;
using FiapCloudGames.Users.Infrastructure.Data;
using FiapCloudGames.Users.Infrastructure.Repositories;
using FiapCloudGames.Users.Infrastructure.ServiceBus;
using FiapCloudGames.Users.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddHealthChecks()
    .AddSqlServer(configuration.GetConnectionString("DefaultConnection") ?? "");
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddFiapCloudGamesSwagger();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection") ?? ""));

builder.Services.AddFiapCloudGamesJwtAuthentication(configuration);

builder.Services.AddSingleton<ServiceBusClientWrapper>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<ILibraryRepository, LibraryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddHostedService<ResourceLoggingService>();
builder.Services.AddHostedService<PurchaseCompletedConsumer>();

builder.Services.AddFiapCloudGamesOpenTelemetry();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FIAP Cloud Games API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TracingEnrichmentMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
