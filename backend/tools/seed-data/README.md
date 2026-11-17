# Datos de referencia (países, estados y municipios)

`build-seed.mjs` genera los JSON de `src/TrabajoSocial.Infrastructure/Persistence/Seed/Data/` a partir de fuentes oficiales y valida conteos y duplicados.

| Archivo | Fuente |
|---|---|
| `countries.json` | Códigos ISO 3166-1 alpha-2 ([lukes/ISO-3166-Countries-with-Regional-Codes](https://github.com/lukes/ISO-3166-Countries-with-Regional-Codes)) con los nombres en español de Unicode CLDR (`Intl.DisplayNames` de Node). |
| `geo-br.json` | IBGE: `https://servicodados.ibge.gov.br/api/v1/localidades/estados` y `/municipios`. |
| `geo-co.json` | DANE (DIVIPOLA) en datos.gov.co: departamentos `vcjz-niiq` y municipios `gdxc-w37w`. Los nombres vienen en mayúsculas y se normalizan a formato título. |
| `geo-ve.json` | Municipios de [zokeber/venezuela-json](https://github.com/zokeber/venezuela-json), validados contra el total oficial de 335. Los códigos de estado se toman de ISO 3166-2:VE porque los del dataset son incorrectos. |

Para regenerar los archivos:

1. Descargue las fuentes en una carpeta con esta estructura: `br/estados.json`, `br/municipios.json`, `co/departamentos.json`, `co/municipios.json`, `ve/venezuela.json`, `iso/iso3166.json`.
2. Ejecute:

   ```bash
   node build-seed.mjs <carpeta-descargas> ../../src/TrabajoSocial.Infrastructure/Persistence/Seed/Data
   ```

Si los datos cambian, cree un paquete nuevo en `ReferenceDataSeeder` (por ejemplo `geo:BR:v2`). Los paquetes ya aplicados no se vuelven a ejecutar.
