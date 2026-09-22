using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.API;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;
using SCRAP.infrastructure.services;
using System.Linq;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MasterErpDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MasterErp")));

builder.Services.AddDbContext<TenantErpDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("TenantErp")));

// Add services to the container.

// Configure controllers and JSON serializer to avoid object reference cycles when returning entities with navigation properties
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

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// simple authentication: add api-key style auth handler and role policies for the WinForms client
builder.Services.AddAuthentication("ApiKeyScheme").AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKeyScheme", options => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin", "Superadmin"));
    options.AddPolicy("RequireTech", policy => policy.RequireRole("TechStaff", "Superadmin"));
    options.AddPolicy("RequireSales", policy => policy.RequireRole("SalesStaff", "Superadmin"));
    options.AddPolicy("RequireManager", policy => policy.RequireRole("Manager", "Superadmin"));
});

builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
builder.Services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
builder.Services.AddScoped<SCRAP.API.Services.PayrollCalculator>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed initial data at startup (users, categories, recipes)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MasterErpDbContext>();
    try
    {
        SCRAP.infrastructure.DataSeeder.EnsureSeed(db);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogWarning(ex, "Data seeding skipped: database schema may not be ready yet.");
    }
}

app.MapPost("/companies", async (
    Company company,
    MasterErpDbContext db) =>
{
    db.Companies.Add(company);
    await db.SaveChangesAsync();

    return Results.Created($"/companies/{company.CompanyId}", company);
});

app.MapPost("/devices", async (
    Device device,
    MasterErpDbContext db) =>
{
    db.Devices.Add(device);
    await db.SaveChangesAsync();

    return Results.Created($"/devices/{device.DeviceId}", device);
});

app.MapGet("/test-tenant/{companyId:int}", async (
    int companyId,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);

    var productCount = await tenantDb.Products.CountAsync();

    return Results.Ok(new
    {
        companyId,
        productCount
    });
});

app.MapPost("/company-databases", async (
    CompanyDatabase companyDatabase,
    MasterErpDbContext db) =>
{
    db.CompanyDatabases.Add(companyDatabase);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/company-databases/{companyDatabase.CompanyDatabaseId}",
        companyDatabase);
});

app.MapPost("/tenant/{companyId:int}/products", async (
    int companyId,
    Product product,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);

    tenantDb.Products.Add(product);
    await tenantDb.SaveChangesAsync();

    return Results.Created(
        $"/tenant/{companyId}/products/{product.ProductId}",
        product);
});

app.MapGet("/tenant/{companyId:int}/products", async (
    int companyId,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);

    var products = await tenantDb.Products
        .AsNoTracking()
        .OrderBy(x => x.ProductId)
        .ToListAsync();

    return Results.Ok(products);
});
// ===== Device Categories =====
app.MapGet("/device-categories", async (SCRAP.infrastructure.data.MasterErpDbContext db) =>
    await db.Set<DeviceCategory>().AsNoTracking().OrderBy(x => x.Name).ToListAsync());

app.MapPost("/device-categories", async (DeviceCategory category, SCRAP.infrastructure.data.MasterErpDbContext db) =>
{
    db.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/device-categories/{category.Id}", category);
});

app.MapPut("/device-categories/{id:int}", async (int id, DeviceCategory updated, SCRAP.infrastructure.data.MasterErpDbContext db) =>
{
    var existing = await db.Set<DeviceCategory>().FindAsync(id);
    if (existing is null) return Results.NotFound();
    existing.Name = updated.Name;
    existing.Description = updated.Description;
    await db.SaveChangesAsync();
    return Results.Ok(existing);
});

app.MapDelete("/device-categories/{id:int}", async (int id, SCRAP.infrastructure.data.MasterErpDbContext db) =>
{
    var existing = await db.Set<DeviceCategory>().FindAsync(id);
    if (existing is null) return Results.NotFound();
    db.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ===== Standard Yields (ArchetypeRecipes) per category =====
app.MapGet("/device-categories/{categoryId:int}/yields", async (int categoryId, SCRAP.infrastructure.data.MasterErpDbContext db) =>
    await db.Set<ArchetypeRecipe>().AsNoTracking()
        .Where(x => x.DeviceCategoryId == categoryId)
        .ToListAsync());

app.MapPost("/device-categories/{categoryId:int}/yields", async (int categoryId, ArchetypeRecipe recipe, SCRAP.infrastructure.data.MasterErpDbContext db) =>
{
    recipe.DeviceCategoryId = categoryId;
    db.Add(recipe);
    await db.SaveChangesAsync();
    return Results.Created($"/device-categories/{categoryId}/yields/{recipe.Id}", recipe);
});

app.MapDelete("/yields/{id:int}", async (int id, SCRAP.infrastructure.data.MasterErpDbContext db) =>
{
    var existing = await db.Set<ArchetypeRecipe>().FindAsync(id);
    if (existing is null) return Results.NotFound();
    db.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ===== Inventory (active, not-yet-torn-down devices) =====
app.MapGet("/inventory", async (SCRAP.infrastructure.data.MasterErpDbContext db) =>
    await db.Set<Inventory>().AsNoTracking()
        .Include(x => x.DeviceCategory)
        .Where(x => x.Status != InventoryStatus.Disposed)
        .OrderByDescending(x => x.DateReceived)
        .ToListAsync());

app.MapPost("/inventory", async (Inventory item, SCRAP.infrastructure.data.MasterErpDbContext db) =>
{
    if (item.DateReceived == default) item.DateReceived = DateTime.UtcNow;
    db.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/inventory/{item.Id}", item);
});

app.MapPut("/inventory/{id:int}", async (int id, Inventory updated, SCRAP.infrastructure.data.MasterErpDbContext db) =>
{
    var existing = await db.Set<Inventory>().FindAsync(id);
    if (existing is null) return Results.NotFound();
    existing.DeviceName = updated.DeviceName;
    existing.DeviceCategoryId = updated.DeviceCategoryId;
    existing.SerialNumber = updated.SerialNumber;
    existing.Status = updated.Status;
    existing.Notes = updated.Notes;
    await db.SaveChangesAsync();
    return Results.Ok(existing);
});

app.MapDelete("/inventory/{id:int}", async (int id, SCRAP.infrastructure.data.MasterErpDbContext db) =>
{
    var existing = await db.Set<Inventory>().FindAsync(id);
    if (existing is null) return Results.NotFound();
    db.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ===== Recovered Commodities (post-teardown) =====
app.MapGet("/raw-inventory", async (SCRAP.infrastructure.data.MasterErpDbContext db) =>
    await db.Set<RawInventory>().AsNoTracking()
        .OrderByDescending(x => x.CurrentTotalWeightKg)
        .ToListAsync());

// ===== Dashboard summary =====
app.MapGet("/dashboard/summary", async (SCRAP.infrastructure.data.MasterErpDbContext db) =>
{
    var activeCount = await db.Set<Inventory>().CountAsync(x => x.Status != InventoryStatus.Disposed);
    var categoryCount = await db.Set<DeviceCategory>().CountAsync();
    var totalRecoveredKg = await db.Set<RawInventory>().SumAsync(x => (decimal?)x.CurrentTotalWeightKg) ?? 0;

    return Results.Ok(new
    {
        activeInventoryCount = activeCount,
        deviceCategoryCount = categoryCount,
        totalRecoveredWeightKg = totalRecoveredKg
    });
});

app.Run();