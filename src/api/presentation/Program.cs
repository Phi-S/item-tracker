using application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using presentation;
using presentation.Endpoints;
using Serilog;
using Throw;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration);
    });

    var corsOrigin = builder.Configuration.GetValue<string>("CorsOrigin");
    corsOrigin.ThrowIfNull().IfEmpty().IfWhiteSpace();
    Log.Logger.Information("CorsOrigin: {CorsOrigin}", corsOrigin);

    var authenticationAuthority = builder.Configuration.GetSection("AuthenticationAuthority").Value;
    authenticationAuthority.ThrowIfNull().IfEmpty().IfWhiteSpace();
    Log.Logger.Information("AuthenticationAuthority: {AuthenticationAuthority}", authenticationAuthority);

    var exchangeRateApiKey = builder.Configuration.GetValue<string>("ExchangeRatesApiKey");
    exchangeRateApiKey.ThrowIfNull().IfEmpty().IfWhiteSpace();
    Log.Logger.Information("ExchangeRatesApiKey: {ExchangeRatesApiKey}", exchangeRateApiKey);

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = authenticationAuthority;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateAudience = false
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithMethods("GET", "POST", "PUT", "DELETE");
            policy.WithHeaders("authorization");
            policy.WithHeaders("content-type");
            policy.WithOrigins(corsOrigin);
        });
    });

    builder.Services.AddApplication();

    builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "bearer"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        });
    });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(options => options.EnablePersistAuthorization());

    app.UseCors();
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapItemListEndpoints();
    app.MapItemsEndpoints();

    await app.StartAsync();
    Log.Logger.Information("Server is running under: {Addresses}", string.Join(",", app.Urls));
    Log.Logger.Information("Application started");
    await app.WaitForShutdownAsync();
}
catch (Exception e)
{
    Log.Logger.Fatal(e, "Application crashed");
}
finally
{
    await Log.CloseAndFlushAsync();
    Log.Logger.Information("Logger closed and flushed");
    Log.Logger.Information("Application exited");
}