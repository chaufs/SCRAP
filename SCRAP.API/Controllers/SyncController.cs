using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SCRAP.domain.entities;
using SCRAP.infrastructure.services;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly ICloudSyncService _syncService;
        private readonly ILogger<SyncController> _logger;

        public SyncController(ICloudSyncService syncService, ILogger<SyncController> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }

        [HttpGet("status")]
        public async Task<ActionResult<CloudSyncStatus>> GetStatus()
        {
            var status = await _syncService.GetSyncStatusAsync();
            return Ok(status);
        }

        [HttpPost("trigger")]
        public async Task<ActionResult<CloudSyncResult>> TriggerSync()
        {
            _logger.LogInformation("Manual sync triggered via API.");
            var result = await _syncService.SyncAllAsync();
            return Ok(result);
        }

        [HttpPost("trigger/{companyCode}")]
        public async Task<ActionResult<CloudSyncResult>> TriggerTenantSync(string companyCode)
        {
            _logger.LogInformation("Manual tenant sync triggered for company {CompanyCode} via API.", companyCode);
            var result = await _syncService.SyncTenantAsync(companyCode);
            return Ok(result);
        }
    }
}
