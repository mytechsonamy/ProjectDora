using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "ProjectDora Query Engine",
    Author = "ProjectDora Team",
    Website = "https://projectdora.kosgeb.gov.tr",
    Version = "1.0.0",
    Description = "Query management with Lucene full-text search, SQL query execution, and saved query CRUD. Includes Turkish language analyzer and SQL safety validation.",
    Category = "Search",
    Dependencies = new[] { "OrchardCore.Queries" }
)]
