using core_service.Application.Interfaces;
using core_service.Infrastructure.BackgroundServices;
using core_service.Infrastructure.Options;
using core_service.Infrastructure.Persistence;
using core_service.Infrastructure.Persistence.Repositories;
using core_service.Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace core_service.Infrastructure;

public static class DependencyInjection
{

    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<GoogleAuthSettings>(configuration.GetSection(GoogleAuthSettings.SectionName));

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                          ?? throw new InvalidOperationException("JwtSettings configuration is missing");

        var googleSettings = configuration.GetSection(GoogleAuthSettings.SectionName).Get<GoogleAuthSettings>()
                             ?? throw new InvalidOperationException("GoogleAuthSettings configuration is missing");

        services.AddScoped<TokenService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        })
        .AddGoogle(options =>
        {
            options.ClientId = googleSettings.ClientId;
            options.ClientSecret = googleSettings.ClientSecret;
            options.CallbackPath = "/signin-google";

            options.Events.OnRedirectToAuthorizationEndpoint = context =>
            {
                context.RedirectUri += "&prompt=select_account";

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AccessOnly", policy =>
                policy.RequireClaim("TokenType", "Access"));
            options.AddPolicy("RegisterOnly", policy =>
                policy.RequireClaim("TokenType", "Register"));
        });

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<CoreServiceDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));

        services.AddScoped<IBookingAvailabilityChecker, BookingAvailabilityChecker>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBookingNotificationPublisher, BookingNotificationPublisher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<CoreServiceDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitOptions = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                cfg.Host(rabbitOptions.Host, rabbitOptions.VirtualHost, h =>
                {
                    h.Username(rabbitOptions.Username);
                    h.Password(rabbitOptions.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });
        services.AddHostedService<BookingStatusNotifierWorker>();
        return services;
    }
}
