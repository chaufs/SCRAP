using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.API;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;
using SCRAP.infrastructure.services;
using System.Linq;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Master Database (Platform / SuperAdmin)
builder.Services.AddDbContext<MasterErpDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MasterErp")));

// Multi-Tenant Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();

// Scoped TenantErpDbContext: dynamically resolves per-request tenant DB, falls back to default config if none specified
builder.Services.AddScoped<TenantErpDbContext>(sp =>
{
    var tenantContext = sp.GetRequiredService<ITenantContext>();
    var config = sp.GetRequiredService<IConfiguration>();

    var connStr = tenantContext.IsResolved
        ? tenantContext.ConnectionString
        : (config.GetConnectionString("TenantErp") ?? config.GetConnectionString("MasterErp")!);

    var options = new DbContextOptionsBuilder<TenantErpDbContext>()
        .UseSqlServer(connStr)
        .Options;

    return new TenantErpDbContext(options);
});

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Configure API behavior to return structured validation errors
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Where(kvp => kvp.Value.Errors.Count > 0)
            .Select(kvp => new { Field = kvp.Key, Errors = kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray() })
            .ToArray();

        var problem = new { message = "Validation failed", errors };
        return new BadRequestObjectResult(problem);
    };
});

builder.Services.AddOpenApi();

// Authentication: API key / Header handler with Superadmin vs Tenant Admin policies
builder.Services.AddAuthentication("ApiKeyScheme")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKeyScheme", options => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSuperadmin", policy => policy.RequireRole("Superadmin"));
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin", "Superadmin"));
    options.AddPolicy("RequireTech", policy => policy.RequireRole("TechStaff", "Superadmin"));
    options.AddPolicy("RequireSales", policy => policy.RequireRole("SalesStaff", "Superadmin"));
    options.AddPolicy("RequireManager", policy => policy.RequireRole("Manager", "Superadmin"));
});

builder.Services.AddScoped<SCRAP.API.Services.PayrollCalculator>();
builder.Services.AddScoped<ICloudSyncService, CloudSyncService>();
builder.Services.AddHostedService<SCRAP.API.Services.CloudSyncBackgroundWorker>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Resolve tenant DB for each request before auth
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed initial data at startup
using (var scope = app.Services.CreateScope())
{
    var masterDb = scope.ServiceProvider.GetRequiredService<MasterErpDbContext>();
    try
    {
        masterDb.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SuperAdmins')
            BEGIN
                CREATE TABLE [SuperAdmins] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Username] NVARCHAR(100) NOT NULL,
                    [PasswordHash] NVARCHAR(1000) NOT NULL,
                    [Email] NVARCHAR(200) NULL,
                    [FullName] NVARCHAR(200) NULL,
                    [IsActive] BIT NOT NULL DEFAULT 1,
                    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );
                CREATE UNIQUE INDEX [IX_SuperAdmins_Username] ON [SuperAdmins] ([Username]);
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'ContactEmail')
            BEGIN
                ALTER TABLE [Companies] ADD [ContactEmail] NVARCHAR(200) NULL;
                ALTER TABLE [Companies] ADD [ContactPhone] NVARCHAR(50) NULL;
                ALTER TABLE [Companies] ADD [SubscriptionPlan] NVARCHAR(100) NOT NULL DEFAULT 'Standard';
                ALTER TABLE [Companies] ADD [SubscriptionExpiresAt] DATETIME2 NULL;
            END
        ");
        SCRAP.infrastructure.DataSeeder.EnsureMasterSeed(masterDb);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogWarning(ex, "Master DB data seeding skipped.");
    }

    var tenantDb = scope.ServiceProvider.GetRequiredService<TenantErpDbContext>();
    try
    {
        SCRAP.infrastructure.DataSeeder.EnsureTenantSeed(tenantDb);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogWarning(ex, "Tenant DB data seeding skipped.");
    }
}

app.Run();