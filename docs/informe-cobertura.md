# Informe de pruebas del backend

| Ejecución | Casos aprobados | Líneas cubiertas | Cobertura de líneas | Cobertura de ramas |
|---|---:|---:|---:|---:|
| Reporte inicial | 89 | 630 de 7.123 | 8,8 % | 7,6 % |
| Solo pruebas unitarias | 322 | 3.738 de 7.123 | 52,4 % | 58,4 % |
| Suite completa | 357 | 5.039 de 7.123 | 70,7 % | 66,8 % |
| Suite actual antes de ampliar las pruebas | 358 | 5.195 de 7.556 | 68,75 % | 62,0 % |
| Suite actual ampliada | 387 | 6.166 de 7.556 | 81,60 % | 70,1 % |

Las ejecuciones nuevas terminaron sin fallos ni omisiones. El reporte usa el mismo alcance que la captura inicial. Mide el ensamblado `ChefMetrics-BackEnd` en Debug, incluido el modelo de EF. El frontend, el ensamblado `ChefMetrics.Contracts`, `Program` y las migraciones están fuera de esa métrica.

## Tipos de pruebas

| Tipo | Comportamiento que comprueba |
|---|---|
| Unitarias de servicios con Moq | Inicio y renovación de sesión, vencimiento y revocación de permisos, validaciones de insumos, recetas y proveedores, y efectos como guardar historiales o impedir escrituras inválidas. |
| Unitarias de reglas y cálculos | Conversión de unidades, merma, costos y márgenes, precios sugeridos, punto de equilibrio, redondeos y valores límite. |
| Unitarias de controladores | Invocan acciones directamente y verifican respuestas como 201, 400, 401, 404 y 409, y el traspaso de la identidad al servicio. |
| Unitarias de validadores y utilidades | Formato y tamaño de fotos de perfil, contratos de entrada, URLs y cabeceras de seguridad. |
| Componentes con EF Core InMemory y SQLite en memoria | Comprueban consultas, relaciones y persistencia con datos aislados. SQLite cubre transacciones y operaciones en lote en invitaciones de empleados, proveedores e historial de insumos. Ambos grupos usan la marca `ComponenteEnMemoria`. |

Las pruebas con SQLite verifican el comportamiento relacional de esos flujos. Las migraciones, la traducción específica de SQL Server y los triggers requieren pruebas en SQL Server. Las pruebas directas de controladores dejan pendiente la verificación del flujo HTTP completo. Los envíos de correo en flujos de cuenta usan dobles de prueba.

## Reproducción

Desde la raíz del backend, con ReportGenerator instalado:

```powershell
./scripts/run-coverage.ps1
```

El script exige un mínimo de 78 % de cobertura de líneas para la suite completa, genera el HTML y conserva el TRX y el XML original en `TestResults`. Para ejecutar solo el grupo unitario:

```powershell
dotnet test ChefMetrics-BackEnd.Tests/ChefMetrics-BackEnd.Tests.csproj --filter 'Tipo!=ComponenteEnMemoria'
```
