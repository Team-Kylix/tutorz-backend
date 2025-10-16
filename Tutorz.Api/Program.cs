
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Microsoft.EntityFrameworkCore;
using Tutorz.Infrastructure;
using Tutorz.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
// 1. Get the connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Register the DbContext
builder.Services.AddDbContext<TutorzDbContext>(options =>
    options.UseSqlServer(connectionString));

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

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
// --- START: Add Authentication Middleware ---
// IMPORTANT: This must come BEFORE app.UseAuthorization();
app.UseAuthentication();
// --- END: Add Authentication Middleware ---
app.UseAuthorization();
app.MapControllers();
app.Run();
