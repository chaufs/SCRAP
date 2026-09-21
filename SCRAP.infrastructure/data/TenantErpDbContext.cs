using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SCRAP.infrastructure.data
{
    public class TenantErpDbContext : DbContext
    {
        public TenantErpDbContext(DbContextOptions<TenantErpDbContext> options)
            : base(options)
        {

        }
        public DbSet<Product> Products => Set<Product>();



        protected override void OnModelCreating(ModelBuilder builder)

        {

            base.OnModelCreating(builder);



            builder.Entity<Product>(entity =>

            {

                entity.HasKey(x => x.ProductId);



                entity.Property(x => x.ProductCode)

                  .HasMaxLength(50)

                  .IsRequired();



                entity.Property(x => x.ProductName)

                  .HasMaxLength(200)

                  .IsRequired();



                entity.Property(x => x.UnitPrice)

                  .HasPrecision(18, 2);



                entity.HasIndex(x => x.ProductCode)

                  .IsUnique();

            });
        }
    }
}
