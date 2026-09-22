using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using SCRAP.domain.entities;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
namespace SCRAP.infrastructure.data

{

    public class MasterErpDbContext : IdentityDbContext
    {
        public DbSet<Device> Devices { get; set; }


        public MasterErpDbContext(DbContextOptions<MasterErpDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)

        {

            base.OnModelCreating(builder);


            // existing company configuration
            builder.Entity<Company>(entity =>
            {
                entity.HasKey(x => x.CompanyId);
                entity.Property(x => x.CompanyCode).HasMaxLength(50).IsRequired();
                entity.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
                entity.HasIndex(x => x.CompanyCode).IsUnique();
            });

            builder.Entity<CompanyDatabase>(entity =>
            {
                entity.HasKey(x => x.CompanyDatabaseId);
                entity.Property(x => x.ServerName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.DatabaseName).HasMaxLength(200).IsRequired();
                entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<Device>(entity =>

            {

                entity.HasKey(x => x.DeviceId);


                    
                entity.Property(x => x.DeviceCode)

                  .HasMaxLength(50)

                  .IsRequired();



                entity.Property(x => x.DeviceName)

                  .HasMaxLength(200)

                  .IsRequired();



                entity.HasOne(x => x.Company)

                  .WithMany(x => x.Devices)

                  .HasForeignKey(x => x.CompanyId)

                  .OnDelete(DeleteBehavior.Restrict);



                entity.HasIndex(x => new { x.CompanyId, x.DeviceCode })

                  .IsUnique();

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
                entity.HasIndex(x => x.BatchCode);
                entity.HasOne(x => x.DeviceCategory).WithMany(d => d.InventoryItems).HasForeignKey(x => x.DeviceCategoryId).OnDelete(DeleteBehavior.Restrict);
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
                entity.Property(x => x.Address).HasMaxLength(500);
                entity.Property(x => x.Position).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Department).HasMaxLength(200);
                entity.Property(x => x.PayRate).HasColumnType("decimal(18,2)");
                entity.Property(x => x.Notes).HasMaxLength(1000);
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

        }


        public DbSet<Company> Companies => Set<Company>();



        public DbSet<CompanyDatabase> CompanyDatabases => Set<CompanyDatabase>();

        public DbSet<Inventory> Inventories => Set<Inventory>();

        public DbSet<DeviceCategory> DeviceCategories => Set<DeviceCategory>();

        public DbSet<Teardown> Teardowns => Set<Teardown>();

        public DbSet<ArchetypeRecipe> ArchetypeRecipes => Set<ArchetypeRecipe>();

        public DbSet<RawInventory> RawInventories => Set<RawInventory>();

        public DbSet<TeardownBatch> TeardownBatches => Set<TeardownBatch>();

        public DbSet<TeardownYield> TeardownYields => Set<TeardownYield>();

        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<CompanyFinanceTransaction> CompanyFinanceTransactions => Set<CompanyFinanceTransaction>();

        public DbSet<UserManagement> Users => Set<UserManagement>();
        public DbSet<CommoditySale> CommoditySales => Set<CommoditySale>();

        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

        public DbSet<PaidLeaveRequest> PaidLeaveRequests => Set<PaidLeaveRequest>();

        public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalances => Set<EmployeeLeaveBalance>();
    }

    


    
    } 
