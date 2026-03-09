using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "ProjectDora User Management",
    Author = "ProjectDora Team",
    Website = "https://projectdora.kosgeb.gov.tr",
    Version = "1.0.0",
    Description = "User, role, and permission management with unlimited user/role creation, two-level permission taxonomy, and auto-generated content-type permissions.",
    Category = "Security",
    Dependencies = new[] { "OrchardCore.Users", "OrchardCore.Roles" }
)]
