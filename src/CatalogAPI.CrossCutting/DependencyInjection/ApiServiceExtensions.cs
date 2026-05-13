using Asp.Versioning;
using CatalogAPI.CrossCutting.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

namespace CatalogAPI.CrossCutting.DependencyInjection;

public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Add API Versioning
        // Version is now optional - defaults to 1.0 if not specified
        // Can be provided via query string (?api-version=1.0) or header (api-version: 1.0)
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            // Use query string or header for versioning (optional)
            // If not specified, defaults to 1.0
            options.ApiVersionReader = ApiVersionReader.Combine(
                new QueryStringApiVersionReader("api-version"),
                new HeaderApiVersionReader("api-version")
            );
        })
        .AddMvc();

        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    RoleClaimType = "role",
                    NameClaimType = "name",
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        // Add Swagger/OpenAPI with versioning support
        services.AddSwaggerGen(options =>
        {
            // Configure Swagger to automatically use API versioning
            // The version will be determined by the selected definition in Swagger UI
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Catalog API",
                Version = "v1",
                Description = "API de Catálogo de Jogos - FIAP",
                Contact = new OpenApiContact
                {
                    Name = "FIAP"
                }
            });

            // Add authorization header to Swagger using Swashbuckle v10.0.0 syntax
            options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme. Enter only the token (without 'Bearer')"
            });

            // Apply security requirement to all endpoints using Swashbuckle v10.0.0 syntax
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", document)] = []
            });
        });

        // Add Controllers
        services.AddControllers();

        // Add Health Checks
        var connectionString = configuration.GetConnectionString("CatalogDatabase");
        var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitMqPort = configuration["RabbitMQ:Port"] ?? "5672";
        var rabbitMqUsername = configuration["RabbitMQ:Username"] ?? string.Empty;
        var rabbitMqPassword = configuration["RabbitMQ:Password"] ?? string.Empty;

        services.AddHealthChecks()
            .AddNpgSql(connectionString!, name: "postgresql")
            .AddRabbitMQ(_ => 
            {
                var factory = new RabbitMQ.Client.ConnectionFactory
                {
                    HostName = rabbitMqHost,
                    Port = int.Parse(rabbitMqPort),
                    UserName = rabbitMqUsername,
                    Password = rabbitMqPassword
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            }, name: "rabbitmq")
            .AddCheck<OpenSearchHealthCheck>("opensearch")
            .AddCheck<RedisHealthCheck>("redis")
            .AddCheck<DynamoDbHealthCheck>("dynamodb");

        return services;
    }

    public static WebApplication UseApiConfiguration(this WebApplication app)
    {
        // Enable Swagger and Swagger UI
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
            options.RoutePrefix = "swagger"; // Swagger em /swagger para consistência
        });

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}
