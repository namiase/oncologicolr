# TrabajoSocial

## Testing

- Backend (xUnit, `backend/tests/TrabajoSocial.UnitTests`, SQLite en memoria con `Support/TestHost`): `cd backend && dotnet test`
  - Si la API está corriendo en Debug, los DLL quedan bloqueados: usar `dotnet test -c Release`.
- Frontend (sin pruebas automáticas todavía): `cd frontend && npm run build && npm run lint`
- Convenciones de las pruebas del backend: una clase por servicio (`SocialReportServiceTests`, `SocialReportPdfTests`), `TestHost` crea la base y los usuarios de prueba, los errores de validación se comprueban por clave de campo (p. ej. `phones[0].number`).
