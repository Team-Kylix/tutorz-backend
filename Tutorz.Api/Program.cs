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

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITutorRepository, TutorRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IInstituteRepository, InstituteRepository>();

// Add JWT Configuration ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = false, //
            ValidateAudience = false // For development
        };
    });

builder.Services.AddControllers();
// --- START: Add this code for Swagger ---
builder.Services.AddSwaggerGen(options =>
{
    // ... your existing SwaggerGen config ...
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
// --- END: Add this code for Swagger ---

// DEFINE your CORS policy (This belongs with builder.Services)
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowMyReactApp",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173") // Vite frontend URL
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});


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

app.UseHttpsRedirection();

// Your CORS policy MUST be used before Authentication/Authorization
app.UseCors("AllowMyReactApp");

// --- START: Add Authentication Middleware ---
app.UseAuthentication();
// --- END: Add Authentication Middleware ---
app.UseAuthorization();

app.MapControllers();
app.Run();
