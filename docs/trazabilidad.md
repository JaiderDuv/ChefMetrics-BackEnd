# Trazabilidad propuesta → funcionalidad → archivos → pruebas

Rutas relativas a `C:\PGC`. La propuesta PGC se implementa entre el backend (`ChefMetrics-BackEnd`) y el frontend Blazor (`PWGAPG-Frontend`).

| Propuesta / necesidad | Funcionalidad implementada | Archivos principales | Pruebas / verificación |
|---|---|---|---|
| Medir y costear con las unidades que realmente usa una panadería o pastelería | Catálogo compartido de unidades: `mg`, `g`, `kg`, `oz`, `lb`, `ml`, `l`, `cdta`, `cda`, `taza`, `floz`, `pt`, `qt`, `gal`, `unidad`, `docena`. Conversión dentro de la misma familia y entre masa/volumen cuando hay densidad. | `ChefMetrics-BackEnd/ChefMetrics.Contracts/Costeo/ConversorUnidades.cs`; `ChefMetrics-BackEnd/ChefMetrics.Contracts/Costeo/CalculadoraCostos.cs`; `PWGAPG-Frontend/Components/RecetaForm.razor`; `PWGAPG-Frontend/Components/ProductoMenuForm.razor`; `PWGAPG-Frontend/Pages/Insumos.razor`; `PWGAPG-Frontend/Pages/InsumosProveedores.razor`; `PWGAPG-Frontend/Pages/ProveedoresInsumo.razor` | `ChefMetrics-BackEnd.Tests/ConversorUnidadesTests.cs`; `ChefMetrics-BackEnd.Tests/CostoRecetaServiceTests.cs`; `ChefMetrics-BackEnd.Tests/CostoProductoServiceTests.cs` |
| Convertir una taza de harina a gramos sin cambiar la unidad natural del insumo | Densidad opcional por insumo (`DensidadGPorMl`, g/ml). Se valida en alta/edición, se guarda en base de datos y se usa en recetas, productos, proveedores y costos. | `ChefMetrics-BackEnd/Model/Insumo.cs`; `ChefMetrics-BackEnd/Model/ChefMetricsContext.cs`; `ChefMetrics-BackEnd/ChefMetrics.Contracts/Dtos/InsumoDto.cs`; `ChefMetrics-BackEnd/Services/InsumoService.cs`; `ChefMetrics-BackEnd/Services/RecetaService.cs`; `ChefMetrics-BackEnd/Services/CostoRecetaService.cs`; `ChefMetrics-BackEnd/Services/CostoProductoService.cs`; `ChefMetrics-BackEnd/Services/ProveedorService.cs`; `ChefMetrics-BackEnd/Repositories/ProveedorRepository.cs`; `ChefMetrics-BackEnd/Migrations/20261027194740_AgregarDensidadInsumo.cs`; `PWGAPG-Frontend/Pages/Insumos.razor`; `PWGAPG-Frontend/Components/RecetaForm.razor` | `ChefMetrics-BackEnd.Tests/ConversorUnidadesTests.cs`; `ChefMetrics-BackEnd.Tests/CostoRecetaServiceTests.cs` (`Costea_una_receta_en_tazas_usando_la_densidad_del_insumo`); `ChefMetrics-BackEnd.Tests/CostoProductoServiceTests.cs` (`Costea_empaque_en_tazas_si_el_insumo_tiene_densidad`) |
| Costos confiables: merma, escalado y costo por porción | La merma se aplica sobre la cantidad convertida; el escalado conserva el costo por porción; el costo de producto suma recetas, empaques y fijos prorrateados. | `ChefMetrics-BackEnd/ChefMetrics.Contracts/Costeo/CalculadoraCostos.cs`; `ChefMetrics-BackEnd/Services/CostoRecetaService.cs`; `ChefMetrics-BackEnd/Services/CostoProductoService.cs`; `PWGAPG-Frontend/Components/RecetaForm.razor`; `PWGAPG-Frontend/Components/ProductoMenuForm.razor` | `ChefMetrics-BackEnd.Tests/CalculadoraCostosTests.cs`; `ChefMetrics-BackEnd.Tests/CostoRecetaServiceTests.cs` (`La_merma_aumenta_la_cantidad_que_se_debe_comprar`, `El_escalado_mantiene_el_costo_por_porcion`); `ChefMetrics-BackEnd.Tests/CostoProductoServiceTests.cs` |
| Evaluar el menú y sugerir precios con un margen objetivo | Evaluación por margen directo contra umbral configurable y sugerencia de precio `costo / (1 - margen)`. | `ChefMetrics-BackEnd/Services/EvaluacionMenuService.cs`; `ChefMetrics-BackEnd/Services/SugerirPrecioService.cs`; `ChefMetrics-BackEnd/Services/SugerirPrecioProductoService.cs`; `PWGAPG-Frontend/Components/ProductoMenuForm.razor` | `ChefMetrics-BackEnd.Tests/EvaluacionMenuServiceTests.cs`; `ChefMetrics-BackEnd.Tests/SugerirPrecioServiceTests.cs` |
| Cerrar el onboarding incompleto | El onboarding queda en dos pasos: Información Personal e Información del Negocio. Se eliminaron Empleados, Productos e Insumos. Los datos se guardan mediante `POST api/onboarding/datos-personales` y `POST api/onboarding/datos-negocio`, y luego se marca como completado. | `PWGAPG-Frontend/Pages/Onboarding.razor`; `PWGAPG-Frontend/Pages/OnboardingTabs/InformacionPersonalTab.razor`; `PWGAPG-Frontend/Pages/OnboardingTabs/InformacionNegocioTab.razor`; `PWGAPG-Frontend/Models/DuenioDTO.cs`; `PWGAPG-Frontend/Models/OnboardingNegocioDto.cs`; `ChefMetrics-BackEnd/Controllers/OnboardingController.cs` | Compilación del frontend y prueba manual del flujo de dos pasos. No hay prueba automatizada de UI en el repositorio. |
| Categorías de negocio para el público objetivo | Se agregaron `Panadería` y `Pastelería` y se unificó el catálogo entre registro y edición de perfil: Panadería, Pastelería, Repostería, Cafetería, Gastronomía, Productos frescos y Otros. | `PWGAPG-Frontend/Pages/Registro.razor`; `PWGAPG-Frontend/Pages/EditarPerfilEmprendimiento.razor` | Verificación manual en ambos formularios; el backend ya persiste el nombre de la categoría en `TipoEmprendimiento`. |
| Evitar regresiones en los cálculos críticos | Se agregaron pruebas unitarias de conversión, densidad, merma, escalado, costo de producto, evaluación de menú y precio sugerido. | `ChefMetrics-BackEnd.Tests/*.cs` | `dotnet test ChefMetrics-BackEnd.Tests\ChefMetrics-BackEnd.Tests.csproj` → 65 pruebas correctas. |
| Documentar y facilitar el mantenimiento | README y esta matriz de trazabilidad. | `ChefMetrics-BackEnd/README.md`; `ChefMetrics-BackEnd/docs/trazabilidad.md`; `PWGAPG-Frontend/README.md`; `PWGAPG-Frontend/docs/trazabilidad.md` | Revisión manual. |

## Comandos de verificación usados

```powershell
# Backend
cd C:\PGC\ChefMetrics-BackEnd
dotnet build ChefMetrics-BackEnd.csproj
dotnet test ChefMetrics-BackEnd.Tests\ChefMetrics-BackEnd.Tests.csproj

# Frontend
cd C:\PGC\PWGAPG-Frontend
dotnet build PWGAPG-Frontend.csproj
```

## Archivo que no debe entrar en commits

`ChefMetrics-BackEnd/wwwroot/uploads/recetas/e1e50b5566ef42869bb3dc3431314c44.png` es un archivo no rastreado de prueba. No agregarlo a los commits.
