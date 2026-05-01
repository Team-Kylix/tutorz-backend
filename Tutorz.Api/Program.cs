using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Tutorz.Infrastructure;
using Tutorz.Application.Interfaces;
using Tutorz.Application.Services;
using Tutorz.Infrastructure.Repositories;
using Tutorz.Infrastructure.Data;
using Tutorz.Infrastructure.Seeders; // Ensure this namespace is imported for LocationSeeder
using Tutorz.Api.Middlewares;
using Tutorz.Infrastructure.Services; // EncryptionService, FinancialsService
using Tutorz.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);
//  Get the connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//  Register the DbContext
builder.Services.AddDbContext<TutorzDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    }));

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICityRepository, CityRepository>();
builder.Services.AddScoped<IDistrictRepository, DistrictRepository>();
builder.Services.AddScoped<IProvinceRepository, ProvinceRepository>();
builder.Services.AddScoped<IInstituteStudentRepository, InstituteStudentRepository>();
builder.Services.AddScoped<IInstituteTutorRepository, InstituteTutorRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITutorRepository, TutorRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IInstituteRepository, InstituteRepository>();
builder.Services.AddScoped<IInstituteJoinRequestRepository, InstituteJoinRequestRepository>();
builder.Services.AddScoped<IHallRepository, HallRepository>();
builder.Services.AddScoped<IUserSequenceRepository, UserSequenceRepository>();
builder.Services.AddScoped<IEmailService, Tutorz.Infrastructure.Services.EmailService>();
builder.Services.AddHttpClient<ISmsService, Tutorz.Infrastructure.Services.SmsService>();
builder.Services.AddScoped<IIdGeneratorService, IdGeneratorService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<ITutorService, TutorService>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IInstituteService, InstituteService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IHallService, HallService>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IClassPaymentRepository, ClassPaymentRepository>();
builder.Services.AddScoped<IPaymentService, Tutorz.Infrastructure.Services.PaymentService>();
builder.Services.AddScoped<IProfilePictureService, Tutorz.Infrastructure.Services.ProfilePictureService>();
builder.Services.AddScoped<IEncryptionService, Tutorz.Infrastructure.Services.EncryptionService>();
builder.Services.AddScoped<IFinancialsService, Tutorz.Infrastructure.Services.FinancialsService>();

// Dispute / Ticketing System
builder.Services.AddScoped<IDisputeRepository, DisputeRepository>();
builder.Services.AddScoped<IDisputeService, Tutorz.Infrastructure.Services.DisputeService>();
builder.Services.AddScoped<IBillService, BillService>();

// Named HTTP client for PayHere API calls (Charging API, OAuth)
builder.Services.AddHttpClient("PayHere", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Notification Services
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationPusher, NotificationPusher>();

// SignalR
builder.Services.AddSignalR();

// API Usage Tracking Services
builder.Services.AddSingleton<Tutorz.Infrastructure.Services.ApiUsageTracker>();
builder.Services.AddSingleton<IApiUsageTracker>(sp => sp.GetRequiredService<Tutorz.Infrastructure.Services.ApiUsageTracker>());
builder.Services.AddHostedService<Tutorz.Infrastructure.Services.ApiUsageBatchWorker>();
builder.Services.AddHostedService<Tutorz.Infrastructure.Services.DailyAggregationWorker>();
builder.Services.AddHostedService<Tutorz.Infrastructure.Services.MonthlyAggregationWorker>();
builder.Services.AddHostedService<MonthlyBillingWorker>();

// Add JWT Configuration ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing"))),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        // SignalR WebSocket connections cannot send Authorization headers.
        // ASP.NET reads the token from the query string for hub connections.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }
                return System.Threading.Tasks.Task.CompletedTask;
            },

            // Layer 3: Minimum Token Date enforcement.
            // After the JWT signature is validated, check whether the token was issued
            // AFTER the MinTokenDate stored in the database.
            // If you update MinTokenDate to "now" during a release deploy, every token
            // issued before that deploy will be rejected on the next API call,
            // forcing the user to log in again and get a fresh token.
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices
                    .GetRequiredService<Tutorz.Infrastructure.Data.TutorzDbContext>();

                var setting = await db.AppSettings
                    .FindAsync("MinTokenDate");

                if (setting != null &&
                    DateTime.TryParse(setting.Value, null,
                        System.Globalization.DateTimeStyles.RoundtripKind,
                        out var minDate) &&
                    minDate > DateTime.UnixEpoch) // Skip check if still at epoch default
                {
                    // Read the "iat" (issued at) claim from the JWT
                    var iatClaim = context.Principal?.FindFirst(
                        System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iat)?.Value;

                    if (long.TryParse(iatClaim, out var iatSeconds))
                    {
                        var tokenIssuedAt = DateTimeOffset.FromUnixTimeSeconds(iatSeconds).UtcDateTime;
                        if (tokenIssuedAt < minDate)
                        {
                            // Token is older than the minimum — reject it
                            context.Fail("Token was issued before the minimum allowed date. Please log in again.");
                        }
                    }
                }
            }
        };
    });

builder.Services.AddMemoryCache();
builder.Services.AddControllers();
// code for Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Tutorz API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
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
            new string[]{}
        }
    });
});

// DEFINE CORS policy (This belongs with builder.Services)
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowMyReactApp",
                      policy =>
                      {
                          policy.WithOrigins(allowedOrigins)
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials(); // Often needed for auth apps
                      });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TutorzDbContext>();

        // any new migrations to the Azure database
        await context.Database.MigrateAsync();

        // Seeds the initial data (Roles, Admin, etc.)
        DbInitializer.Initialize(context);

        var env = services.GetRequiredService<IWebHostEnvironment>();
        var locationSeeder = new LocationSeeder(context, env);
        await locationSeeder.SeedAsync();

        // Seed Sri Lankan Bank + Branch directory from LankaPay xlsx
        var bankLogger = services.GetRequiredService<ILogger<Tutorz.Infrastructure.Seeders.BankDirectorySeeder>>();
        var bankSeeder = new Tutorz.Infrastructure.Seeders.BankDirectorySeeder(context, env, bankLogger);
        await bankSeeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database update.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tutorz API V1");
    });
}

// Serve static files (profile pictures, QR codes, etc.) with cross-origin headers
// so the PWA Service Worker can load them across origins (e.g. localhost:5173 -> localhost:7010)
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Allow cross-origin loading by Service Workers and browsers
        ctx.Context.Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
        // Cache images for 1 hour in the browser (reduces repeated fetches)
        ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=3600";
    }
});


if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowMyReactApp");

app.UseAuthentication();

app.UseAuthorization();

app.UseApiUsageTracking();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.Run();