using CinemaBooking.API;
using CinemaBooking.Infrastructure;
using CinemaBooking.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddApiServices(builder.Configuration, builder.Environment);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CinemaBookingDbContext>("database");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
