# Pruebas: ubicación, ejecución y evidencia para el comité

## 1. Dónde están las pruebas

Las pruebas automatizadas del backend viven en:

```text
C:\PGC\ChefMetrics-BackEnd\ChefMetrics-BackEnd.Tests\
```

Son proyectos xUnit con Moq. Al cierre de este cambio hay **72 pruebas** y **0 fallos**:

| Archivo | Qué cubre |
|---|---|
| `CalculadoraCostosTests.cs` | Cálculo de costos, merma, escalado y redondeos. |
| `ConversorUnidadesTests.cs` | Conversión de unidades y densidad. |
| `CostoProductoServiceTests.cs` | Costo de productos del menú (recetas + empaques). |
| `CostoRecetaServiceTests.cs` | Costo de recetas y comportamiento con unidades. |
| `CostosFijosServiceTests.cs` | Distribución de costos fijos. |
| `EvaluacionMenuServiceTests.cs` | Evaluación de margen contra el umbral. |
| `PreparacionesBaseMesDtoTests.cs` | Contratos de preparaciones base. |
| `SimuladorVentasServiceTests.cs` | Simulación de ventas. |
| `SugerirPrecioServiceTests.cs` | Precio sugerido a partir del costo y margen. |
| `SecurityHeadersMiddlewareTests.cs` | Cabeceras de seguridad en respuestas de API. |
| `SeguridadSqlTests.cs` | Guardrail de arquitectura: el código de aplicación no usa SQL crudo. |
| `ProveedorServiceSeguridadTests.cs` | Un proveedor de otro negocio no se puede asociar. |
| `AuthControllerSeguridadTests.cs` | Recuperación sin enumeración de correos y errores sin detalles internos. |

El frontend no tiene pruebas de UI automatizadas en este repositorio; su verificación
en CI es la compilación en modo `Release`. Las pruebas de integración de SQL Server
se ejecutan con un script local porque GitHub Actions no tiene la instancia
`SATUS22\MSSQL1SVR`.

## 2. Cómo ejecutar y dejar evidencia

Desde `C:\PGC\ChefMetrics-BackEnd`:

```powershell
dotnet test ChefMetrics-BackEnd.sln `
  --logger "trx;LogFileName=test-results.trx" `
  --results-directory TestResults `
  --collect:"XPlat Code Coverage"
```

Resultado esperado al cierre:

```text
Correctas! - Con error: 0, Superado: 72, Omitido: 0, Total: 72
```

Archivos que quedan disponibles:

- `TestResults\test-results.trx`: resultado detallado en formato Visual Studio.
- `TestResults\test-results-release.trx`: el mismo resultado ejecutado en Release.
- `TestResults\<guid>\coverage.cobertura.xml`: cobertura generada por
  `coverlet.collector`.

La cobertura agregada incluye el modelo EF generado y las migraciones, por lo que
su porcentaje global no refleja la cobertura de la lógica de negocio. Para la
sustentación, lo relevante es que el reporte se genera y queda adjunto; las
pruebas se enfocan en costos, menú, proveedores y seguridad.

### Abrir el TRX

- **Visual Studio:** `Prueba` → `Ventana` → `Explorador de pruebas` → importar
  `TestResults\test-results.trx`.
- **Alternativa:** ejecutar de nuevo `dotnet test` y mostrar la salida de consola;
  el TRX es la evidencia persistente para adjuntar al informe.

## 3. Verificación de integridad en SQL Server

Las protecciones de base de datos se prueban con:

```powershell
sqlcmd -S "SATUS22\MSSQL1SVR" -d ChefMetrics -E -C -b -f 65001 `
  -i scripts\verificar-integridad.sql
```

El script abre una transacción por prueba y hace `ROLLBACK`; no deja datos. Comprueba:

1. Trigger que impide mezclar productos y recetas de negocios distintos.
2. Trigger que impide mezclar productos e insumos de negocios distintos.
3. Trigger que impide mezclar recetas e insumos de negocios distintos.
4. Trigger que impide mezclar insumos y proveedores de negocios distintos.
5. Check de nombre de producto (3 a 75 caracteres, sin espacios externos).
6. Check de precio de venta mayor que cero y máximo `999.999,99`.
7. FK de `HistorialPrecioInsumo` contra una asociación inexistente.
8. `ON DELETE SET NULL`: el historial se conserva al desasociar un proveedor.

Si todo pasa, la última línea es:

```text
OK: todas las verificaciones de integridad pasaron.
```

Si alguna protección falla, el script termina con código de salida distinto de cero
y muestra `FALLO`, lo que permite usarlo en una revisión local o en un pipeline con
SQL Server.

## 4. Integración continua (CI)

Sí, ya existía CI en ambos repositorios y se actualizó:

- Backend: `.github/workflows/ci.yml`
- Frontend: `.github/workflows/ci.yml`

Cambios realizados:

- Se agregó la rama `Ajustes-finales` a `push` y `pull_request`.
- El backend ejecuta `dotnet test` con TRX y cobertura.
- El artefacto `backend-test-results` conserva `TestResults` durante 14 días.
- El frontend sigue compilando en `Release`; para ello clona los contratos del
  backend. Si la rama del backend aún no está publicada, usa `develop`.

Para verlo en GitHub:

1. Abrir el repositorio → pestaña **Actions**.
2. Entrar al workflow **CI Backend**.
3. Abrir la ejecución de `Ajustes-finales`.
4. Descargar el artefacto **backend-test-results** y mostrar el TRX/cobertura.

## 5. Cómo presentarlo al comité

Sugerencia de guion breve:

1. **Cobertura funcional:** mostrar la tabla anterior y la salida
   `72 superadas / 0 fallidas`.
2. **Pruebas nuevas de seguridad:** abrir
   `ProveedorServiceSeguridadTests.cs` y explicar que el proveedor se busca por
   `NegocioId`, no solo por `Id`.
3. **Guardrail de SQL:** abrir `SeguridadSqlTests.cs`; explicar que si alguien
   introduce `FromSqlRaw`, `new SqlCommand` o un `SELECT` literal en controllers,
   services o repositories, la prueba falla.
4. **Defensa en base de datos:** ejecutar `verificar-integridad.sql` y mostrar los
   ocho `OK`; recalcar que los inserts inválidos por SQL directo son rechazados.
5. **CI:** mostrar el workflow y el artefacto con TRX + cobertura.

## 6. Cómo agregar una prueba nueva

1. Crear un archivo `*Tests.cs` dentro de
   `ChefMetrics-BackEnd.Tests`.
2. Usar xUnit (`[Fact]` / `[Theory]`) y Moq para dependencias.
3. Ejecutar:

```powershell
dotnet test ChefMetrics-BackEnd.Tests\ChefMetrics-BackEnd.Tests.csproj
```

No se requiere una base de datos para las pruebas unitarias: los repositorios se
simulan con Moq. Las pruebas que sí necesitan SQL Server deben ir al script de
integridad o a un proyecto de integración separado con su propia base de prueba.
