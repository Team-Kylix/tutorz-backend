
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

var builder = WebApplication.CreateBuilder(args);
// 1. Get the connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Register the DbContext
builder.Services.AddDbContext<TutorzDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register the main service (from Application layer)
builder.Services.AddScoped<IAuthService, AuthService>();

// This line tells your app:
// "When any service (like AuthService) asks for the IRoleRepository contract,
// give it a new instance of the RoleRepository tool."
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

// Register the repositories (from Infrastructure layer)
// When AuthService asks for IUserRepository, give it a UserRepository
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITutorRepository, TutorRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
// builder.Services.AddScoped<IInstituteRepository, InstituteRepository>();

// --- END: Register Services and Repositories ---

// --- START: Add JWT Configuration ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = false, // For development
            ValidateAudience = false // For development
        };
    });
// --- END: Add JWT Configuration ---

// Add services to the container.
builder.Services.AddControllers();
// --- START: Add this code for Swagger ---

builder.Services.AddSwaggerGen(options =>
{
    // This defines the basic info for your API
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Tutorz API", Version = "v1" });

    // This configures the "Authorize" button to accept JWT Bearer tokens
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    // This makes Swagger automatically add the token to all locked endpoints
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

// --- END: Add this code for Swagger ---

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
//builder.Services.AddSwaggerGen();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tutorz API V1");
    });
}

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

app.UseHttpsRedirection();
// --- START: Add Authentication Middleware ---
// IMPORTANT: This must come BEFORE app.UseAuthorization();
app.UseAuthentication();
// --- END: Add Authentication Middleware ---
app.UseAuthorization();
app.MapControllers();
app.Run();

