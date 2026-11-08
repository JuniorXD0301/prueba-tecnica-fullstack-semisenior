# TaskFlow · Prueba técnica Fullstack Semisenior

Sistema de usuarios y tareas con .NET 10 Web API, Angular 21 y SQL Server.

## Organización

- `backend/TaskManager.Api`: Controllers → Service → Repository con Entity Framework Core.
- `backend/TaskManager.Tests`: pruebas HTTP con persistencia sustituida por un repositorio en memoria.
- `frontend`: Angular standalone, formularios reactivos, filtros y manejo de errores.
- `database`: esquema, consultas JSON, datos opcionales y verificación con limpieza de datos de prueba.

## Requisitos

SDK .NET 10, Node.js compatible con Angular 21 (por ejemplo 20.19 o superior de la rama 20), npm y SQL Server 2016 SP1 o superior con compatibilidad 130+. Recomendado SQL Server 2022. Los scripts se ejecutan completos desde SSMS, sqlcmd o un cliente SQL, sin separadores de lote ni transacciones explícitas.

En el equipo de desarrollo se instaló el SDK sin modificar el PATH global, en `%LOCALAPPDATA%\fullstack-dotnet`. Si `dotnet` no se reconoce, agregarlo para la terminal actual:

```powershell
$env:Path = "$env:LOCALAPPDATA\fullstack-dotnet;$env:Path"
```

## Base de datos

Conectar con SQL Server en SSMS y ejecutar:

1. `database/001_create.sql`: obligatorio; crea TaskManager, tablas, claves, índice, restricciones y trigger.
2. `database/003_seed.sql`: opcional; usuario y tarea de ejemplo.
3. `database/002_queries.sql`: ejemplos de consultas; ajustar variables según los datos.
4. `database/004_verify.sql`: comprobaciones con datos temporales que se eliminan explícitamente mediante DELETE, tanto al terminar como al capturar un error.

Alternativa con autenticación Windows:

```powershell
sqlcmd -S localhost -E -C -b -i database/001_create.sql
sqlcmd -S localhost -E -C -b -i database/003_seed.sql
```

El script de creación se puede repetir, pero no constituye un sistema de migraciones. La API no crea el esquema automáticamente.

La creación de tablas se ejecuta en el contexto de TaskManager mediante `sp_executesql`; el trigger utiliza otra llamada para cumplir la exigencia de SQL Server de que su definición sea la primera sentencia de un lote. Cada sentencia confirma sus cambios de forma independiente. Si la ejecución se interrumpe, pueden quedar cambios parciales o datos de verificación; corregir la causa y repetir la creación, o limpiar las filas de prueba identificadas por su usuario. Ejecutar las verificaciones solo en desarrollo.

## Backend

La conexión predeterminada usa Windows contra `localhost,1433`. Ajustar instancia y puerto según la instalación. Para configurar la conexión en la terminal de la API:

```powershell
$env:ConnectionStrings__TaskManager = "Server=localhost;Database=TaskManager;Integrated Security=true;TrustServerCertificate=true"
dotnet restore backend/TaskManager.Api/TaskManager.Api.csproj
dotnet run --project backend/TaskManager.Api --urls http://localhost:5000
```

Para autenticación SQL, reemplazar `Integrated Security=true` por `User Id=USUARIO;Password=CONTRASENA_LOCAL`. No guardar credenciales en Git. HTTP y `TrustServerCertificate=true` son configuraciones locales; en producción usar HTTPS y certificados válidos. CORS permite `http://localhost:4200` y se configura mediante `AllowedOrigins`.

## Frontend

En otra terminal:

```powershell
cd frontend
npm ci
npm start
```

Abrir http://localhost:4200. El proxy de desarrollo envía `/api` a http://localhost:5000. Crear una persona y después una tarea con responsable. Se pueden filtrar tareas por estado y persona, registrar prioridad, etiquetas, fecha estimada y metadatos, y cambiar el estado.

`npm run build` genera `frontend/dist/frontend/browser`. En producción configurar reverse proxy para `/api` y fallback de Angular; el proxy de desarrollo no se incluye en el bundle.

