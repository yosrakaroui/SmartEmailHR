using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartEmailHR.API.Configuration;
using SmartEmailHR.API.Data;
using SmartEmailHR.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<GroqOptions>(builder.Configuration.GetSection(GroqOptions.SectionName));
builder.Services.Configure<N8nOptions>(builder.Configuration.GetSection(N8nOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var provider = configuration.GetValue<string>("Database:Provider")?.Trim().ToLowerInvariant();
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (provider == "sqlserver" && !string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlServer(connectionString);
        return;
    }

    if ((provider == "postgresql" || provider == "postgres" || provider == "npgsql") &&
        !string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseNpgsql(connectionString);
        return;
    }

    if ((provider == "mysql" || provider == "mariadb") && !string.IsNullOrWhiteSpace(connectionString))
    {
        ServerVersion serverVersion = provider == "mariadb"
            ? new MariaDbServerVersion(new Version(10, 4, 32))
            : new MySqlServerVersion(new Version(8, 0, 36));

        options.UseMySql(connectionString, serverVersion);
        return;
    }

    options.UseInMemoryDatabase("SmartEmailHR");
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret.Length < 32)
{
    throw new InvalidOperationException("Configuration invalide: Jwt:Secret doit contenir au moins 32 caracteres.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.RhOrAdmin, policy => policy.RequireRole(Roles.Rh, Roles.Admin));
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole(Roles.Admin));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithOrigins(
                "http://localhost:4200",
                "http://127.0.0.1:4200");
    });
});

builder.Services.AddHttpClient<IAiService, GroqAiService>();
builder.Services.AddHttpClient<IEmailWorkflowService, EmailWorkflowService>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IOfferLifecycleService, OfferLifecycleService>();
builder.Services.AddScoped<IN8nSecretValidator, N8nSecretValidator>();
builder.Services.AddScoped<DataSeeder>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartEmail HR API",
        Version = "v1",
        Description = "API REST pour la plateforme de recrutement intelligent SmartEmail HR."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Entrez: Bearer {votre_token_jwt}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            securityScheme,
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    if (!context.Database.IsInMemory())
    {
        await context.Database.EnsureCreatedAsync();
    }

    var seeder = services.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.Run();

public partial class Program
{
}
