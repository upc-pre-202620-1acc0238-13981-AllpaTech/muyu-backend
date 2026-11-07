using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Muyu.API;
using Muyu.Modules.Agreements.Infrastructure;
using Muyu.Modules.Agreements.Presentation;
using Muyu.Modules.ClimateAlerts.Infrastructure;
using Muyu.Modules.ClimateAlerts.Presentation;
using Muyu.Modules.Escrow.Infrastructure;
using Muyu.Modules.Escrow.Presentation;
using Muyu.Modules.IAM.Infrastructure;
using Muyu.Modules.IAM.Presentation;
using Muyu.Modules.ParcelManagement.Infrastructure;
using Muyu.Modules.ParcelManagement.Presentation;
using Muyu.Modules.Tracking.Infrastructure;
using Muyu.Modules.Tracking.Presentation;
using Muyu.Shared.Application;
using Muyu.Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(railwayPort))
{
    if (!int.TryParse(railwayPort, out var port) || port is < 1 or > 65535)
    {
        throw new InvalidOperationException("La variable PORT no contiene un puerto válido.");
    }

    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var connectionString = builder.Configuration.GetConnectionString("MuyuDatabase")
    ?? throw new InvalidOperationException("No se configuró ConnectionStrings:MuyuDatabase.");

var sqliteConnection = new SqliteConnectionStringBuilder(connectionString);
if (!Path.IsPathRooted(sqliteConnection.DataSource))
{
    sqliteConnection.DataSource = Path.GetFullPath(
        sqliteConnection.DataSource,
        builder.Environment.ContentRootPath);
}

var databaseDirectory = Path.GetDirectoryName(sqliteConnection.DataSource);
if (!string.IsNullOrWhiteSpace(databaseDirectory))
{
    Directory.CreateDirectory(databaseDirectory);
}

var jwtOptions = builder.Configuration
    .GetRequiredSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("No se configuró la sección Jwt.");

if (builder.Environment.IsProduction()
    && jwtOptions.SigningKey.StartsWith("CHANGE-THIS-KEY", StringComparison.Ordinal))
{
    throw new InvalidOperationException(
        "Configure Jwt__SigningKey con un secreto seguro antes de ejecutar en Production.");
}

builder.Services.AddScoped<IDomainEventPublisher, InProcessDomainEventPublisher>();

builder.Services.AddDbContext<MuyuDbContext>(options =>
    options.UseSqlite(sqliteConnection.ConnectionString));

var dataProtectionPath = builder.Configuration["Storage:DataProtectionPath"];
if (string.IsNullOrWhiteSpace(dataProtectionPath))
{
    dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, ".keys");
}

Directory.CreateDirectory(dataProtectionPath);

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("Muyu");

builder.Services.AddIamModule(builder.Configuration);
builder.Services.AddParcelManagementModule();
builder.Services.AddAgreementsModule();
builder.Services.AddEscrowModule();
builder.Services.AddTrackingModule();
builder.Services.AddClimateAlertsModule();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(AuthController).Assembly)
    .AddApplicationPart(typeof(ParcelsController).Assembly)
    .AddApplicationPart(typeof(AgreementsController).Assembly)
    .AddApplicationPart(typeof(EscrowController).Assembly)
    .AddApplicationPart(typeof(EvidencesController).Assembly)
    .AddApplicationPart(typeof(AlertsController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Muyu API",
        Version = "v1",
        Description = "Backend modular monolítico de Muyu."
    });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Escriba: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [bearerScheme] = Array.Empty<string>()
    });
});

const string CorsPolicy = "MuyuFrontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(allowedOrigins);
        }

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MuyuDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS mobile_app_states (
            UserId TEXT NOT NULL CONSTRAINT PK_mobile_app_states PRIMARY KEY,
            PayloadJson TEXT NOT NULL,
            UpdatedAtUtc TEXT NOT NULL
        );
        """);
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS mobile_plots (
            Id INTEGER NOT NULL CONSTRAINT PK_mobile_plots PRIMARY KEY,
            OwnerId TEXT NOT NULL,
            OwnerEmail TEXT NOT NULL,
            FundedByEmail TEXT NULL,
            PayloadJson TEXT NOT NULL,
            UpdatedAtUtc TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS mobile_visits (
            Id INTEGER NOT NULL CONSTRAINT PK_mobile_visits PRIMARY KEY,
            FarmerEmail TEXT NOT NULL,
            MerchantEmail TEXT NOT NULL,
            PayloadJson TEXT NOT NULL,
            UpdatedAtUtc TEXT NOT NULL
        );
        """);

    var iamSchemaUpgrade = scope.ServiceProvider.GetRequiredService<IamSchemaUpgrade>();
    await iamSchemaUpgrade.ApplyAsync();

    var trackingSchemaUpgrade = scope.ServiceProvider.GetRequiredService<TrackingSchemaUpgrade>();
    await trackingSchemaUpgrade.ApplyAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<IamDataSeeder>();
    await seeder.SeedAsync();
}

// Keep the API contract available in Railway so mobile integration can be
// verified without weakening authorization on protected operations.
app.UseSwagger();
app.UseSwaggerUI();

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/health"),
    branch => branch.UseHttpsRedirection());
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous();
app.MapControllers();

await app.RunAsync();

public partial class Program;
