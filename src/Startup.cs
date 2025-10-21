using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Tranzor.Context;
using Tranzor.Filters;
using Tranzor.HealthChecks;
using Tranzor.Interfaces.Config;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs;
using Tranzor.Interfaces.Gtfs.External;
using Tranzor.Interfaces.Gtfs.Realtime;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Interfaces.Http;
using Tranzor.Interfaces.MQTT;
using Tranzor.Services.Config;
using Tranzor.Services.Database;
using Tranzor.Services.External;
using Tranzor.Services.Gtfs;
using Tranzor.Services.Gtfs.Realtime;
using Tranzor.Services.Gtfs.Static;
using Tranzor.Services.Http;
using Tranzor.Services.MQTT;
using Tranzor.Services.OTP;

namespace Tranzor;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string envPath = Path.Combine(baseDirectory, ".env");

        Log.Information("Trying to load environment file from bin directory.");

        if (File.Exists(envPath))
        {
            Env.Load(envPath);
            Log.Information("environment file found and loaded successfully.");
        }
        else
        {
            string rootPath = Path.Combine(baseDirectory, "..", "..", "..", "..", ".env");
            Log.Information("Trying to load from root directory.");

            if (File.Exists(rootPath))
            {
                Env.Load(rootPath);
                Log.Information("Environment file found and loaded successfully.");
            }
            else
            {
                Log.Error("ERROR: environment file not found in any location...");
                Log.Information("Please create a environment file in the project root");
                Environment.Exit(1);
            }
        }

        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    private void ValidateEnvironmentVariables(ILogger<Startup> logger)
    {
        List<string> missingVars = new List<string>();

        foreach (var envVar in Constant.RequiredEnvVars)
        {
            string? value = Environment.GetEnvironmentVariable(envVar);

            if (string.IsNullOrWhiteSpace(value))
            {
                missingVars.Add(envVar);
                logger.LogError("Required environment variable not found: {0}", envVar);
            }
            else
            {
                logger.LogInformation("Loaded environment variable: {0}", envVar);
            }
        }

        if (missingVars.Any())
        {
            logger.LogError("Application terminated due to missing environment variables.");
            Environment.Exit(0);
        }
    }

    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureProtectionServices(services);
        ConfigureInfrastructureServices(services);
        ConfigureDatabaseServices(services);
        ConfigureCacheServices(services);
        ConfigureApplicationServices(services);
        ConfigureSecurityServices(services);
        ConfigureLogging(services);
    }

    private void ConfigureProtectionServices(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    }

    private void ConfigureInfrastructureServices(IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
        });

        services.AddControllers(options =>
        {
            options.Filters.Add<ValidateModelStateFilter>();
            options.Filters.Add<SanitizeInputFilter>();
            options.CacheProfiles.Add("Default", new CacheProfile
            {
                Duration = 60,
                Location = ResponseCacheLocation.Any
            });
        })
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
            opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            opts.JsonSerializerOptions.MaxDepth = 64;
        });

        services.AddResponseCaching();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = $"{Constant.Name} API", Version = Constant.Version });
            c.DocumentFilter<IgnoreProtobufTypesDocumentFilter>();
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    Array.Empty<string>()
                }
            });
        });

        services.AddScoped<TokenAuthFilter>();
        services.AddHttpClient();
    }

    private void ConfigureSecurityServices(IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.WithOrigins(Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                       .WithMethods(Configuration.GetSection("Cors:AllowedMethods").Get<string[]>() ?? Array.Empty<string>())
                       .WithHeaders(Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>() ?? Array.Empty<string>())
                       .WithExposedHeaders(Configuration.GetSection("Cors:ExposedHeaders").Get<string[]>() ?? Array.Empty<string>())
                       .SetPreflightMaxAge(TimeSpan.FromSeconds(Configuration.GetValue<int>("Cors:MaxAge", 3600)));
            });
        });

        services.AddHealthChecks()
            .AddCheck<SecurityHealthCheck>("security")
            .AddCheck<RateLimitHealthCheck>("rate_limit")
            .AddCheck<AuthenticationHealthCheck>("auth");
    }

    private void ConfigureLogging(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddSerilog(new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger());
        });
    }

    private void ConfigureDatabaseServices(IServiceCollection services)
    {
        services.AddDbContext<GtfsDbContext>(options =>
        {
            string? connection = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
            string? dbName = Environment.GetEnvironmentVariable("POSTGRES_DATABASE_NAME");

            string fullConnection = $"{connection};Database={dbName}";

            options
                .UseNpgsql(fullConnection, o => o.UseNetTopologySuite())
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IPostgresService, PostgresService>();
        services.AddSingleton<ICassandraService, CassandraService>();
    }

    private void ConfigureCacheServices(IServiceCollection services)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            string? connection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
            string? instanceName = Environment.GetEnvironmentVariable("REDIS_INSTANCE_NAME")?.ToLower();

            options.Configuration = connection;
            options.InstanceName = $"{instanceName}:";
        });

        services.AddSingleton<IRedisService, RedisService>();
    }

    private void ConfigureApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IAgencyService, AgencyService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<ICalendarDatesService, CalendarDatesService>();
        services.AddScoped<IFareAttributesService, FareAttributesService>();
        services.AddScoped<IFareRulesService, FareRulesService>();
        services.AddScoped<IRoutesService, RoutesService>();
        services.AddScoped<IShapesService, ShapesService>();
        services.AddScoped<IStopsService, StopsService>();
        services.AddScoped<IStopTimesService, StopTimesService>();
        services.AddScoped<ITransfersService, TransfersService>();
        services.AddScoped<ITripsService, TripsService>();
        services.AddScoped<IConfigService, ConfigService>();
        services.AddScoped<IGtfsDataService, GtfsDataService>();
        services.AddScoped<IFeedInfoService, FeedInfoService>();
        services.AddSingleton<IGtfsFileService, GtfsFileService>();
        services.AddSingleton<IGtfsRealtimeCacheService, GtfsRealtimeCacheService>();
        services.AddSingleton<IGtfsMqttRealtimeService, GtfsMqttRealtimeService>();
        services.AddScoped<ITranslationService, TranslationService>();
        services.AddScoped<IAttributionService, AttributionService>();
        services.AddScoped<IStopAreaService, StopAreaService>();
        services.AddScoped<IFareMediaService, FareMediaService>();
        services.AddScoped<IFareLegRuleService, FareLegRuleService>();
        services.AddScoped<IFareProductService, FareProductService>();
        services.AddScoped<INetworkService, NetworkService>();
        services.AddScoped<IOpenTripPlannerService, OpenTripPlannerService>();
        services.AddScoped<ICityService, CityService>();
        services.AddHttpClient<IOtpHttpClient, OtpHttpClient>();
    }

    public void ConfigureSecurityHeaders(IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
            context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await next();
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, ILogger<Startup> logger)
    {
        ValidateEnvironmentVariables(logger);

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseSerilogRequestLogging();

        ConfigureSecurityHeaders(app);

        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{Constant.Name} API {Constant.Version}"));

        app.UseIpRateLimiting();

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseResponseCaching();
        app.UseResponseCompression();

        using (IServiceScope scope = app.ApplicationServices.CreateScope())
        {
            GtfsDbContext db = scope.ServiceProvider.GetRequiredService<GtfsDbContext>();
            //db.Database.Migrate();
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health");
        });

        InitializeSystem(serviceProvider, logger);
    }

    private void InitializeSystem(IServiceProvider serviceProvider, ILogger<Startup> logger)
    {
        IConfigService configService = serviceProvider.GetRequiredService<IConfigService>();
        IGtfsDataService gtfsDataService = serviceProvider.GetRequiredService<IGtfsDataService>();
        ICassandraService cassandraService = serviceProvider.GetRequiredService<ICassandraService>();

        try
        {
            // Start Postgres
            using (var scope = serviceProvider.CreateScope())
            {
                IPostgresService postgresService = scope.ServiceProvider.GetRequiredService<IPostgresService>();
                postgresService.InitializeAsync().Wait();
            }

            // Start Redis
            IRedisService redisService = serviceProvider.GetRequiredService<IRedisService>();
            
            Task.Run(async () =>
            {
                bool isAvailable = await redisService.IsRedisAvailable();
                if (isAvailable)
                {
                    await redisService.RSetupAsync();
                    logger.LogInformation("[Redis] Connection verified successfully");
                }
                else
                {
                    logger.LogWarning("[Redis] Not available, caching will be disabled");
                }
            }).Wait();

            // Start Cassandra
            cassandraService.InitializeAsync().Wait();

            configService.InitializeAsync().Wait();

            if (!GtfsDataContext.Finish)
                Environment.Exit(0);

            gtfsDataService.InitializeAsync().Wait();
        }
        catch (Exception ex)
        {
            logger.LogError($"Unable to start the system.\n -> {ex.Message}");
            Task.Delay(5000).Wait();
            Environment.Exit(0);
        }
    }
}