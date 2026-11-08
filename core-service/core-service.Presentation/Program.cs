using core_service.Application.Interfaces;
using core_service.Infrastructure;
using core_service.Infrastructure.Persistence.Repositories;
using core_service.Infrastructure.Services;
using core_service.Presentation.Hubs;
using core_service.Presentation.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
builder.Services.AddCustomAuthentication(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<CoreServiceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICoreServiceDbContext>(provider => provider.GetRequiredService<CoreServiceDbContext>());
builder.Services.AddScoped<IBookingAvailabilityChecker, BookingAvailabilityChecker>();
builder.Services.AddScoped<IBookingNotificationPublisher, BookingNotificationPublisher>();
builder.Services.AddScoped<IBookingNotificationService, SignalRBookingNotificationService>();


var app = builder.Build();

app.MapHub<BookingHub>("/hubs/bookings");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar/v1", options =>
    {
        options.WithTitle("Meeting Room Booking API")
               .WithTheme(ScalarTheme.Mars);
    });

    // Редирект с корня сайта сразу на Scalar UI
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthorization();
app.MapControllers();

app.Run();