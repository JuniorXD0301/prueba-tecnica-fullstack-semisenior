using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts;
using TaskManager.Api.Services;
namespace TaskManager.Api.Controllers;

[ApiController, Route("api/users")]
public class UsersController(WorkService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<List<UserResponse>>> List(CancellationToken ct) => Ok(await service.ListUsers(ct));
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await service.CreateUser(request, ct); return Created($"/api/users?userId={user.Id}", user);
    }
}
