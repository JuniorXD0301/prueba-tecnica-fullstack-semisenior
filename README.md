# TaskFlow · Prueba técnica Fullstack Semisenior

Sistema de usuarios y tareas con .NET 10 Web API, Angular 21 y SQL Server.

## 1. Preparar el equipo

Esta guía usa Windows, PowerShell y DBeaver. Instala estos componentes si todavía no los tienes.

- **SDK de .NET 10** para compilar el backend. [Descargar .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).
- **Node.js con npm** para ejecutar Angular. Angular 21 admite Node.js 20.19+ dentro de la rama 20, 22.12+ dentro de la rama 22 o la rama 24. [Compatibilidad de Angular](https://angular.dev/reference/versions) · [Descargar Node.js](https://nodejs.org/en/download).
- **Motor de SQL Server** 2016 o superior con nivel de compatibilidad 130 o mayor. Puedes usar la edición Express o Developer para desarrollo local. [Descargar SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads).
- **DBeaver** para conectarte al motor y ejecutar los archivos SQL. [Descargar DBeaver](https://dbeaver.io/download/).

Abre en VS Code la carpeta que contiene este README, `backend`, `database` y `frontend`. Si aún no tienes el código, clona el repositorio con Git y abre la carpeta resultante. Git descargará `main`, la rama principal.

```powershell
git clone https://github.com/JuniorXD0301/prueba-tecnica-fullstack-semisenior.git
cd prueba-tecnica-fullstack-semisenior
```

En VS Code, selecciona **Terminal → Nueva terminal**. Desde la raíz del proyecto, comprueba que PowerShell reconoce las herramientas.

```powershell
dotnet --version
node --version
npm --version
```

Si `dotnet --version` no se reconoce y el SDK está instalado en la ruta estándar de Windows, ejecuta esto en esa terminal y comprueba que devuelve una versión `10.0.x`.

```powershell
$env:Path = "C:\Program Files\dotnet;$env:Path"
dotnet --version
```

## 2. Crear la base de datos

1. Comprueba que el servicio de SQL Server esté iniciado. Si lo instalaste en el mismo PC donde ejecutarás el proyecto, el servidor será `localhost`. El puerto predeterminado es `1433`. Usa el servidor y el puerto de tu instalación si son distintos.
2. En DBeaver, elige **Nueva conexión → SQL Server**. Usa el servidor y puerto del paso anterior, `master` como base inicial y el método de autenticación configurado en tu instancia. Pulsa **Probar conexión**. La cuenta debe tener permiso para crear la base y sus tablas.
3. Abre un editor SQL en esa conexión y activa **Auto-commit** en la barra del editor. Copia el contenido de [000_create_database.sql](database/000_create_database.sql) al editor y ejecuta con **Alt+X**. Esto crea `TaskManager`. Si ya existe, pasa al siguiente paso.
4. Actualiza el navegador de DBeaver. Cambia **Database/Schema** de la conexión a `TaskManager`, vuelve a conectar y abre un editor SQL nuevo. Copia [001_create.sql](database/001_create.sql) y ejecuta con **Alt+X**. En la sección **Tables** deben aparecer `dbo.Users` y `dbo.Tasks`.
5. Abre otro editor SQL con `TaskManager` como base activa. Copia [001_create_trigger.sql](database/001_create_trigger.sql), selecciona todo el texto con **Ctrl+A** y ejecuta la selección con **Ctrl+Enter**. En **Triggers** de `dbo.Tasks` debe aparecer `TR_Tasks_StatusTransition`. [Atajos de ejecución en DBeaver](https://dbeaver.com/docs/dbeaver/SQL-Execution/).

Ejecuta cada archivo de creación una sola vez. Si ya creaste las tablas o el trigger, omite el paso correspondiente. El archivo del trigger se ejecuta como un bloque porque `CREATE TRIGGER` debe iniciar su lote.

## 3. Iniciar el backend

La configuración incluida funciona cuando SQL Server está en el mismo PC que la API, escucha en el puerto `1433` y acepta tu cuenta de Windows. Usa el mismo servidor, puerto y método de autenticación que probaste en DBeaver.

Si esos datos son distintos, configura la conexión en PowerShell antes de iniciar la API. Sustituye `SERVIDOR` y `PUERTO` por los de tu instalación. La base sigue siendo `TaskManager`, creada en el paso anterior.

```powershell
$env:ConnectionStrings__TaskManager = "Server=SERVIDOR,PUERTO;Database=TaskManager;Integrated Security=true;TrustServerCertificate=true"
```

Si en DBeaver te conectaste con un usuario de SQL Server, reemplaza `Integrated Security=true` por `User Id=TU_USUARIO;Password=TU_CONTRASEÑA` en la variable de esa sesión.

Desde la raíz del proyecto, inicia la API en esa misma terminal.

```powershell
dotnet run --project backend/TaskManager.Api --urls http://localhost:5000
```

Espera a que aparezca `Now listening on: http://localhost:5000`. Abre [http://localhost:5000/api/users](http://localhost:5000/api/users) en el navegador. Debe devolver una lista JSON. En una base recién creada verás `[]`. Deja la terminal abierta.

## 4. Iniciar el frontend

Abre una segunda terminal desde la raíz del proyecto. Si PowerShell te bloquea `npm.ps1`, usa `npm.cmd` en lugar de `npm`.

```powershell
cd frontend
npm ci
npm start -- --port 4200
```

`npm ci` instala las versiones de las dependencias fijadas en `package-lock.json`. `npm start` inicia el servidor de Angular.

Abre [http://localhost:4200](http://localhost:4200). Angular reenvía las llamadas `/api` al backend del puerto `5000`. Mantén ambas terminales abiertas mientras usas la app. Para detener un servidor, pulsa **Ctrl+C** en su terminal.

## 5. Probar el flujo

1. En **Personas del equipo**, agrega una persona con un nombre y un correo válido, por ejemplo `ana@example.com`.
2. En **Nueva tarea**, escribe un título, selecciona a esa persona como responsable y elige una prioridad. Pulsa **Crear tarea**. Debe aparecer una tarjeta con el estado **Pendiente**.
3. Crea una segunda tarea. En **Orden por fecha**, cambia entre **Más recientes** y **Más antiguas** y observa cómo se invierten las tarjetas. Prueba también los filtros de estado y responsable.
4. Vuelve a **Todos los estados** para que la tarjeta siga visible al cambiar de estado. En **Cambiar estado**, comprueba que **Terminada** está deshabilitada mientras la tarea está pendiente. Elige **En progreso** y después **Terminada**. La tarjeta debe actualizarse y mostrar un mensaje de éxito.
5. Para revisar el JSON en DBeaver, ejecuta las consultas de [002_queries.sql](database/002_queries.sql) sobre `TaskManager`. Puedes ver el `id` de la persona en [http://localhost:5000/api/users](http://localhost:5000/api/users). Si es distinto de `1`, cambia `UserId = 1` por ese valor. El filtro de prioridad del ejemplo usa `High`, que corresponde a **Alta** en la aplicación.

Si DBeaver muestra `Connection refused` en el puerto `1433`, revisa TCP/IP en **SQL Server Configuration Manager → Protocols for MSSQLSERVER** y reinicia el servicio tras habilitarlo. Si el backend no puede abrir `TaskManager`, comprueba la base activa en DBeaver y los permisos del usuario de Windows. Si la app no carga tareas, abre primero `http://localhost:5000/api/users` para localizar si el problema está en la API o en Angular.

## Organización

- `backend/TaskManager.Api` contiene los controladores HTTP, el servicio de negocio y el repositorio con Entity Framework Core.
- `backend/TaskManager.Tests` contiene las pruebas HTTP con un repositorio en memoria.
- `frontend` contiene la aplicación Angular, los formularios y los filtros.
- `database` contiene los scripts de creación, el trigger y los ejemplos de consulta.

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

## Ejecutar las pruebas y compilar

```powershell
dotnet test backend/TaskManager.Tests/TaskManager.Tests.csproj
cd frontend
npm ci
npm run build
```

Ejecuta estos comandos desde la raíz. Las pruebas HTTP usan persistencia en memoria y cubren creación, listados, filtros, validaciones y transiciones. La compilación de Angular genera los archivos estáticos en `frontend/dist/frontend/browser`.

## Funcionalidades pendientes y alcance

La aplicación cumple los requisitos de la prueba, incluido el manejo de datos adicionales en JSON. Para facilitar su uso, muestra mensajes de éxito, advertencia y error que indican si una acción se completó, si hay datos que corregir o si ocurrió un problema.

Quedan fuera del alcance de la prueba el inicio de sesión, los permisos por usuario y la paginación de las listas. Tampoco se incluye editar o eliminar usuarios y tareas, cambiar el responsable después de crear una tarea, aplicar cambios automáticos a la base de datos ni publicar la aplicación y automatizar su entrega.
