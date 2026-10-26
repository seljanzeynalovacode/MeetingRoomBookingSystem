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
using MRBSystem.WebAPI.Hubs;
using MRBSystem.WebAPI.Services;
using Serilog;
using System.Security.Claims;
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
                          .AllowAnyMethod()
                          .AllowCredentials();
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

                options.AddPolicy("ChatPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
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

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                // Token provider təyin edilir
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;

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
            builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();

            builder.Services.AddScoped<IBuildingService, BuildingService>();
            builder.Services.AddScoped<IFloorService, FloorService>();
            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<IEquipmentService, EquipmentService>();
            builder.Services.AddScoped<IRoomEquipmentService, RoomEquipmentService>();
            builder.Services.AddScoped<IRoomBlackoutService, RoomBlackoutService>();
            builder.Services.AddScoped<IWorkingHourService, WorkingHourService>();
            builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IRoleService, RoleService>();

            builder.Services.AddSignalR();
            builder.Services.AddScoped<IReservationService, ReservationService>();
            builder.Services.AddScoped<IRoomCalendarBroadcastService, SignalRRoomCalendarBroadcastService>();
            builder.Services.AddScoped<IParticipantService, ParticipantService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddHttpClient<IChatService, GeminiChatService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });



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

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var tokenStamp = context.Principal?.FindFirstValue("SecurityStamp");

                        if (userId == null || tokenStamp == null)
                        {
                            context.Fail("Invalid token.");
                            return;
                        }

                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<AppUser>>();
                        var user = await userManager.FindByIdAsync(userId);

                        if (user == null || user.SecurityStamp != tokenStamp)
                        {
                            context.Fail("Bu token artıq etibarlı deyil, yenidən daxil olun.");
                        }
                    }
                };
            });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // --- Apply EF Core migrations on startup (waits for SQL Server to become ready) ---
            using (var migrationScope = app.Services.CreateScope())
            {
                var db = migrationScope.ServiceProvider
                    .GetRequiredService<MRBAppDbContext>();

                const int maxAttempts = 12;

                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        await db.Database.MigrateAsync();
                        Log.Information("Database migration completed successfully.");
                        break;
                    }
                    catch (Exception ex) when (attempt < maxAttempts)
                    {
                        Log.Warning(ex,
                            "Database not ready (attempt {Attempt}/{Max}). Retrying in 5s...",
                            attempt, maxAttempts);
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                }
            }
            // --- End migration block ---

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
            else
            {
                app.UseHsts();
            }

            app.UseMiddleware<ExceptionMiddleware>();
            app.UseHttpsRedirection();

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                await next();
            });

            app.UseCors("FrontendPolicy");
            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();
            app.MapHub<RoomCalendarHub>("/hubs/room-calendar");

            await app.RunAsync();
        }
    }
}
