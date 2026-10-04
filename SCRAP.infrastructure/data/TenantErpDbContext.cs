using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using System;
using System.Collections.Generic;

namespace SCRAP.infrastructure.data
{
    public class TenantErpDbContext : DbContext
    {
        public TenantErpDbContext(DbContextOptions<TenantErpDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<UserManagement> Users => Set<UserManagement>();
        public DbSet<DeviceCategory> DeviceCategories => Set<DeviceCategory>();
        public DbSet<ArchetypeRecipe> ArchetypeRecipes => Set<ArchetypeRecipe>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<Teardown> Teardowns => Set<Teardown>();
        public DbSet<TeardownBatch> TeardownBatches => Set<TeardownBatch>();
        public DbSet<TeardownYield> TeardownYields => Set<TeardownYield>();
        public DbSet<RawInventory> RawInventories => Set<RawInventory>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<CommoditySale> CommoditySales => Set<CommoditySale>();
        public DbSet<CompanyFinanceTransaction> CompanyFinanceTransactions => Set<CompanyFinanceTransaction>();
        public DbSet<StorageDestructionRecord> StorageDestructionRecords => Set<StorageDestructionRecord>();
        public DbSet<CertificateOfDestruction> CertificatesOfDestruction => Set<CertificateOfDestruction>();
        public DbSet<CertificateOfDestructionItem> CertificateOfDestructionItems => Set<CertificateOfDestructionItem>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
        public DbSet<PaidLeaveRequest> PaidLeaveRequests => Set<PaidLeaveRequest>();
        public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalances => Set<EmployeeLeaveBalance>();
        public DbSet<ProcurementRequest> ProcurementRequests => Set<ProcurementRequest>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Product
            builder.Entity<Product>(entity =>
            {
                entity.HasKey(x => x.ProductId);
                entity.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
                entity.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
                entity.HasIndex(x => x.ProductCode).IsUnique();
            });

            // Branch
            builder.Entity<Branch>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Address).HasMaxLength(500);
                entity.HasIndex(x => x.Code).IsUnique();
            });

            // Inventory
            builder.Entity<Inventory>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.DeviceName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.SerialNumber).HasMaxLength(200);
                entity.Property(x => x.BatchCode).HasMaxLength(200);
                entity.Property(x => x.PurchasedFrom).HasMaxLength(300);
                entity.Property(x => x.PurchaseCost).HasColumnType("decimal(18,2)");
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.Property(x => x.RecordedByUsername).HasMaxLength(200);
                entity.HasIndex(x => x.BatchCode);
                entity.HasOne(x => x.DeviceCategory).WithMany(d => d.InventoryItems).HasForeignKey(x => x.DeviceCategoryId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            });

            // DeviceCategory
            builder.Entity<DeviceCategory>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(1000);
            });

            // Teardown
            builder.Entity<Teardown>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.MaterialsRecovered).HasMaxLength(1000);
                entity.Property(x => x.PerformedBy).HasMaxLength(200);
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.HasOne(x => x.InventoryItem).WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
            });

            // ArchetypeRecipe
            builder.Entity<ArchetypeRecipe>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.MaterialName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.WeightKgPerUnit).HasColumnType("decimal(18,4)");
                entity.HasOne(x => x.DeviceCategory).WithMany().HasForeignKey(x => x.DeviceCategoryId).OnDelete(DeleteBehavior.Cascade);
            });

            // RawInventory
            builder.Entity<RawInventory>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.MaterialName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.CurrentTotalWeightKg).HasColumnType("decimal(18,4)");
            });

            // TeardownBatch and Yields
            builder.Entity<TeardownBatch>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasOne(x => x.DeviceCategory).WithMany().HasForeignKey(x => x.DeviceCategoryId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<TeardownYield>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.MaterialName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.WeightKg).HasColumnType("decimal(18,4)");
                entity.HasOne(x => x.TeardownBatch).WithMany(t => t.Yields).HasForeignKey(x => x.TeardownBatchId).OnDelete(DeleteBehavior.Cascade);
            });

            // Sales
            builder.Entity<Sale>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.ItemDescription).HasMaxLength(500);
                entity.Property(x => x.BuyerName).HasMaxLength(200);
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            });

            builder.Entity<CompanyFinanceTransaction>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
                entity.Property(x => x.SourceReference).HasMaxLength(100);
                entity.HasIndex(x => x.SourceReference).IsUnique().HasFilter("[SourceReference] IS NOT NULL");
                entity.HasIndex(x => x.TransactionDate);
                entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            });

            // UserManagement
            builder.Entity<UserManagement>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.MiddleName).HasMaxLength(100);
                entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Email).HasMaxLength(200);
                entity.Property(x => x.Username).HasMaxLength(100);
                entity.Property(x => x.PasswordHash).HasMaxLength(1000);
                entity.Property(x => x.Role)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                entity.HasOne(x => x.Branch).WithMany(b => b.Users).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<StorageDestructionRecord>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasOne(x => x.Inventory).WithMany().HasForeignKey(x => x.InventoryId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.TeardownBatch).WithMany().HasForeignKey(x => x.TeardownBatchId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CertificateOfDestruction>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.CertificateNumber).HasMaxLength(50).IsRequired();
                entity.HasIndex(x => x.CertificateNumber).IsUnique();
                entity.Property(x => x.OrganizationName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.OrganizationAddress).HasMaxLength(500);
                entity.Property(x => x.ProviderName).HasMaxLength(200);
                entity.Property(x => x.ProviderAddress).HasMaxLength(500);
                entity.Property(x => x.SecurityStandard).HasMaxLength(200);
                entity.Property(x => x.SoftwareToolName).HasMaxLength(200);
                entity.Property(x => x.SoftwareToolVersion).HasMaxLength(50);
                entity.Property(x => x.VerifiedByName).HasMaxLength(200);
                entity.Property(x => x.Notes).HasMaxLength(1000);
            });

            builder.Entity<CertificateOfDestructionItem>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.SerialNumber).HasMaxLength(200);
                entity.Property(x => x.DeviceType).HasMaxLength(200);
                entity.Property(x => x.Model).HasMaxLength(200);
                entity.HasOne(x => x.CertificateOfDestruction).WithMany(c => c.Items).HasForeignKey(x => x.CertificateOfDestructionId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CommoditySale>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.MaterialName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.BuyerName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.QuantityKg).HasColumnType("decimal(18,4)");
                entity.Property(x => x.PricePerKg).HasColumnType("decimal(18,2)");
                entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
                entity.HasIndex(x => x.InvoiceNumber).IsUnique();
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Employee>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
                entity.HasIndex(x => x.EmployeeCode).IsUnique();
                entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.MiddleName).HasMaxLength(100);
                entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.ContactNumber).HasMaxLength(50);
                entity.Property(x => x.EmailAddress).HasMaxLength(200);
                entity.Property(x => x.Street).HasMaxLength(300);
                entity.Property(x => x.Barangay).HasMaxLength(200);
                entity.Property(x => x.City).HasMaxLength(200);
                entity.Property(x => x.Province).HasMaxLength(200);
                entity.Property(x => x.Country).HasMaxLength(200);
                entity.Property(x => x.Position).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Department).HasMaxLength(200);
                entity.Property(x => x.PayRate).HasColumnType("decimal(18,2)");
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AttendanceRecord>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.AttendanceDate).HasColumnType("date");
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.Notes).HasMaxLength(500);
                entity.HasIndex(x => new { x.EmployeeId, x.AttendanceDate }).IsUnique();
                entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PaidLeaveRequest>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.StartDate).HasColumnType("date");
                entity.Property(x => x.EndDate).HasColumnType("date");
                entity.Property(x => x.DaysRequested).HasColumnType("decimal(18,2)");
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
                entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<EmployeeLeaveBalance>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.MaximumPaidLeaveDays).HasColumnType("decimal(18,2)");
                entity.Property(x => x.UsedPaidLeaveDays).HasColumnType("decimal(18,2)");
                entity.HasIndex(x => new { x.EmployeeId, x.LeaveYear }).IsUnique();
                entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            });

            // ProcurementRequest
            builder.Entity<ProcurementRequest>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.SupplierCompany).HasMaxLength(300).IsRequired();
                entity.Property(x => x.DeviceName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.TotalCost).HasColumnType("decimal(18,2)");
                entity.Property(x => x.CostPerDevice).HasColumnType("decimal(18,2)");
                entity.Property(x => x.SerialNumber).HasMaxLength(200);
                entity.Property(x => x.BatchCode).HasMaxLength(200);
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
                entity.Property(x => x.RequestedByUserName).HasMaxLength(100);
                entity.Property(x => x.RequestedByFullName).HasMaxLength(200);
                entity.Property(x => x.ReviewedByUserName).HasMaxLength(100);
                entity.Property(x => x.ReviewedByFullName).HasMaxLength(200);
                entity.Property(x => x.RejectionReason).HasMaxLength(500);
                entity.Property(x => x.AssignedTechStaffUserName).HasMaxLength(100);
                entity.Property(x => x.AssignedTechStaffFullName).HasMaxLength(200);
                entity.Property(x => x.CompletedByUserName).HasMaxLength(100);
                entity.HasOne(x => x.DeviceCategory).WithMany().HasForeignKey(x => x.DeviceCategoryId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
