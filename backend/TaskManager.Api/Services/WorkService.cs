using System.Text.Json;
using TaskManager.Api.Contracts;
using TaskManager.Api.Data;
using TaskManager.Api.Models;
namespace TaskManager.Api.Services;

public class ApiError(int status, string message) : Exception(message) { public int Status { get; } = status; }
public class WorkService(IWorkRepository repository)
{
    public async Task<List<UserResponse>> ListUsers(CancellationToken ct) => (await repository.ListUsers(ct)).Select(x => new UserResponse(x.Id, x.Name, x.Email)).ToList();
    public async Task<UserResponse> CreateUser(CreateUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ApiError(400, "El nombre es obligatorio.");
        var user = await repository.AddUser(new User { Name = request.Name.Trim(), Email = request.Email.Trim().ToLowerInvariant() }, ct);
        return new(user.Id, user.Name, user.Email);
    }
    public async Task<List<TaskResponse>> ListTasks(int? userId, string? status, CancellationToken ct)
    {
        if (userId is <= 0) throw new ApiError(400, "El usuario debe ser un identificador positivo.");
        if (status != null && !WorkStatus.IsValid(status)) throw new ApiError(400, "Estado inválido.");
        return (await repository.ListTasks(userId, status, ct)).Select(Map).ToList();
    }
    public async Task<TaskResponse> CreateTask(CreateTaskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) throw new ApiError(400, "El título es obligatorio.");
        if (!await repository.UserExists(request.UserId, ct)) throw new ApiError(400, "El usuario asignado no existe.");
        if (request.AdditionalData.HasValue && request.AdditionalData.Value.ValueKind != JsonValueKind.Object)
            throw new ApiError(400, "La información adicional debe ser un objeto JSON.");
        return Map(await repository.AddTask(new WorkItem
        {
            Title = request.Title.Trim(),
            UserId = request.UserId,
            AdditionalData = request.AdditionalData?.GetRawText() ?? "{}"
        }, ct));
    }
    public async Task<TaskResponse> ChangeStatus(int id, ChangeStatusRequest request, CancellationToken ct)
    {
        var item = await repository.FindTask(id, ct) ?? throw new ApiError(404, "La tarea no existe.");
        if (!WorkStatus.IsValid(request.Status)) throw new ApiError(400, "Estado inválido.");
        if (!WorkStatus.CanTransition(item.Status, request.Status)) throw new ApiError(409, "Una tarea debe pasar por InProgress antes de Done.");
        if (!await repository.UpdateStatus(id, item.Status, request.Status, ct)) throw new ApiError(409, "La tarea cambió. Actualiza el listado e intenta nuevamente.");
        item.Status = request.Status; return Map(item);
    }
    private static TaskResponse Map(WorkItem x) => new(x.Id, x.Title, x.UserId, x.User.Name, x.Status, DateTime.SpecifyKind(x.CreatedAt, DateTimeKind.Utc), JsonSerializer.Deserialize<JsonElement>(x.AdditionalData));
}
