using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "ProjectDora Workflow Engine",
    Author = "ProjectDora Team",
    Website = "https://projectdora.kosgeb.gov.tr",
    Version = "1.0.0",
    Description = "Visual drag-and-drop workflow designer with pre-defined triggers and activities for KOSGEB business process automation.",
    Category = "Workflows",
    Dependencies = new[] { "OrchardCore.Workflows" }
)]
