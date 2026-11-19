# TrabajoSocial

Sistema web de trabajo social para la atención de pacientes oncológicos y sus familias.

- **Backend:** ASP.NET Core 10 (LTS), EF Core 10 Code First y SQL Server.
- **Frontend:** React 19, TypeScript, Vite y Tailwind CSS v4.

## Requisitos

| Herramienta | Versión |
|---|---|
| .NET SDK | 10.0.x |
| Node.js | 20 o superior (probado con 24) |
| SQL Server | Instancia local (`localhost`) con autenticación de Windows |

## Puesta en marcha

### 1. Secretos del backend

Los secretos no se guardan en el repositorio. Se configuran una sola vez con `dotnet user-secrets`:

```bash
cd backend
dotnet user-secrets set "Jwt:Key" "<clave-aleatoria-de-al-menos-32-caracteres>" --project src/TrabajoSocial.Api
dotnet user-secrets set "Seed:AdminPassword" "<contraseña-inicial-del-admin>" --project src/TrabajoSocial.Api
dotnet user-secrets list --project src/TrabajoSocial.Api   # para consultarlos
```

La cadena de conexión de desarrollo está en `src/TrabajoSocial.Api/appsettings.Development.json`:

```
Server=localhost;Database=TrabajoSocialDb;Trusted_Connection=True;TrustServerCertificate=True
```

### 2. API

```bash
cd backend
dotnet run --project src/TrabajoSocial.Api   # http://localhost:5080
```

En Development, al arrancar, la API:

- aplica las migraciones;
- carga los datos iniciales: estatus, perfiles, menús, permisos, el usuario `admin` y algunos catálogos base de Venezuela.

La documentación interactiva de la API (Scalar) está en `http://localhost:5080/scalar`.

### 3. Frontend

```bash
cd frontend
npm install
npm run dev   # http://localhost:5173
```

Vite redirige `/api` y `/uploads` a la API, así que en desarrollo no hace falta configurar CORS.

Inicie sesión con el usuario `admin` y la contraseña definida en `Seed:AdminPassword`.

## Arquitectura

```
backend/
  src/
    TrabajoSocial.Domain          Entidades y constantes (sin dependencias)
    TrabajoSocial.Application     DTOs, servicios, reglas de negocio, IRepository<T>
    TrabajoSocial.Infrastructure  EF Core (DbContext, configuraciones, migraciones),
                                  repositorio, JWT, hash de contraseñas, seeder
    TrabajoSocial.Api             Controllers, autorización por permisos, errores (ProblemDetails)
  tests/TrabajoSocial.UnitTests   xUnit + SQLite en memoria con la DI real
frontend/src/
  components/ui                   Componentes base (Button, Input, Modal, Pagination…)
  components/crud                 CRUD genérico reutilizable (CrudManager)
  components/layout               Layout, sidebar con logo, guards de sesión y permisos
  features/*                      Una carpeta por módulo (auth, dashboard, master-tables…)
```

### Datos

- **Auditoría:** todas las entidades heredan de `BaseEntity`, con `Id`, `CreatedAt/By`, `UpdatedAt/By` e `IsDeleted`. Un interceptor de EF Core completa estos campos automáticamente.
- **Fechas:** se guardan en UTC.
- **Borrado lógico:** un `DELETE` marca `IsDeleted = 1` y asigna el estatus *Eliminado*. Un filtro global oculta esos registros.
- **Unicidad:** los índices únicos se filtran por `IsDeleted = 0`, así que un nombre eliminado se puede volver a usar.
- **Esquemas SQL:**
  - `security`: usuarios, perfiles, menús, permisos y refresh tokens.
  - `catalog`: estatus y tablas maestras.
  - `dbo`: datos del negocio.
- **Datos de referencia precargados:** se cargan solos en el primer arranque, tardan unos 7 segundos y vienen de fuentes oficiales.

  | Catálogo | Contenido | Fuente |
  |---|---|---|
  | Países | 249, con Venezuela, Colombia y Brasil primero | ISO 3166-1 y CLDR |
  | Venezuela | 25 estados y 335 municipios | — |
  | Colombia | 33 departamentos y 1.122 municipios y áreas no municipalizadas | DANE |
  | Brasil | 27 estados y 5.571 municipios | IBGE |
  | Listas | Religiones, profesiones y oficios | — |

  - Cada paquete se aplica **una sola vez** y queda registrado en `dbo.SeedHistory`, así que lo que el administrador modifique o elimine después no se sobrescribe.
  - Los archivos están en `Infrastructure/Persistence/Seed/Data`. Para regenerarlos, ver `backend/tools/seed-data`.

