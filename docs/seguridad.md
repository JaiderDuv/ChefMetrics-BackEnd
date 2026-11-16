# Seguridad, integridad y rendimiento

Este documento resume las defensas agregadas y lo que queda fuera de alcance.
La idea es **defensa en profundidad**: no depender solo de la validación del
frontend ni de una única capa.

## 1. Capa API

| Control | Implementación | Efecto |
|---|---|---|
| Algoritmo JWT fijo | `Program.cs` usa `ValidAlgorithms = [HmacSha256]` | Un token no puede elegir otro algoritmo de firma. |
| Rate limiting global | 300 solicitudes/minuto por IP | Frena abuso general y scraping. |
| Rate limiting de autenticación | 10 solicitudes/minuto por IP en `AuthController` | Dificulta fuerza bruta, enumeración de correos y abuso de refresh. |
| Cabeceras de seguridad | `Middleware/SecurityHeadersMiddleware.cs` | `nosniff`, `DENY` para frames, `no-referrer`, Permissions Policy y CSP estricta en `/api`. |
| Errores sin detalles internos | `Middleware/GlobalExceptionHandler.cs` + `UseExceptionHandler` | El cliente recibe un mensaje genérico; el stack y SQL Server quedan en el log. |
| Recuperación sin enumeración | `AuthController.RecuperarPassword` responde igual exista o no el correo | Evita que un atacante descubra cuentas registradas. |
| HSTS | `UseHsts` en producción | Fuerza HTTPS durante un año e incluye subdominios. |
| Límite de request | Kestrel y `FormOptions`: 5 MB | Evita cargas enormes; las imágenes de menú se validan además por tipo/tamaño. |
| CORS restringido | Orígenes de `Cors:AllowedOrigins` | Solo el frontend configurado puede llamar a la API desde el navegador. |
| Health checks | `GET /health` y `/health/ready` | Permiten monitorear la API y SQL Server sin exponer detalles. |
| Compresión | Brotli/Gzip para JSON | Reduce el volumen de respuestas del menú. |

Notas operativas:

- En producción, configure `Jwt:Key` fuera del repositorio y mantenga HTTPS.
- Detrás de un proxy, configure `KnownProxies`/`KnownNetworks` de
  `ForwardedHeadersOptions`; de lo contrario el rate limit verá la IP del proxy.
- `appsettings.json` está ignorado por Git. No agregar secretos al repositorio.

## 2. Capa de datos: evitar inserts inválidos por SQL directo

La API usa EF Core en todos los controllers, services y repositories. Aun así,
una persona con acceso a SQL Server podría intentar un `INSERT` directo. Para
ese escenario se agregó la migración:

```text
Migrations\20261029031241_EndurecerIntegridadTenant.cs
```

### 2.1 Restricciones `CHECK`

- `CK_ProductoMenu_Nombre`: nombre sin espacios externos y entre 3 y 75 caracteres.
- `CK_ProductoMenu_Precio`: precio nulo o mayor que 0 y máximo `999.999,99`
  (antes se permitía 0).
- `CK_ProductoMenuInsumo_Cantidad`: cantidad mayor que 0 y máximo 100.000.
- Se conservan los checks existentes de cantidades de recetas y asociaciones.

### 2.2 Llave foránea e índices

- Se creó `FK_HistorialPrecioInsumo_InsumoProveedor` con `ON DELETE SET NULL`.
  Así el historial de precios conserva sus snapshots cuando se desasocia un
  proveedor, en lugar de bloquear el borrado o perder la trazabilidad.
- Se creó `IX_HistorialPrecioInsumo_InsumoProveedorId`, que el modelo ya declaraba
  pero no existía en la base.
- Se agregaron índices por negocio/fecha y por insumo original para las consultas
  de historial, y un índice de listado de menú por `(NegocioId, Activo, Nombre)`.

### 2.3 Validación de tenencia

La migración `20261116000000_AgregarTriggersYProcedimientos` instala cinco
triggers de inserción y actualización. Cuatro validan las asociaciones de
insumo con proveedor, receta con insumo, y producto de menú con insumo o receta.
El quinto comprueba que el negocio indicado en un historial de precios coincida
con el del insumo asociado. EF Core desactiva la cláusula `OUTPUT` directa en
las cinco tablas para permitir que los triggers convivan con los guardados de la API.

Los servicios también validan la tenencia al recibir datos del cliente.

| Servicio | Regla |
|---|---|
| `ProductoMenuService` | El producto y cada receta o insumo directo deben pertenecer al mismo `NegocioId`. |
| `RecetaService` | La receta y cada insumo deben pertenecer al mismo `NegocioId`. |
| `InsumoService` / `ProveedorService` | El insumo y el proveedor deben pertenecer al mismo `NegocioId`. |

### 2.4 Lo que no cubre la base

Un producto se crea primero y sus recetas se agregan después; por eso la base no
puede exigir “al menos una receta” en el `INSERT` de `ProductoMenu`. Esa regla se
valida en `ProductoMenuService` y en el DTO (`[MinLength(1)]`).

### 2.5 Prueba local

```powershell
sqlcmd -S "SATUS22\MSSQL1SVR" -d ChefMetrics -E -C -b -f 65001 `
  -i scripts\verificar-integridad.sql
```

El script comprueba que estén presentes los cinco triggers activos y los cinco
procedimientos almacenados. Prueba los checks, la FK y `ON DELETE SET NULL` en
transacciones con `ROLLBACK`. La salida esperada termina con el siguiente mensaje.

```text
OK: todas las verificaciones de integridad pasaron.
```

## 3. Inyección SQL

- El código de aplicación no usa `FromSqlRaw`, `ExecuteSqlRaw`, `SqlCommand` ni
  SQL literal en controllers, services, repositories o utilidades.
- EF Core parametriza las consultas.
- `SeguridadSqlTests.cs` es un guardrail: falla si alguien introduce SQL crudo en
  esas carpetas.
- Las migraciones sí usan `migrationBuilder.Sql`, pero son scripts controlados,
  versionados y no reciben entrada del usuario.

## 4. Corrección de tenencia en proveedores

`ProveedorService.AsociarAsync` validaba que el insumo perteneciera al negocio,
pero buscaba el proveedor solo por `Id`. Un cliente podía enviar el `Id` de un
proveedor de otra cuenta. Ahora se usa
`ObtenerPorIdDelNegocioAsync(negocioId, proveedorId)`, y
`ProveedorServiceSeguridadTests` cubre el caso.

## 5. Rendimiento

- `AsSplitQuery()` en las consultas del menú que incluyen dos colecciones; evita
  el producto cartesiano entre recetas e insumos.
- Búsqueda de menú con colación `CI_AI` en lugar de `LOWER()`, y comparación
  directa en `ExisteNombreAsync` para aprovechar el índice único.
- Índices de historial y de listado de menú.
- Compresión Brotli/Gzip para JSON.
- Caché de una semana para las imágenes de `/uploads`, cuyos nombres incluyen GUID
  y son inmutables.

Pendiente para una siguiente iteración: el endpoint del menú todavía devuelve
todos los productos y el frontend ordena por margen en el cliente. Para menús
muy grandes conviene precalcular costos o paginar en servidor; se mantiene el
comportamiento actual para no romper el orden por margen.
