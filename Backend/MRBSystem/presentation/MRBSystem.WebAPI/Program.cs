using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MRBSystem.Application.Exceptions;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;
using MRBSystem.Infrasturucture.Services;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

namespace MRBSystem.WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                 .ReadFrom.Configuration(builder.Configuration)
                 .Enrich.FromLogContext()
                 .CreateLogger();

            builder.Host.UseSerilog();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            //---
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "MRBSystem API", Version = "v1" });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT token-i belə daxil et: Bearer {token}"
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
            Array.Empty<string>()
        }
    });
            });
            //--

            var corsAllowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", policy =>
                {
                    policy.WithOrigins(corsAllowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("LoginPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        statusCode = 429,
                        message = "Çox sayda cəhd edildi. Zəhmət olmasa bir az sonra yenidən cəhd edin."
                    }, cancellationToken);
                };
            });

            builder.Services.AddDbContext<MRBAppDbContext>(options =>
     options.UseSqlServer(
         builder.Configuration.GetConnectionString("MRBSystemConnectionString"),
         sqlOptions => sqlOptions.EnableRetryOnFailure()));

            builder.Services.AddIdentity<AppUser, AppRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;

                // Token provider təyin edilir
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
 .AddEntityFrameworkStores<MRBAppDbContext>() 
 .AddDefaultTokenProviders(); // DİQQƏT: Email Token provider-in işləməsi üçün bu şərtdir!

            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

            builder.Services.AddScoped<IBuildingService, BuildingService>();
            builder.Services.AddScoped<IFloorService, FloorService>();
            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<IEquipmentService, EquipmentService>();
            builder.Services.AddScoped<IRoomEquipmentService, RoomEquipmentService>();
            builder.Services.AddScoped<IRoomBlackoutService, RoomBlackoutService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IRoleService, RoleService>();

            builder.Services.AddScoped<IReservationService, ReservationService>();
            builder.Services.AddScoped<IParticipantService, ParticipantService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IEmailService, EmailService>();


            builder.Services.AddAutoMapper(x => x.AddProfile(typeof(CustomProfile)));

            builder.Services.AddHostedService<NotificationDeliveryService>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };
            });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var roleManager =
                    scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

                var userManager =
                    scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

                var configuration =
                    scope.ServiceProvider.GetRequiredService<IConfiguration>();

                const string adminRole = "Admin";
                const string userRole = "User";

                // Create Admin role
                if (!await roleManager.RoleExistsAsync(adminRole))
                {
                    var result = await roleManager.CreateAsync(
                        new AppRole { Name = adminRole });

                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Failed to create Admin role.");
                    }
                }

                // Create User role
                if (!await roleManager.RoleExistsAsync(userRole))
                {
                    var result = await roleManager.CreateAsync(
                        new AppRole { Name = userRole });

                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Failed to create User role.");
                    }
                }

                // Get admin credentials from User Secrets / configuration
                var adminEmail = configuration["SeedAdmin:Email"];
                var adminPassword = configuration["SeedAdmin:Password"];

                if (string.IsNullOrWhiteSpace(adminEmail) ||
                    string.IsNullOrWhiteSpace(adminPassword))
                {
                    throw new InvalidOperationException(
                        "SeedAdmin credentials are not configured.");
                }

                // Check if admin already exists
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser is null)
                {
                    adminUser = new AppUser
                    {
                        FirstName = configuration["SeedAdmin:FirstName"] ?? "System",
                        LastName = configuration["SeedAdmin:LastName"] ?? "Admin",
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(
                        adminUser,
                        adminPassword);

                    if (!result.Succeeded)
                    {
                        var errors = string.Join(
                            ", ",
                            result.Errors.Select(x => x.Description));

                        throw new InvalidOperationException(
                            $"Failed to create admin user: {errors}");
                    }

                    var roleResult = await userManager.AddToRoleAsync(
                        adminUser,
                        adminRole);

                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join(
                            ", ",
                            roleResult.Errors.Select(x => x.Description));

                        throw new InvalidOperationException(
                            $"Failed to assign Admin role: {errors}");
                    }
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<ExceptionMiddleware>();
            app.UseHttpsRedirection();

            app.UseCors("FrontendPolicy");
            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            await app.RunAsync();
        }
    }
}
