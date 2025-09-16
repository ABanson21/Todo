using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TodoBackend.Configurations;
using TodoBackend.Database;
using TodoBackend.Model;
using TodoBackend.Services;

namespace TodoBackend;

public class Startup(IConfiguration configuration)
{
    private IConfiguration Configuration { get; } = configuration;

    // im using this to add services to the container. called by runtime
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddScoped(typeof(IDatabaseContext<>), typeof(DatabaseContext<>));
        services.AddScoped<TaskService>();
        services.AddScoped<UserService>();
        services.Configure<DatabaseConfig>(Configuration.GetSection("DatabaseSettings"));
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
                    ValidAudience = jwtOptions!.Audience ?? throw new InvalidOperationException("Jwt Audience is missing"),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions!.Key ?? throw new InvalidOperationException("Jwt Key is missing")))
                };
            });
        
        services.AddAuthorization();
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", build => build
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowed((hosts) => true));
        });
    }


    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }
        
        app.UseCors("CorsPolicy");
        app.UseHttpsRedirection();

        app.UseRouting();
        app.UseAuthentication();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}