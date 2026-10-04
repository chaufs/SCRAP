using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;

namespace SCRAP.infrastructure.data
{
    public class MasterErpDbContext : IdentityDbContext
    {
        public MasterErpDbContext(DbContextOptions<MasterErpDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CompanyDatabase> CompanyDatabases => Set<CompanyDatabase>();
        public DbSet<SuperAdminUser> SuperAdmins => Set<SuperAdminUser>();
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<SubscriptionHistory> SubscriptionHistories => Set<SubscriptionHistory>();
        public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Company (Subscriber)
            builder.Entity<Company>(entity =>
            {
                entity.HasKey(x => x.CompanyId);
                entity.Property(x => x.CompanyCode).HasMaxLength(50).IsRequired();
                entity.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
                entity.HasIndex(x => x.CompanyCode).IsUnique();
            });

            // Company Database Mapping
            builder.Entity<CompanyDatabase>(entity =>
            {
                entity.HasKey(x => x.CompanyDatabaseId);
                entity.Property(x => x.ServerName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.DatabaseName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.CredentialKey).HasMaxLength(100);
                entity.Property(x => x.DatabaseType).HasMaxLength(20).HasDefaultValue("Local");
                entity.Property(x => x.ConnectionString).HasMaxLength(1000);
                entity.HasOne(x => x.Company)
                      .WithMany()
                      .HasForeignKey(x => x.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // SuperAdminUser (Platform Developer Accounts)
            builder.Entity<SuperAdminUser>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
                entity.HasIndex(x => x.Username).IsUnique();
                entity.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
                entity.Property(x => x.Email).HasMaxLength(200);
                entity.Property(x => x.FullName).HasMaxLength(200);
            });

            // Legacy / master-level Device (if needed)
            builder.Entity<Device>(entity =>
            {
                entity.HasKey(x => x.DeviceId);
                entity.Property(x => x.DeviceCode).HasMaxLength(50).IsRequired();
                entity.Property(x => x.DeviceName).HasMaxLength(200).IsRequired();
                entity.HasOne(x => x.Company)
                      .WithMany(x => x.Devices)
                      .HasForeignKey(x => x.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(x => new { x.CompanyId, x.DeviceCode }).IsUnique();
            });

            // Platform Settings (key-value store for SuperAdmin-managed content)
            builder.Entity<PlatformSetting>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
                entity.HasIndex(x => x.Key).IsUnique();
                entity.Property(x => x.Value).HasColumnType("nvarchar(max)");
                entity.Property(x => x.UpdatedBy).HasMaxLength(100);
            });
        }
    }
}
