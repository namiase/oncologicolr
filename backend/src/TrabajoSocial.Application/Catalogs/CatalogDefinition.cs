namespace TrabajoSocial.Application.Catalogs;

/// <summary>
/// Metadatos de una tabla maestra: clave de API, nombres visibles, grupo, catálogo padre y
/// género gramatical del nombre (para textos como "Nueva raza" / "Nuevo país").
/// </summary>
public sealed record CatalogDefinition(
    string Key,
    string Name,
    string SingularName,
    string Group,
    Type EntityType,
    string? ParentKey = null,
    Type? ParentEntityType = null,
    bool Feminine = false)
{
    public bool IsHierarchical => ParentKey is not null;
}
