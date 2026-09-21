using System;
using System.Linq;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.infrastructure
{
    public static class DataSeeder
    {
        public static void EnsureSeed(MasterErpDbContext db)
        {
            // Ensure the database exists. If it cannot be created here (migrations not applied), exit silently.
            try
            {
                db.Database.EnsureCreated();
            }
            catch
            {
                // If EnsureCreated throws (e.g., invalid connection), skip seeding.
                return;
            }

            try
            {
                if (!db.Users.Any())
            {

                    db.Users.Add(new UserManagement { FirstName = "Admin", LastName = "User", Username = "admin", Email = "admin@local", PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("adminpass")), Role = UserRole.Admin, IsActive = true });
                    db.Users.Add(new UserManagement { FirstName = "Tech", LastName = "User", Username = "tech", Email = "tech@local", PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("techpass")), Role = UserRole.TechStaff, IsActive = true });
                    db.Users.Add(new UserManagement { FirstName = "Sales", LastName = "User", Username = "sales", Email = "sales@local", PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("salespass")), Role = UserRole.SalesStaff, IsActive = true });
                    db.Users.Add(new UserManagement { FirstName = "Manager", LastName = "User", Username = "manager", Email = "manager@local", PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("managerpass")), Role = UserRole.Manager, IsActive = true });
                    db.SaveChanges();
                }
            }
            catch
            {
                // If Users query fails (table missing), abort seeding.
                return;
            }

            try
            {
                if (!db.DeviceCategories.Any())
                {
                    db.DeviceCategories.Add(new DeviceCategory { Name = "Standard Laptop", Description = "Generic laptop" });
                    db.DeviceCategories.Add(new DeviceCategory { Name = "Desktop PC", Description = "Standard desktop" });
                    db.SaveChanges();
                }
            }
            catch
            {
                return;
            }

            // ArchetypeRecipes seeding is optional; wrap in try/catch to avoid errors if table missing
            try
            {
                if (!db.ArchetypeRecipes.Any())
                {
                    var laptop = db.DeviceCategories.FirstOrDefault(d => d.Name == "Standard Laptop");
                    if (laptop != null)
                    {
                        db.ArchetypeRecipes.Add(new ArchetypeRecipe { DeviceCategoryId = laptop.Id, MaterialName = "Copper", WeightKgPerUnit = 0.5m });
                        db.ArchetypeRecipes.Add(new ArchetypeRecipe { DeviceCategoryId = laptop.Id, MaterialName = "Plastic", WeightKgPerUnit = 0.2m });
                    }

                    var desktop = db.DeviceCategories.FirstOrDefault(d => d.Name == "Desktop PC");
                    if (desktop != null)
                    {
                        db.ArchetypeRecipes.Add(new ArchetypeRecipe { DeviceCategoryId = desktop.Id, MaterialName = "Copper", WeightKgPerUnit = 0.7m });
                        db.ArchetypeRecipes.Add(new ArchetypeRecipe { DeviceCategoryId = desktop.Id, MaterialName = "Plastic", WeightKgPerUnit = 0.3m });
                    }
                    db.SaveChanges();
                }
            }
            catch
            {
                // ignore if archetype table missing
            }
        }
    }
}
