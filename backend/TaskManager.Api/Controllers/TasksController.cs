using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts;
using TaskManager.Api.Services;
namespace TaskManager.Api.Controllers;

[ApiController, Route("api/tasks")]
public class TasksController(WorkService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<List<TaskResponse>>> List([FromQuery] int? userId, [FromQuery] string? status, CancellationToken ct) => Ok(await service.ListTasks(userId, status, ct));
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var task = await service.CreateTask(request, ct); return Created($"/api/tasks?userId={task.UserId}", task);
    }
    [HttpPut("{id:int}/status")] public async Task<ActionResult<TaskResponse>> Change(int id, ChangeStatusRequest request, CancellationToken ct) => Ok(await service.ChangeStatus(id, request, ct));
}