## Contrato HTTP

| Método | Ruta | Resultado |
| --- | --- | --- |
| POST | `/api/users` | Crea usuario, 201 |
| GET | `/api/users` | Lista usuarios, 200 |
| POST | `/api/tasks` | Crea tarea pendiente, 201 |
| GET | `/api/tasks?userId=1&status=Pending` | Filtra y ordena por creación descendente, 200 |
| PUT | `/api/tasks/{id}/status` | Cambia estado, 200 |

Usuario:

```json
{"name":"Ana Torres","email":"ana@example.com"}
```

Tarea:

```json
{
  "title": "Preparar entrega",
  "userId": 1,
  "additionalData": {
    "priority": "High",
    "estimatedCompletion": "2026-11-10",
    "tags": ["entrega", "fullstack"],
    "metadata": {"equipo": "desarrollo"}
  }
}
```

Cambio de estado: `{"status":"InProgress"}`, seguido de `{"status":"Done"}`. Los valores distinguen mayúsculas en la API. `additionalData` es opcional; si se envía debe ser un objeto JSON, no una cadena escapada ni un array.

Errores: 400 para datos inválidos o usuario inexistente, 404 para tarea inexistente, 409 para transición prohibida, cambios concurrentes o correo duplicado, 503 ante errores SQL y 500 ante errores inesperados. Se usa Problem Details sin exponer excepciones internas, con identificador de trazabilidad. ASP.NET Core rechaza JSON malformado automáticamente.

## Decisiones técnicas

- Título obligatorio, no vacío y de máximo 200 caracteres. La tarea debe tener un usuario existente y se crea siempre en Pending.
- Se prohíbe Pending → Done tanto en el servicio como en un trigger SQL. Las otras transiciones, retrocesos y repetir el estado se permiten porque el enunciado no las restringe.
- La actualización compara el estado leído para detectar cambios concurrentes.
- Nombre obligatorio y correo válido y único: la unicidad es una decisión adicional para evitar duplicados.
- DTOs separados de entidades, consultas parametrizadas por EF y lecturas sin tracking.
- SQL genera fechas UTC; la API conserva la zona UTC y Angular muestra la hora del navegador.
- Claves primarias, FK y restricciones SQL. Índice compuesto `(UserId, Status, CreatedAt)` para consultas por responsable y estado.
- `AdditionalData NVARCHAR(MAX)` con `CHECK ISJSON(...) = 1`. Los ejemplos muestran JSON_VALUE, JSON_QUERY, OPENJSON, filtro por prioridad y una vista previa con JSON_MODIFY mediante SELECT que no altera los datos almacenados.
- Metadatos libres: prioridad, etiquetas y fecha estimada no agregan reglas obligatorias al dominio.
- Angular cancela consultas anteriores al cambiar filtros, valida formularios y muestra errores y estados de carga.

## Verificación

```powershell
dotnet test backend/TaskManager.Tests/TaskManager.Tests.csproj
cd frontend
npm ci
npm run build
```

Las pruebas HTTP comprueban creación/listados, filtros, transición prohibida y válida, título en blanco, usuario inválido, correo inválido, JSON y respuestas 400/404/409. Reemplazan solo la persistencia; no sustituyen pruebas con SQL Server real.

Ejecutar `database/004_verify.sql` para verificar defaults, consulta JSON, transición válida, transición prohibida y rechazo de JSON inválido. No usar en producción.

Flujo manual: crear usuario → crear tarea → filtrar Pending → intentar Done (409) → InProgress → Done → filtrar Done. Comprobar también el mensaje de error al detener la API.

Consultar `VALIDATION.md` para el estado real de las verificaciones en este entorno.

## Funcionalidades pendientes y alcance

Los requisitos funcionales y el requisito adicional de JSON están implementados. No se incluyen autenticación/autorización, paginación, eliminación/edición de tareas, reasignación posterior, migraciones automáticas, despliegue productivo o CI/CD. Son ampliaciones fuera del alcance del enunciado. Usar localmente para evaluación; antes de exponer datos reales se necesitan controles de acceso.
