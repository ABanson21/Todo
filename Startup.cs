using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TodoBackend.Configurations;
using TodoBackend.Database;
using TodoBackend.Repository;
using TodoBackend.Security;
using TodoBackend.Services;

namespace TodoBackend;

public class Startup(IConfiguration configuration)
{
    private IConfiguration Configuration { get; } = configuration;

    // im using this to add services to the container. called by runtime
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddScoped<UserRepository>();          
        services.AddScoped<AuthProvider>();
        services.AddScoped<TokenRepository>();
        services.Configure<DatabaseSettings>(Configuration.GetSection("DatabaseSettings"));
        
        ConfigureAuthentication(services);
        ConfigureAuthorization(services);
        ConfigureDatabase(services);
        ConfigureRateLimiting(services);
        
        services.AddHttpContextAccessor();

        // -------------------- Swagger --------------------
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // -------------------- CORS --------------------
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", build => build
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowed((hosts) => true));
        });
    }

    // -------------------- Middleware Pipeline --------------------
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    { 
        if (!env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }

        app.UseRateLimiter();
        app.UseCors("CorsPolicy");
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
    
    private void ConfigureDatabase(IServiceCollection services)
    {
        // -------------------- Database Settings --------------------
        //var serviceProvider = services.BuildServiceProvider();
        var databaseSettings = Configuration.GetSection(nameof(DatabaseSettings)).Get<DatabaseSettings>();
        var connectionString = $"Host={databaseSettings.Host};" +
                               $"Port={databaseSettings.Port};" +
                               $"Database={databaseSettings.Database};" +
                               $"Username={databaseSettings.Username};" +
                               $"Password={databaseSettings.Password};";
                           
        var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";
        
        if (isProduction)
        {
            connectionString += "SslMode=Require;" + "Trust Server Certificate=true;";
        }
        
        
        services.AddDbContext<AppDatabaseContext>( options =>
            options.UseNpgsql(connectionString).UseLowerCaseNamingConvention());
        
    }
    
    

    private void ConfigureAuthentication(IServiceCollection services)
    {
        services.Configure<JwtOptions>(Configuration.GetSection("Jwt"));
        var jwtOptions = Configuration.GetSection("Jwt").Get<JwtOptions>();
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions!.Issuer ?? throw new InvalidOperationException("Jwt Issuer is missing"),
                    ValidAudience = jwtOptions!.Audience ??
                                    throw new InvalidOperationException("Jwt Audience is missing"),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions!.Key ??
                        throw new InvalidOperationException("Jwt Key is missing")))
                };
            });
    }
    
    private void ConfigureAuthorization(IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, ResourceOwnerHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(AppConstants.CanEditOwnProfile,
                policy => policy.Requirements.Add(new ResourceOwnerRequirement()));
    }
    
    private void ConfigureRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var secondsLeft)
                    ? secondsLeft
                    : TimeSpan.FromSeconds(60);
                
                var json = JsonSerializer.Serialize(new
                {
                    error = "Too Many Requests",
                    message = "You have exceeded the allowed request rate. Please try again later.",
                    retryAfterSeconds = seconds
                });

                await context.HttpContext.Response.WriteAsync(json, token);
            };
            
            options.AddPolicy("LoginPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy("RefreshPolicy", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4, 
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });
    }
}