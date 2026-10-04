using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SCRAP.infrastructure.services;

namespace SCRAP.API
{
    public interface ITenantContext
    {
        int CompanyId { get; set; }
        string CompanyCode { get; set; }
        string ConnectionString { get; set; }
        bool IsResolved { get; }
    }

    public class TenantContext : ITenantContext
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public bool IsResolved => !string.IsNullOrWhiteSpace(ConnectionString);
    }

    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantResolutionMiddleware> _logger;
        // In-memory cache for connection strings by company code / id to avoid querying Master DB on every request
        private static readonly ConcurrentDictionary<string, string> _connectionCache = new(StringComparer.OrdinalIgnoreCase);

        public static void InvalidateCache(string? companyCode = null)
        {
            if (string.IsNullOrWhiteSpace(companyCode))
            {
                _connectionCache.Clear();
            }
            else
            {
                _connectionCache.TryRemove(companyCode, out _);
            }
        }

        public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ITenantContext tenantContext,
            ITenantDbContextFactory factory,
            ITenantDatabaseResolver resolver)
        {
            string? companyCode = null;
            int? companyId = null;

            // 1. Try reading header 'X-Company-Code'
            if (context.Request.Headers.TryGetValue("X-Company-Code", out var codeVal) && !string.IsNullOrWhiteSpace(codeVal))
            {
                var rawCode = codeVal.ToString();
                if (rawCode.Contains(','))
                {
                    var parts = rawCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    companyCode = parts.FirstOrDefault(p => string.Equals(p, "MASTER", StringComparison.OrdinalIgnoreCase) || string.Equals(p, "SUPERADMIN", StringComparison.OrdinalIgnoreCase))
                                  ?? parts.Last();
                }
                else
                {
                    companyCode = rawCode.Trim();
                }
            }
            // 2. Try reading header 'X-Company-Id'
            else if (context.Request.Headers.TryGetValue("X-Company-Id", out var idVal) && int.TryParse(idVal, out var parsedId))
            {
                companyId = parsedId;
            }

            if (!string.IsNullOrWhiteSpace(companyCode))
            {
                // SuperAdmin / Master context does not require a tenant database
                if (string.Equals(companyCode, "MASTER", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(companyCode, "SUPERADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

                try
                {
                    if (!_connectionCache.TryGetValue(companyCode, out var connStr))
                    {
                        var info = await resolver.GetDatabaseInfoByCodeAsync(companyCode);
                        connStr = await factory.GetConnectionStringAsync(info.CompanyId);
                        _connectionCache[companyCode] = connStr;
                    }

                    tenantContext.CompanyCode = companyCode;
                    tenantContext.ConnectionString = connStr;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Subscription Expired", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("suspended", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Tenant resolution blocked for '{CompanyCode}': {Message}", companyCode, ex.Message);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($"{{\"message\":\"{ex.Message}\"}}");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve tenant DB for company code '{CompanyCode}'", companyCode);
                }
            }
            else if (companyId.HasValue)
            {
                try
                {
                    var cacheKey = $"ID_{companyId.Value}";
                    if (!_connectionCache.TryGetValue(cacheKey, out var connStr))
                    {
                        var info = await resolver.GetDatabaseInfoAsync(companyId.Value);
                        connStr = await factory.GetConnectionStringAsync(info.CompanyId);
                        _connectionCache[cacheKey] = connStr;
                    }

                    tenantContext.CompanyId = companyId.Value;
                    tenantContext.ConnectionString = connStr;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve tenant DB for company id '{CompanyId}'", companyId.Value);
                }
            }

            await _next(context);
        }
    }
}
