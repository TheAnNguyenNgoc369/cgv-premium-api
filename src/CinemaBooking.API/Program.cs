using CinemaBooking.API;
using CinemaBooking.Infrastructure;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddApiServices(builder.Configuration, builder.Environment);
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:5174", "https://cgv-premium-fe.vercel.app/", "https://cgv-premium-fe.vercel.app/")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<CinemaBookingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120) // Gives it 2 minutes to run the scripts
    ));

var app = builder.Build();

//Seed initial data
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider;

    await CinemaBookingDbSeeder.SeedUsersAsync(seeder);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CGV Premium API v1");
    c.RoutePrefix = "swagger"; // Đảm bảo đường dẫn là /swagger
});


if (!app.Environment.IsDevelopment())
{
    //app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
