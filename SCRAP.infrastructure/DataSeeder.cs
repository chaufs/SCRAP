using System;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.infrastructure
{
    public static class DataSeeder
    {
        public static void EnsureMasterSeed(MasterErpDbContext masterDb)
        {
            try
            {
                string hash(string pw) => Convert.ToBase64String(Encoding.UTF8.GetBytes(pw));

                // 1. Seed SuperAdmin (Platform Developer)
                if (!masterDb.SuperAdmins.Any(x => x.Username == "superadmin"))
                {
                    masterDb.SuperAdmins.Add(new SuperAdminUser
                    {
                        Username = "superadmin",
                        PasswordHash = hash("SuperAdmin@123"),
                        Email = "developer@scrap-platform.local",
                        FullName = "Platform Super Admin (Developer)",
                        IsActive = true
                    });
                    masterDb.SaveChanges();
                }

                // 2. Ensure default demo company subscriber exists
                var demoCompany = masterDb.Companies.FirstOrDefault(c => c.CompanyCode == "DEMO");
                if (demoCompany == null)
                {
                    demoCompany = new Company
                    {
                        CompanyCode = "DEMO",
                        CompanyName = "Demo Recycling Corp.",
                        IsActive = true
                    };
                    masterDb.Companies.Add(demoCompany);
                    masterDb.SaveChanges();
                }

                // 3. Ensure demo company database record exists
                if (!masterDb.CompanyDatabases.Any(cd => cd.CompanyId == demoCompany.CompanyId))
                {
                    masterDb.CompanyDatabases.Add(new CompanyDatabase
                    {
                        CompanyId = demoCompany.CompanyId,
                        ServerName = "DESKTOPPOL\\geofferpserver",
                        DatabaseName = "DB_Tenant_DEMO",
                        CredentialKey = "DefaultTenant",
                        IsActive = true
                    });
                    masterDb.SaveChanges();
                }
            }
            catch
            {
                // Silently skip if master database is not yet migrated
            }
        }

        public static void EnsureTenantSeed(TenantErpDbContext tenantDb)
        {
            try
            {
                // Ensure ProcurementRequests table exists
                tenantDb.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcurementRequests')
                    BEGIN
                        CREATE TABLE [ProcurementRequests] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [SupplierCompany] NVARCHAR(300) NOT NULL,
                            [DeviceName] NVARCHAR(200) NOT NULL,
                            [DeviceCategoryId] INT NOT NULL,
                            [Quantity] INT NOT NULL DEFAULT 1,
                            [TotalCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                            [CostPerDevice] DECIMAL(18,2) NOT NULL DEFAULT 0,
                            [HasStorageDevice] BIT NOT NULL DEFAULT 0,
                            [SerialNumber] NVARCHAR(200) NULL,
                            [BatchCode] NVARCHAR(200) NULL,
                            [Notes] NVARCHAR(1000) NULL,
                            [Status] NVARCHAR(30) NOT NULL DEFAULT 'PendingApproval',
                            [RequestedByUserId] INT NULL,
                            [RequestedByUserName] NVARCHAR(100) NULL,
                            [RequestedByFullName] NVARCHAR(200) NULL,
                            [RequestedAtUtc] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                            [ReviewedByUserId] INT NULL,
                            [ReviewedByUserName] NVARCHAR(100) NULL,
                            [ReviewedByFullName] NVARCHAR(200) NULL,
                            [ReviewedAtUtc] DATETIME2 NULL,
                            [RejectionReason] NVARCHAR(500) NULL,
                            [AssignedTechStaffUserId] INT NULL,
                            [AssignedTechStaffUserName] NVARCHAR(100) NULL,
                            [AssignedTechStaffFullName] NVARCHAR(200) NULL,
                            [AssignedAtUtc] DATETIME2 NULL,
                            [CompletedAtUtc] DATETIME2 NULL,
                            [CompletedByUserName] NVARCHAR(100) NULL,
                            [BranchId] INT NOT NULL DEFAULT 1,
                            CONSTRAINT [FK_ProcurementRequests_DeviceCategories_DeviceCategoryId] FOREIGN KEY ([DeviceCategoryId]) REFERENCES [DeviceCategories] ([Id]),
                            CONSTRAINT [FK_ProcurementRequests_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id])
                        );
                    END
                ");

                EnsureCommoditiesAndRecipes(tenantDb);

                // If tenant already has users seeded, preserve existing differentiated data
                if (tenantDb.Users.Any())
                {
                    return;
                }
                Branch? branch1 = tenantDb.Branches.FirstOrDefault(b => b.Code == "BR-001");
                if (branch1 == null)
                {
                    branch1 = new Branch
                    {
                        Name = "Davao Main Branch",
                        Code = "BR-001",
                        Address = "Bangkal, Davao City, Davao del Sur, Philippines",
                        IsActive = true
                    };
                    tenantDb.Branches.Add(branch1);
                    tenantDb.SaveChanges();
                }

                Branch? branch2 = tenantDb.Branches.FirstOrDefault(b => b.Code == "BR-002");
                if (branch2 == null)
                {
                    branch2 = new Branch
                    {
                        Name = "Cebu Processing Branch",
                        Code = "BR-002",
                        Address = "Mandaue Industrial Park, Cebu, Central Visayas, Philippines",
                        IsActive = true
                    };
                    tenantDb.Branches.Add(branch2);
                    tenantDb.SaveChanges();
                }

                // 2. Users (Role-based logins)
                string hash(string pw) => Convert.ToBase64String(Encoding.UTF8.GetBytes(pw));

                void EnsureUser(string username, string fn, string ln, string email, string pw, UserRole role, int? bId)
                {
                    var u = tenantDb.Users.FirstOrDefault(x => x.Username == username);
                    if (u == null)
                    {
                        tenantDb.Users.Add(new UserManagement
                        {
                            Username = username,
                            FirstName = fn,
                            LastName = ln,
                            Email = email,
                            PasswordHash = hash(pw),
                            Role = role,
                            BranchId = bId,
                            IsActive = true
                        });
                    }
                    else
                    {
                        u.Role = role;
                        u.BranchId = bId;
                        u.IsActive = true;
                    }
                }

                EnsureUser("admin", "Company", "Admin", "admin@scrap.local", "adminpass", UserRole.Admin, null);
                EnsureUser("manager", "Roberto", "Mendoza", "manager.dvo@scrap.local", "managerpass", UserRole.Manager, branch1.Id);
                EnsureUser("tech", "Marcus", "Vance", "tech.dvo@scrap.local", "techpass", UserRole.TechStaff, branch1.Id);
                EnsureUser("angelo", "Angelo", "Rosales", "angelo@scrap.local", "dGVjaHBhc3M=", UserRole.TechStaff, branch1.Id);
                EnsureUser("sales", "Christian", "Bautista", "sales.dvo@scrap.local", "salespass", UserRole.SalesStaff, branch1.Id);

                EnsureUser("manager2", "Patricia", "Gomez", "manager.ceb@scrap.local", "manager2pass", UserRole.Manager, branch2.Id);
                EnsureUser("tech2", "Rafael", "Torres", "tech.ceb@scrap.local", "tech2pass", UserRole.TechStaff, branch2.Id);
                EnsureUser("sales2", "Maria", "Santos", "sales.ceb@scrap.local", "sales2pass", UserRole.SalesStaff, branch2.Id);
                EnsureUser("dj", "Daniel Jhon", "Capuno", "dj.ceb@scrap.local", "salespass", UserRole.SalesStaff, branch2.Id);
                tenantDb.SaveChanges();

                // 3. Device Categories
                DeviceCategory EnsureCategory(string name, string desc)
                {
                    var c = tenantDb.DeviceCategories.FirstOrDefault(x => x.Name == name);
                    if (c == null)
                    {
                        c = new DeviceCategory { Name = name, Description = desc };
                        tenantDb.DeviceCategories.Add(c);
                        tenantDb.SaveChanges();
                    }
                    return c;
                }

                var catLaptop = EnsureCategory("Standard Laptop", "Generic and corporate laptops");
                var catDesktop = EnsureCategory("Desktop PC", "Standard and mini tower desktop PCs");
                var catGaming = EnsureCategory("GamingLaptop", "High-performance gaming laptops with heavy heatsinks");
                var catServer = EnsureCategory("Enterprise Server", "Rackmount enterprise server nodes");
                var catNetwork = EnsureCategory("Networking Equipment", "Enterprise routers and rack switches");

                // 4. Employees
                void EnsureEmp(string code, string fn, string? mn, string ln, string phone, string email,
                               string street, string brgy, string city, string prov, string pos, string dept, decimal pay)
                {
                    var emp = tenantDb.Employees.FirstOrDefault(e => e.EmployeeCode == code);
                    if (emp == null)
                    {
                        tenantDb.Employees.Add(new Employee
                        {
                            EmployeeCode = code,
                            FirstName = fn,
                            MiddleName = mn,
                            LastName = ln,
                            ContactNumber = phone,
                            EmailAddress = email,
                            Street = street,
                            Barangay = brgy,
                            City = city,
                            Province = prov,
                            Country = "Philippines",
                            Position = pos,
                            Department = dept,
                            DateHired = DateTime.UtcNow.AddMonths(-12),
                            Status = EmploymentStatus.Active,
                            PayType = PayType.Monthly,
                            PayRate = pay
                        });
                    }
                }

                EnsureEmp("EMP-0001", "Angelo", null, "Rosales", "09171234567", "angelo.rosales@scrap.ph", "14 Bangkal Blvd", "Bangkal", "Davao City", "Davao del Sur", "Chief Technical Officer", "Technical", 48000m);
                EnsureEmp("EMP-0002", "Daniel Jhon", null, "Capuno", "09187654321", "dj.capuno@scrap.ph", "88 Reclamation Rd", "Subangdaku", "Mandaue City", "Cebu", "Chief Sales Officer", "Sales", 48000m);
                EnsureEmp("EMP-0003", "Marcus", "Gabriel", "Vance", "09223344551", "marcus.vance@scrap.ph", "12 Acacia St", "Matina Crossing", "Davao City", "Davao del Sur", "Senior Dismantling Specialist", "Operations", 35000m);
                EnsureEmp("EMP-0004", "Sarah", "Nicole", "Chen", "09223344552", "sarah.chen@scrap.ph", "45 Mangga Ave", "Buhangin", "Davao City", "Davao del Sur", "Electronics Recovery Technician", "Operations", 32000m);
                EnsureEmp("EMP-0005", "Rafael", "Luis", "Torres", "09334455663", "rafael.torres@scrap.ph", "88 Orchid Lane", "Banilad", "Mandaue City", "Cebu", "Hardware Diagnostics Tech", "Technical", 34000m);
                EnsureEmp("EMP-0006", "Elena", "Beatriz", "Morales", "09334455664", "elena.morales@scrap.ph", "23 Mabolo St", "Subangdaku", "Mandaue City", "Cebu", "Hazardous Materials Specialist", "Operations", 36000m);
                EnsureEmp("EMP-0007", "Christian", "Dave", "Bautista", "09445566775", "christian.bautista@scrap.ph", "56 Sampaguita St", "Poblacion", "Davao City", "Davao del Sur", "Commodities Broker", "Sales", 38000m);
                EnsureEmp("EMP-0008", "Maria", "Kristina", "Santos", "09445566776", "maria.santos@scrap.ph", "102 Colon Ext", "Kamputhaw", "Cebu City", "Cebu", "Key Accounts Executive", "Sales", 40000m);
                EnsureEmp("EMP-0009", "Roberto", "Miguel", "Mendoza", "09556677887", "roberto.mendoza@scrap.ph", "77 Palm Drive", "Lanang", "Davao City", "Davao del Sur", "Branch Operations Manager", "Management", 60000m);
                EnsureEmp("EMP-0010", "Patricia", "Anne", "Gomez", "09556677888", "patricia.gomez@scrap.ph", "15 Escario St", "Lahug", "Cebu City", "Cebu", "Branch General Manager", "Management", 65000m);
                EnsureEmp("EMP-0011", "Katrina", "Joy", "Villanueva", "09667788999", "katrina.villanueva@scrap.ph", "33 Rosal St", "Talamban", "Cebu City", "Cebu", "HR & Payroll Specialist", "Human Resources", 36000m);
                tenantDb.SaveChanges();

                // Link User logins to Employee records & branches
                void LinkUserToEmp(string username, string empCode, int? branchId)
                {
                    var u = tenantDb.Users.FirstOrDefault(x => x.Username == username);
                    var e = tenantDb.Employees.FirstOrDefault(x => x.EmployeeCode == empCode);
                    if (u != null && e != null)
                    {
                        e.UserId = u.Id;
                        if (branchId.HasValue)
                        {
                            e.BranchId = branchId;
                            u.BranchId = branchId;
                        }
                    }
                }

                LinkUserToEmp("angelo", "EMP-0001", branch1.Id);
                LinkUserToEmp("dj", "EMP-0002", branch2.Id);
                LinkUserToEmp("tech", "EMP-0003", branch1.Id);
                LinkUserToEmp("tech2", "EMP-0005", branch2.Id);
                LinkUserToEmp("sales", "EMP-0007", branch1.Id);
                LinkUserToEmp("sales2", "EMP-0008", branch2.Id);
                LinkUserToEmp("manager", "EMP-0009", branch1.Id);
                LinkUserToEmp("manager2", "EMP-0010", branch2.Id);
                tenantDb.SaveChanges();

                // 5. Leave balances for 2026
                foreach (var emp in tenantDb.Employees.ToList())
                {
                    if (!tenantDb.EmployeeLeaveBalances.Any(b => b.EmployeeId == emp.Id && b.LeaveYear == 2026))
                    {
                        tenantDb.EmployeeLeaveBalances.Add(new EmployeeLeaveBalance
                        {
                            EmployeeId = emp.Id,
                            LeaveYear = 2026,
                            MaximumPaidLeaveDays = 15m,
                            UsedPaidLeaveDays = 1m
                        });
                    }
                }
                tenantDb.SaveChanges();
            }
            catch
            {
                // Silently skip if table schema is currently undergoing migration
            }
        }

        private static void EnsureCommoditiesAndRecipes(TenantErpDbContext tenantDb)
        {
            try
            {
                // Ensure ArchetypeRecipes exist for each device category
                if (!tenantDb.ArchetypeRecipes.Any())
                {
                    var categories = tenantDb.DeviceCategories.ToList();
                    foreach (var cat in categories)
                    {
                        var name = cat.Name.ToLowerInvariant();
                        if (name.Contains("server") || name.Contains("blade") || name.Contains("chassis"))
                        {
                            tenantDb.ArchetypeRecipes.AddRange(
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "High-Grade Server PCB", WeightKgPerUnit = 1.80m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Pure Copper Heat Sinks", WeightKgPerUnit = 1.50m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Extruded Aluminum", WeightKgPerUnit = 3.20m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Heavy Gauge Steel Chassis", WeightKgPerUnit = 5.20m }
                            );
                        }
                        else if (name.Contains("storage") || name.Contains("san") || name.Contains("array"))
                        {
                            tenantDb.ArchetypeRecipes.AddRange(
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Storage Controller PCB", WeightKgPerUnit = 1.20m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Aluminum Drive Trays", WeightKgPerUnit = 3.10m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Galvanized Steel Enclosure", WeightKgPerUnit = 6.40m }
                            );
                        }
                        else if (name.Contains("switch") || name.Contains("router") || name.Contains("network") || name.Contains("telecom"))
                        {
                            tenantDb.ArchetypeRecipes.AddRange(
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Networking Grade Motherboards", WeightKgPerUnit = 1.10m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Telecom Copper Wire Harness", WeightKgPerUnit = 0.75m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Aluminum Faceplates & Sinks", WeightKgPerUnit = 1.60m }
                            );
                        }
                        else if (name.Contains("laptop") || name.Contains("thinkpad"))
                        {
                            tenantDb.ArchetypeRecipes.AddRange(
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Laptop Motherboard PCB", WeightKgPerUnit = 0.40m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Pure Copper Heat Sinks", WeightKgPerUnit = 0.20m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Extruded Aluminum", WeightKgPerUnit = 0.55m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Lithium-Ion Battery Packs", WeightKgPerUnit = 0.35m }
                            );
                        }
                        else if (name.Contains("pcb") || name.Contains("motherboard"))
                        {
                            tenantDb.ArchetypeRecipes.AddRange(
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Crushed High-Grade PCB", WeightKgPerUnit = 0.92m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Trace Gold & Precious Metals", WeightKgPerUnit = 0.02m }
                            );
                        }
                        else if (name.Contains("copper") || name.Contains("motor"))
                        {
                            tenantDb.ArchetypeRecipes.AddRange(
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Clean Heavy Copper #1", WeightKgPerUnit = 0.85m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Insulated Copper Scrap #2", WeightKgPerUnit = 0.15m }
                            );
                        }
                        else
                        {
                            tenantDb.ArchetypeRecipes.AddRange(
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Printed Circuit Boards", WeightKgPerUnit = 0.65m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Copper Wiring & Coils", WeightKgPerUnit = 0.50m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Recycled Aluminum", WeightKgPerUnit = 1.20m },
                                new ArchetypeRecipe { DeviceCategoryId = cat.Id, MaterialName = "Structural Scrap Steel", WeightKgPerUnit = 2.50m }
                            );
                        }
                    }
                    tenantDb.SaveChanges();
                }

                // Ensure initial RawInventories exist so sales staff can sell commodities
                if (!tenantDb.RawInventories.Any())
                {
                    var recipeMaterials = tenantDb.ArchetypeRecipes
                        .Select(r => r.MaterialName)
                        .Distinct()
                        .ToList();

                    if (!recipeMaterials.Any())
                    {
                        recipeMaterials = new List<string>
                        {
                            "High-Grade Server PCB",
                            "Pure Copper Heat Sinks",
                            "Extruded Aluminum",
                            "Heavy Gauge Steel Chassis",
                            "Storage Controller PCB",
                            "Lithium-Ion Battery Packs"
                        };
                    }

                    var random = new Random(42);
                    foreach (var mat in recipeMaterials)
                    {
                        decimal initialWeight = Math.Round((decimal)(random.NextDouble() * 150 + 50), 2);
                        tenantDb.RawInventories.Add(new RawInventory
                        {
                            MaterialName = mat,
                            CurrentTotalWeightKg = initialWeight
                        });
                    }
                    tenantDb.SaveChanges();
                }
            }
            catch
            {
                // Silently skip if table schema is currently undergoing migration
            }
        }
    }
}
