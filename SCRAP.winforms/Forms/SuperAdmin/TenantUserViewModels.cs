using System;
using System.Collections.Generic;

namespace SCRAP.winforms.Forms
{
    public class TenantUserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string StatusText => IsActive ? "Active" : "Deactivated";
    }

    public class TenantBranchItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public override string ToString() => string.IsNullOrWhiteSpace(Code) ? Name : $"{Name} ({Code})";
    }

    public class TenantUsersResponse
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public List<TenantUserViewModel> Users { get; set; } = new();
        public List<TenantBranchItem> Branches { get; set; } = new();
    }
}
