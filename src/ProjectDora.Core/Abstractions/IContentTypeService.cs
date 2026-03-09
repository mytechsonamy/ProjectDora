namespace ProjectDora.Core.Abstractions;

/// <summary>
/// Content type definition operations — wraps Orchard Core's IContentDefinitionManager.
/// </summary>
public interface IContentTypeService
{
    Task<ContentTypeDto> CreateAsync(CreateContentTypeRequest request);
    Task<ContentTypeDto?> GetAsync(string typeName);
    Task<IReadOnlyList<ContentTypeDto>> ListAsync();
    Task<ContentTypeDto> UpdateAsync(string typeName, UpdateContentTypeRequest request);
    Task DeleteAsync(string typeName);
    Task<ContentTypeDto> AddFieldAsync(string typeName, AddFieldRequest request);
    Task<ContentTypeDto> RemoveFieldAsync(string typeName, string fieldName);
    Task<ContentTypeDto> AddPartAsync(string typeName, string partName);
    Task<ContentTypeDto> RemovePartAsync(string typeName, string partName);
}

public record ContentTypeDto(
    string Name,
    string DisplayName,
    string? Stereotype,
    IReadOnlyList<ContentPartDto> Parts,
    IReadOnlyList<ContentFieldDto> Fields);

public record ContentPartDto(
    string Name,
    int Position);

public record ContentFieldDto(
    string Name,
    string FieldType,
    bool Required,
    IDictionary<string, object>? Settings);

public record CreateContentTypeRequest(
    string Name,
    string DisplayName,
    string? Stereotype,
    IReadOnlyList<string>? Parts,
    IReadOnlyList<AddFieldRequest>? Fields);

public record UpdateContentTypeRequest(
    string? DisplayName,
    string? Stereotype);

public record AddFieldRequest(
    string Name,
    string FieldType,
    bool Required = false,
    IDictionary<string, object>? Settings = null);
