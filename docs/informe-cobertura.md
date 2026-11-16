# Informe de pruebas del backend

| Ejecución | Casos aprobados | Líneas cubiertas | Cobertura de líneas | Cobertura de ramas |
|---|---:|---:|---:|---:|
| Reporte inicial | 89 | 630 de 7.123 | 8,8 % | 7,6 % |
| Solo pruebas unitarias | 322 | 3.738 de 7.123 | 52,4 % | 58,4 % |
| Suite completa | 357 | 5.039 de 7.123 | 70,7 % | 66,8 % |

Las ejecuciones nuevas terminaron sin fallos ni omisiones. El reporte usa el mismo alcance que la captura inicial. Mide el ensamblado `ChefMetrics-BackEnd` en Debug, incluido el modelo de EF. El frontend, el ensamblado `ChefMetrics.Contracts`, `Program` y las migraciones están fuera de esa métrica.

## Tipos de pruebas

| Tipo | Comportamiento que comprueba |
|---|---|
| Unitarias de servicios con Moq | Inicio y renovación de sesión, vencimiento y revocación de permisos, validaciones de insumos, recetas y proveedores, y efectos como guardar historiales o impedir escrituras inválidas. |
| Unitarias de reglas y cálculos | Conversión de unidades, merma, costos y márgenes, precios sugeridos, punto de equilibrio, redondeos y valores límite. |
| Unitarias de controladores | Invocan acciones directamente y verifican respuestas como 201, 400, 401, 404 y 409, y el traspaso de la identidad al servicio. |
| Unitarias de validadores y utilidades | Formato y tamaño de fotos de perfil, contratos de entrada, URLs y cabeceras de seguridad. |
| Componentes con EF Core InMemory | Comprueban consultas, relaciones y persistencia en repositorios, registro y onboarding con datos aislados. Hay 35 casos marcados como `ComponenteEnMemoria`. |

Las pruebas de componentes en memoria dejan pendientes las transacciones reales, la traducción SQL y las restricciones del motor. Las pruebas directas de controladores dejan pendiente la verificación del flujo HTTP completo. Los envíos de correo en flujos de cuenta usan dobles de prueba.

## Reproducción

Desde la raíz del backend, con ReportGenerator instalado:

```powershell
./scripts/run-coverage.ps1
```

El script genera el HTML y conserva el TRX y el XML original en `TestResults`. Para ejecutar solo el grupo unitario:

```powershell
dotnet test ChefMetrics-BackEnd.Tests/ChefMetrics-BackEnd.Tests.csproj --filter 'Tipo!=ComponenteEnMemoria'
```
