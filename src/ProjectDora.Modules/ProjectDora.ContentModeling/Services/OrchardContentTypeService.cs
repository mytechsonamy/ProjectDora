using System.Globalization;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.ContentModeling.Services;

public sealed class OrchardContentTypeService : IContentTypeService
{
    private readonly IContentDefinitionManager _definitionManager;

    public OrchardContentTypeService(IContentDefinitionManager definitionManager)
    {
        _definitionManager = definitionManager;
    }

    public async Task<ContentTypeDto> CreateAsync(CreateContentTypeRequest request)
    {
        await _definitionManager.AlterTypeDefinitionAsync(request.Name, type =>
        {
            type.DisplayedAs(request.DisplayName);

            if (!string.IsNullOrEmpty(request.Stereotype))
            {
                type.Stereotype(request.Stereotype);
            }

            type.Creatable()
                .Listable()
                .Draftable()
                .Versionable();

            type.WithPart("CommonPart");

            if (request.Parts is { Count: > 0 })
            {
                var position = 1;
                foreach (var partName in request.Parts)
                {
                    var pos = position++;
                    type.WithPart(partName, part => part
                        .WithSettings(new ContentTypePartSettings { Position = pos.ToString(CultureInfo.InvariantCulture) }));
                }
            }
        });

        if (request.Fields is { Count: > 0 })
        {
            foreach (var field in request.Fields)
            {
                await AddFieldInternalAsync(request.Name, field);
            }
        }

        return await GetAsync(request.Name)
            ?? throw new InvalidOperationException($"Content type '{request.Name}' was not found after creation.");
    }

    public async Task<ContentTypeDto?> GetAsync(string typeName)
    {
        var definition = await _definitionManager.GetTypeDefinitionAsync(typeName);
        return definition is null ? null : MapToDto(definition);
    }

    public async Task<IReadOnlyList<ContentTypeDto>> ListAsync()
    {
        var definitions = await _definitionManager.ListTypeDefinitionsAsync();
        return definitions.Select(MapToDto).ToList();
    }

    public async Task<ContentTypeDto> UpdateAsync(string typeName, UpdateContentTypeRequest request)
    {
        var existing = await _definitionManager.GetTypeDefinitionAsync(typeName)
            ?? throw new KeyNotFoundException($"Content type '{typeName}' not found.");

        await _definitionManager.AlterTypeDefinitionAsync(typeName, type =>
        {
            if (!string.IsNullOrEmpty(request.DisplayName))
            {
                type.DisplayedAs(request.DisplayName);
            }

            if (request.Stereotype is not null)
            {
                type.Stereotype(request.Stereotype);
            }
        });

        return await GetAsync(typeName)
            ?? throw new InvalidOperationException($"Content type '{typeName}' was not found after update.");
    }

    public async Task DeleteAsync(string typeName)
    {
        var existing = await _definitionManager.GetTypeDefinitionAsync(typeName)
            ?? throw new KeyNotFoundException($"Content type '{typeName}' not found.");

        await _definitionManager.DeleteTypeDefinitionAsync(typeName);
    }

    public async Task<ContentTypeDto> AddFieldAsync(string typeName, AddFieldRequest request)
    {
        _ = await _definitionManager.GetTypeDefinitionAsync(typeName)
            ?? throw new KeyNotFoundException($"Content type '{typeName}' not found.");

        await AddFieldInternalAsync(typeName, request);

        return await GetAsync(typeName)
            ?? throw new InvalidOperationException($"Content type '{typeName}' was not found after adding field.");
    }

    public async Task<ContentTypeDto> RemoveFieldAsync(string typeName, string fieldName)
    {
        var existing = await _definitionManager.GetTypeDefinitionAsync(typeName)
            ?? throw new KeyNotFoundException($"Content type '{typeName}' not found.");

        await _definitionManager.AlterPartDefinitionAsync(typeName, part =>
        {
            part.RemoveField(fieldName);
        });

        return await GetAsync(typeName)
            ?? throw new InvalidOperationException($"Content type '{typeName}' was not found after removing field.");
    }

    public async Task<ContentTypeDto> AddPartAsync(string typeName, string partName)
    {
        _ = await _definitionManager.GetTypeDefinitionAsync(typeName)
            ?? throw new KeyNotFoundException($"Content type '{typeName}' not found.");

        await _definitionManager.AlterTypeDefinitionAsync(typeName, type =>
        {
            type.WithPart(partName);
        });

        return await GetAsync(typeName)
            ?? throw new InvalidOperationException($"Content type '{typeName}' was not found after adding part.");
    }

    public async Task<ContentTypeDto> RemovePartAsync(string typeName, string partName)
    {
        _ = await _definitionManager.GetTypeDefinitionAsync(typeName)
            ?? throw new KeyNotFoundException($"Content type '{typeName}' not found.");

        await _definitionManager.AlterTypeDefinitionAsync(typeName, type =>
        {
            type.RemovePart(partName);
        });

        return await GetAsync(typeName)
            ?? throw new InvalidOperationException($"Content type '{typeName}' was not found after removing part.");
    }

    private async Task AddFieldInternalAsync(string typeName, AddFieldRequest request)
    {
        await _definitionManager.AlterPartDefinitionAsync(typeName, part =>
        {
            part.WithField(request.Name, field =>
            {
                field.OfType(request.FieldType);
            });
        });
    }

    private static ContentTypeDto MapToDto(ContentTypeDefinition definition)
    {
        var parts = definition.Parts
            .Select((p, i) => new ContentPartDto(p.Name, i))
            .ToList();

        var fields = new List<ContentFieldDto>();

        var selfPart = definition.Parts
            .FirstOrDefault(p => p.Name == definition.Name);

        if (selfPart?.PartDefinition?.Fields is not null)
        {
            foreach (var field in selfPart.PartDefinition.Fields)
            {
                fields.Add(new ContentFieldDto(
                    field.Name,
                    field.FieldDefinition.Name,
                    false,
                    null));
            }
        }

        return new ContentTypeDto(
            definition.Name,
            definition.DisplayName,
            definition.GetStereotype(),
            parts,
            fields);
    }
}
