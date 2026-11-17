using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Entities.Catalogs;

namespace TrabajoSocial.Application.Catalogs;

/// <summary>
/// Registro central de tablas maestras. Para agregar un catálogo nuevo:
/// 1) crear la entidad (hereda de CatalogEntity), 2) su configuración EF y DbSet,
/// 3) agregarlo aquí. El CRUD (API y pantalla) queda disponible automáticamente.
/// </summary>
public static class CatalogRegistry
{
    public static class Groups
    {
        public const string Personal = "Datos personales";
        public const string Location = "Ubicación";
        public const string Education = "Formación y trabajo";
        public const string Family = "Familia";
    }

    private static readonly CatalogDefinition[] Definitions =
    [
        Define<Gender>("genders", "Sexos", "Sexo", Groups.Personal),
        Define<Race>("races", "Razas", "Raza", Groups.Personal, feminine: true),
        Define<MaritalStatus>("marital-statuses", "Estados civiles", "Estado civil", Groups.Personal),
        Define<Religion>("religions", "Religiones", "Religión", Groups.Personal, feminine: true),

        Define<Country>("countries", "Países", "País", Groups.Location),
        Define<State>("states", "Estados", "Estado", Groups.Location, parentKey: "countries"),
        Define<Municipality>("municipalities", "Municipios", "Municipio", Groups.Location, parentKey: "states"),
        Define<AreaCode>("area-codes", "Códigos de área", "Código de área", Groups.Location),

        Define<EducationLevel>("education-levels", "Grados de instrucción", "Grado de instrucción", Groups.Education),
        Define<Profession>("professions", "Profesiones", "Profesión", Groups.Education, feminine: true),
        Define<Occupation>("occupations", "Oficios", "Oficio", Groups.Education),

        Define<Kinship>("kinships", "Parentescos", "Parentesco", Groups.Family),
    ];

    private static readonly Dictionary<string, CatalogDefinition> ByKey =
        Definitions.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<CatalogDefinition> All => Definitions;

    public static bool TryGet(string key, out CatalogDefinition definition)
        => ByKey.TryGetValue(key, out definition!);

    /// <summary>Catálogos cuyo padre es <paramref name="parentKey"/>.</summary>
    public static IEnumerable<CatalogDefinition> GetChildren(string parentKey)
        => Definitions.Where(d => string.Equals(d.ParentKey, parentKey, StringComparison.OrdinalIgnoreCase));

    private static CatalogDefinition Define<TEntity>(
        string key, string name, string singularName, string group, string? parentKey = null, bool feminine = false)
        where TEntity : CatalogEntity
    {
        var parentType = GetParentType(typeof(TEntity));
        if ((parentKey is null) != (parentType is null))
        {
            throw new InvalidOperationException(
                $"El catálogo '{key}' debe declarar parentKey si y solo si hereda de HierarchicalCatalogEntity<>.");
        }

        return new CatalogDefinition(key, name, singularName, group, typeof(TEntity), parentKey, parentType, feminine);
    }

    private static Type? GetParentType(Type entityType)
    {
        for (var type = entityType.BaseType; type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HierarchicalCatalogEntity<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }
}
