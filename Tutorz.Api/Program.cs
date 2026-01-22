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

var builder = WebApplication.CreateBuilder(args);
//  Get the connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//  Register the DbContext
builder.Services.AddDbContext<TutorzDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITutorRepository, TutorRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IInstituteRepository, InstituteRepository>();
builder.Services.AddScoped<IUserSequenceRepository, UserSequenceRepository>();
builder.Services.AddScoped<IEmailService, Tutorz.Infrastructure.Services.EmailService>();
builder.Services.AddScoped<IIdGeneratorService, IdGeneratorService>();
builder.Services.AddScoped<ITutorService, TutorService>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IInstituteService, InstituteService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();

// Add JWT Configuration ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

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
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowMyReactApp",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TutorzDbContext>();
        var env = services.GetRequiredService<IWebHostEnvironment>();

        // Initialize DB (Roles, etc.)
        DbInitializer.Initialize(context);

        // Run Location Seeder
        var locationSeeder = new LocationSeeder(context, env);
        await locationSeeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tutorz API V1");
    });
}

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseCors("AllowMyReactApp");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.Run();