using JBP.Data;
using JBP.Services;
using JBP.Services.VerificationProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// API controllers are used by the static frontend and Swagger UI.
builder.Services.AddControllers();

// Shared services used by application and verification flows.
builder.Services.AddScoped<EmailService>();
builder.Services.Configure<DigiLockerOptions>(
    builder.Configuration.GetSection("DigiLocker"));
builder.Services.AddScoped<IDigiLockerGateway, DigiLockerGateway>();
builder.Services.Configure<EpfoOptions>(
    builder.Configuration.GetSection("EPFO"));
builder.Services.AddScoped<VerificationService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IEpfoVerificationProvider, SurepassEpfoProvider>();

// JWT is the main login/session mechanism.
// Frontend sends this token as: Authorization: Bearer <token>.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidateAudience = true,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]!
                        ))
            };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Swagger can call secured APIs when the tester pastes the login token here.
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Enter JWT Token"
        });

options.AddSecurityRequirement(
    new OpenApiSecurityRequirement
    {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,

                            Id = "Bearer"
                        }
                },

                Array.Empty<string>()
            }
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Frontend runs from localhost during development, so API allows local browser calls.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://127.0.0.1:5500",
                "http://localhost:5500",
                "https://thankful-rock-0c403ba00.7.azurestaticapps.net"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Resume uploads are stored outside the frontend folder and exposed as /Uploads/<file>.
var uploadsPath =
    Path.Combine(app.Environment.ContentRootPath, "Uploads");

Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider =
        new PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads"
});

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Local diagnostics endpoint for checking whether EF migrations are applied.
app.MapGet("/__migrations", async (ApplicationDbContext db) =>
{
    var applied = await Task.Run(() => db.Database.GetAppliedMigrations());
    var pending = await Task.Run(() => db.Database.GetPendingMigrations());
    return Results.Ok(new { applied = applied.ToList(), pending = pending.ToList() });
});

app.Run();