### Autenticación, sesión y seguridad

| Elemento | Política |
|---|---|
| Access token (JWT) | 15 min, solo en memoria del navegador. Lleva el Id de sesión (`sid`). |
| Refresh token | Cookie `HttpOnly; Secure; SameSite=Strict`, guardado como hash SHA-256 y rotado en cada uso. |
| Inactividad | 30 min (`Session:IdleTimeoutMinutes`). Aviso 2 min antes y cierre automático. |
| Duración máxima | 12 h por sesión aunque haya actividad (`Session:AbsoluteTimeoutHours`). |
| Cerrar sesión | Invalida **al instante** el access token: el API rechaza su `sid` aunque no haya vencido. |
| Contraseñas | PBKDF2 (100.000 iteraciones). Bloqueo de 15 min tras 5 intentos y límite de 10 logins/min por IP. El bloqueo solo impide entrar: no cierra sesiones ya abiertas, y la respuesta es la misma que con una contraseña incorrecta. |
| Clave temporal | La contraseña que asigna un administrador (alta o restablecimiento) debe cambiarse al entrar. Hasta entonces el API rechaza todo lo demás (403). |

**Cómo se renueva el token sin saturar el API**

- La actividad (mouse, teclado, scroll, touch) se detecta en el navegador, sin llamadas al servidor.
- El token se renueva **una vez por ciclo de 15 min y solo si hubo actividad**. Una pestaña inactiva no hace ninguna llamada. Como referencia, 500 usuarias activas generan unas 0,6 peticiones por segundo.
- Las pestañas del mismo navegador comparten la sesión (`BroadcastChannel`): solo una renueva, y cerrar sesión en una cierra todas.
- El servidor aplica los 30 minutos por su cuenta: si nadie renovó la sesión en ese tiempo (por ejemplo, se cerró el navegador), la rechaza.

**Revocación en memoria**

- La lista de sesiones cerradas vive en memoria (`ISessionRevocationStore`), así que validar cada petición no consulta la base de datos.
- Una sesión cerrada se mantiene en la lista lo que dura un access token, más la tolerancia de reloj y un margen. Se agrega solo cuando el cierre se guardó en la base de datos.
- Al reiniciar, el API la recarga desde `security.UserSessions`. Si la base de datos no responde, reintenta, y si no lo logra, el API no arranca: arrancar con la lista vacía volvería a aceptar sesiones cerradas.
- Si se despliegan varias instancias del API, hay que reemplazarla por una implementación distribuida (por ejemplo, Redis).

**Otras protecciones**

- El tiempo de respuesta del login no revela si un usuario existe: siempre se calcula un hash.
- Cabeceras de seguridad en el API: CSP, `X-Frame-Options`, `nosniff`, `Referrer-Policy` y `Cache-Control: no-store` en `/api`.
- El build del frontend incluye una CSP que solo permite scripts propios.
- Permisos por perfil y menú (`RoleMenu`), validados en el API con `[HasPermission(...)]`.

**Checklist de producción**

1. **HTTPS obligatorio**, con certificado válido. Sin él, la contraseña viaja sin cifrar. El API ya redirige a HTTPS y envía HSTS.
2. Configurar en el servidor que sirve el frontend las mismas cabeceras que `preview.headers` en `vite.config.ts` (CSP con `frame-ancestors 'none'`). `npm run preview` permite probarlas.
3. Si hay un proxy inverso delante del API, configurar `ForwardedHeaders` para que el límite de logins vea la IP real de cada cliente.
4. Secretos (`Jwt:Key`, cadena de conexión) en variables de entorno o en un gestor de secretos, nunca en `appsettings.json`.

### Usuarios

En *Configuración → Usuarios* (permiso `USERS`) se pueden crear, editar y eliminar cuentas, filtrarlas por perfil o estatus y vincularlas con una trabajadora social (una por usuario).

