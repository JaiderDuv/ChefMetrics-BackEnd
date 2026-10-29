# ChefMetrics Backend

API y contratos compartidos de ChefMetrics. El frontend Blazor está en `../PWGAPG-Frontend`.

## Requisitos

- .NET SDK 9
- SQL Server o LocalDB (la base `ChefMetrics` se crea automáticamente)
- `dotnet-ef` solo si vas a crear migraciones nuevas

## Comandos útiles

```powershell
# Compilar
dotnet build ChefMetrics-BackEnd.csproj

# Pruebas
dotnet test ChefMetrics-BackEnd.Tests\ChefMetrics-BackEnd.Tests.csproj

# Aplicar migraciones (revisar la cadena de conexión antes)
dotnet ef database update

# Crear una migración
dotnet ef migrations add NombreDeLaMigracion
```

Al iniciar, el backend crea la base si no existe y aplica las migraciones pendientes.
Si no hay cadena de conexión configurada (por ejemplo en un clon nuevo), usa LocalDB
para que cualquier integrante pueda ejecutar el proyecto sin pasos manuales.

Para usar otra base de datos o una clave JWT distinta, define las variables de entorno
`ConnectionStrings__DefaultConnection` y `Jwt__Key` antes de ejecutar el proyecto.

En desarrollo, si no hay SMTP configurado, las cuentas nuevas se verifican
automáticamente para poder iniciar sesión sin enviar correos.

## Documentación

- [Matriz de trazabilidad](docs/trazabilidad.md)
