using TrabajoSocial.Domain.Common;

namespace TrabajoSocial.Domain.Entities.Catalogs;

// Tablas maestras. Todas heredan la estructura genérica de CatalogEntity
// (Code, Name, Description, SortOrder, StatusId + auditoría).

/// <summary>Sexo.</summary>
public class Gender : CatalogEntity;

/// <summary>Raza / etnia.</summary>
public class Race : CatalogEntity;

/// <summary>Estado civil.</summary>
public class MaritalStatus : CatalogEntity;

/// <summary>Religión.</summary>
public class Religion : CatalogEntity;

/// <summary>Grado de instrucción.</summary>
public class EducationLevel : CatalogEntity;

/// <summary>Profesión.</summary>
public class Profession : CatalogEntity;

/// <summary>Oficio.</summary>
public class Occupation : CatalogEntity;

/// <summary>Código de área telefónico (p. ej. 0212, 0414).</summary>
public class AreaCode : CatalogEntity;

/// <summary>Parentesco.</summary>
public class Kinship : CatalogEntity;

/// <summary>País.</summary>
public class Country : CatalogEntity
{
    public ICollection<State> States { get; set; } = [];
}

/// <summary>Estado (entidad federal). Padre: País (columna CountryId).</summary>
public class State : HierarchicalCatalogEntity<Country>
{
    public ICollection<Municipality> Municipalities { get; set; } = [];
}

/// <summary>Municipio. Padre: Estado (columna StateId).</summary>
public class Municipality : HierarchicalCatalogEntity<State>;