| Acción | Efecto |
|---|---|
| Crear | Exige una contraseña inicial que cumpla la política (mínimo 8 caracteres, letras y números, sin contener el nombre de usuario). Es temporal: el usuario debe cambiarla en su primer ingreso. El nombre de usuario se guarda en minúsculas y no se puede cambiar. |
| Cambiar perfil o estatus | Cierra de inmediato las sesiones del usuario, que debe volver a entrar con sus nuevos permisos. |
| Restablecer contraseña | Asigna una temporal, desbloquea la cuenta y cierra sus sesiones. El usuario debe cambiarla al entrar. |
| Desbloquear | Quita el bloqueo por intentos fallidos. |
| Autorizar para crear administradores (botón de escudo) | Solo lo ve el administrador principal, en las filas de administradores. Cierra las sesiones del usuario para que entre con el permiso nuevo. |
| Cambiar contraseña (botón de llave del encabezado) | Cada usuario cambia la suya. Su sesión actual sigue abierta y se cierran las de otros equipos. |

**Protecciones:**

- Nadie puede eliminarse, desactivarse ni cambiar su propio perfil.
- Jerarquía de administradores (`AdminTier`; el API la valida y la pantalla oculta lo que no se puede hacer):

  | Nivel | Quién es | Qué puede hacer con otras cuentas |
  |---|---|---|
  | Administrador principal | El que creó el sistema (`admin` del seeder; hay uno solo, columna `IsSuperAdmin`). | Todo, incluido autorizar a otros administradores. Nadie más puede modificarlo, eliminarlo, desbloquearlo ni cambiarle la contraseña; él mismo solo edita su nombre y correo, y cambia su contraseña con "Cambiar contraseña". |
  | Administrador autorizado | Un administrador al que el principal autorizó (`CanManageAdministrators`). | Crear administradores y gestionar las cuentas de los administradores normales. A otro autorizado solo lo gestiona el principal. |
  | Administrador | Perfil Administrador sin autorización. | Gestionar los usuarios que no son administradores. No ve el perfil Administrador al crear o editar. |

- Como el principal no se puede desactivar ni eliminar, el sistema nunca se queda sin un administrador activo.
- Iniciar sesión no cuenta como edición: el último acceso y los intentos fallidos no cambian "modificado por" (propiedades marcadas con `[NotAudited]`).
- El nombre de usuario y el correo no se pueden repetir, sin importar mayúsculas o espacios. El correo es opcional, y varias cuentas pueden quedar sin correo.

### Tema claro / oscuro

El botón de sol/luna del encabezado (también presente en el login) alterna el tema.

- **Elección:** se recuerda en el navegador. Si nunca se eligió, se sigue la preferencia del sistema operativo.
- **Implementación:** la escala neutra (`slate`) se redefine en `html.dark`, en `index.css`. Así los componentes se adaptan solos, y solo los tonos de marca y de estado llevan variantes `dark:`.
- **Arranque:** `public/theme-init.js` aplica el tema antes de dibujar la página. Es un archivo propio (no un script en línea) para cumplir la CSP.

### Tablas maestras (CRUD genérico)

Cada catálogo tiene su propia tabla, que hereda de `CatalogEntity` (`Code`, `Name`, `Description`, `SortOrder`, `StatusId`). Un único `CatalogsController` atiende todos los catálogos en `/api/catalogs/{key}`. En el frontend, un único `<CrudManager>` sirve para todos.

Para **agregar un catálogo nuevo**:

1. **Entidad:** créela en `Domain/Entities/Catalogs/Catalogs.cs`, por ejemplo `public class Etnia : CatalogEntity;`. Si depende de otro catálogo, use `HierarchicalCatalogEntity<TPadre>`.
2. **Configuración EF y `DbSet`:** agregue una configuración de una línea en `CatalogConfigurations.cs` y el `DbSet` en `AppDbContext`.
3. **Registro:** agréguelo en `Application/Catalogs/CatalogRegistry.cs`.
4. **Migración:** genérela y aplíquela:

   ```bash
   dotnet ef migrations add AgregarEtnia -p src/TrabajoSocial.Infrastructure -s src/TrabajoSocial.Api -o Persistence/Migrations
   ```

No hace falta código de frontend: la pantalla aparece sola en *Configuración → Tablas maestras*.

## Pruebas

```bash
cd backend && dotnet test     # 41 pruebas: auditoría, soft delete, unicidad, jerarquías, login, bloqueo, inactividad, revocación, datos precargados, usuarios
cd frontend && npm run build && npm run lint
```
