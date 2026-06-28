namespace Teryaq.API.Extensions;

using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Teryaq.API.Infrastructure;
using Teryaq.API.Options;
using Teryaq.Application.Common.Settings;

/// <summary>Registers all API-layer services into the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>Adds controllers, Swagger, versioning, JWT authentication, CORS, rate limiting, and the global exception handler.</summary>
    public static IServiceCollection AddAPI(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddControllers(options => options.SuppressAsyncSuffixInActionNames = false)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
            };
        });

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Teryaq API",
                Version = "v1",
                Description = "Clean Architecture .NET 10 Web API template.",
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter a valid JWT access token.",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                    },
                    []
                },
            });

            string xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            options.IncludeXmlComments(xmlPath);
        });

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddMvc().AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'V";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddOptions<JwtOptions>()
            .BindConfiguration("Jwt")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AlertSettings>()
            .BindConfiguration("Alerts")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOpts) =>
            {
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOpts.Value.Issuer,
                    ValidAudience = jwtOpts.Value.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Value.Secret)),
                };
            });

        services.AddHttpContextAccessor();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("OwnerOnly", policy => policy.RequireRole("Owner"));
            options.AddPolicy("PharmacyStaff", policy => policy.RequireRole("Owner", "Pharmacist"));
        });

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        services.AddOptions<RateLimitOptions>()
            .BindConfiguration("RateLimit")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            var rateLimitOptions = configuration.GetSection("RateLimit").Get<RateLimitOptions>() ?? new RateLimitOptions();

            options.AddFixedWindowLimiter("fixed", limiterOptions =>
            {
                limiterOptions.Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds);
                limiterOptions.PermitLimit = rateLimitOptions.PermitLimit;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        services.AddResponseCompression();
        services.AddOutputCache();

        return services;
    }
}
