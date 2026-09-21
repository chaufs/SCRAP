using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;
using SCRAP.API.Services;
namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertificateOfDestructionController : ControllerBase
    {
        private readonly MasterErpDbContext _db;
        public CertificateOfDestructionController(MasterErpDbContext db) => _db = db;
        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> GetPdf(int id)
        {
            var cert = await _db.Set<CertificateOfDestruction>()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (cert is null) return NotFound();

            var pdfBytes = CertificatePdfGenerator.Generate(cert);
            return File(pdfBytes, "application/pdf", $"{cert.CertificateNumber}.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _db.Set<CertificateOfDestruction>()
                .Include(x => x.Items)
                .OrderByDescending(x => x.DestructionDateTime)
                .ToListAsync());

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cert = await _db.Set<CertificateOfDestruction>()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id);
            return cert is null ? NotFound() : Ok(cert);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCertificateRequest req)
        {
            if (req.StorageDestructionRecordIds == null || req.StorageDestructionRecordIds.Count == 0)
                return BadRequest("Select at least one pending storage device.");

            var records = await _db.Set<StorageDestructionRecord>()
                .Include(x => x.Inventory).ThenInclude(i => i!.DeviceCategory)
                .Where(x => req.StorageDestructionRecordIds.Contains(x.Id) && x.Status == StorageDestructionStatus.PendingDestruction)
                .ToListAsync();

            if (records.Count == 0)
                return BadRequest("No matching pending records found.");

            var certNumber = $"COD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var cert = new CertificateOfDestruction
            {
                CertificateNumber = certNumber,
                DestructionDateTime = DateTime.UtcNow,
                OrganizationName = req.OrganizationName,
                OrganizationAddress = req.OrganizationAddress,
                ProviderName = req.ProviderName,
                ProviderAddress = req.ProviderAddress,
                Method = req.Method,
                SecurityStandard = req.SecurityStandard,
                SoftwareToolName = req.SoftwareToolName,
                SoftwareToolVersion = req.SoftwareToolVersion,
                ManagerUserId = req.ManagerUserId,
                VerifiedByName = req.VerifiedByName,
                VerifiedDate = DateTime.UtcNow
            };


            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Add(cert);
                await _db.SaveChangesAsync();

                foreach (var record in records)
                {
                    _db.Add(new CertificateOfDestructionItem
                    {
                        CertificateOfDestructionId = cert.Id,
                        StorageDestructionRecordId = record.Id,
                        SerialNumber = record.Inventory?.SerialNumber ?? "",
                        DeviceType = record.Inventory?.DeviceCategory?.Name ?? "",
                        Model = record.Inventory?.DeviceName ?? ""
                    });

                    record.Status = StorageDestructionStatus.Destroyed;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return CreatedAtAction(nameof(GetById), new { id = cert.Id }, cert);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Error creating certificate", detail = ex.Message });
            }
        }

        public class CreateCertificateRequest
        {
            public List<int> StorageDestructionRecordIds { get; set; } = new();
            public string OrganizationName { get; set; } = string.Empty;
            public string OrganizationAddress { get; set; } = string.Empty;
            public string ProviderName { get; set; } = string.Empty;
            public string ProviderAddress { get; set; } = string.Empty;
            public DestructionMethod Method { get; set; }
            public string SecurityStandard { get; set; } = string.Empty;
            public string? SoftwareToolName { get; set; }
            public string? SoftwareToolVersion { get; set; }
            public int ManagerUserId { get; set; }
            public string VerifiedByName { get; set; } = string.Empty;
        }
    }
}