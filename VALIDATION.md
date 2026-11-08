# Estado de validación

Verificaciones realizadas el 8 de noviembre de 2026:

- .NET SDK 10.0.403: API y proyecto de pruebas compilan.
- `dotnet test backend/TaskManager.Tests/TaskManager.Tests.csproj`: 7 pruebas aprobadas, 0 fallidas.
- Restauración final de NuGet sin advertencias de vulnerabilidad. Se actualizó EF Core y se fijó System.Security.Cryptography.Pkcs para corregir una dependencia indirecta vulnerable.
- `npm run build`: compilación Angular de producción correcta.
- Interfaz revisada en navegador: estructura, filtros, formularios, textos en español y error visible cuando la API no está disponible.

## Límite de la verificación

No hay servicio SQL Server activo. Docker Desktop está instalado, pero el motor Linux no respondió tras intentar iniciarlo. No se ejecutaron los scripts T-SQL ni se verificó el flujo completo contra una base de datos real. No se utilizó MySQL como sustituto.

Las pruebas HTTP usan el pipeline real de ASP.NET Core con un repositorio en memoria. Comprueban reglas y contrato HTTP, pero no validan traducción SQL de EF, índice, FK o trigger. Para cerrar esa comprobación, iniciar SQL Server y seguir el README, incluyendo `database/004_verify.sql` y el flujo manual completo.
