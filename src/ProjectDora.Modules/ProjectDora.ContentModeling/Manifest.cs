using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "ProjectDora Content Modeling",
    Author = "ProjectDora Team",
    Website = "https://projectdora.kosgeb.gov.tr",
    Version = "1.0.0",
    Description = "Content type, part, and field definition management. Wraps Orchard Core content definitions with custom abstraction layer.",
    Category = "Content",
    Dependencies = new[] { "OrchardCore.ContentTypes" }
)]
