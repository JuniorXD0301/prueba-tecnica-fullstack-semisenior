using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Services;
namespace TaskManager.Api.Middleware;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var sql = exception as SqlException ?? (exception as DbUpdateException)?.InnerException as SqlException;
        var (status, detail) = exception switch
        {
            ApiError error => (error.Status, error.Message),
            _ when sql?.Number is 2601 or 2627 => (409, "Ya existe un usuario con ese correo."),
            _ when sql?.Number == 51000 => (409, "Una tarea debe pasar por InProgress antes de Done."),
            _ when sql?.Number == 547 => (409, "La operación incumple una regla de integridad de los datos."),
            _ when sql != null => (503, "No fue posible acceder a la base de datos. Intenta más tarde."),
            _ => (500, "Ocurrió un error inesperado.")
        };
        if (status >= 500) logger.LogError(exception, "Error procesando {Path}", context.Request.Path);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = detail,
            Detail = detail,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, ct);
        return true;
    }
}
