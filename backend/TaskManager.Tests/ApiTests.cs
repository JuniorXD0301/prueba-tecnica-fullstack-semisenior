using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManager.Api.Contracts;
using TaskManager.Api.Data;
using TaskManager.Api.Models;
using Xunit;
namespace TaskManager.Tests;

public class ApiTests
{
    [Fact]
    public async Task UserAndTaskWorkflowEnforcesTransitionsAndFilters()
    {
        using var factory = new TestApi(); using var client = factory.CreateClient();
        var userResult = await client.PostAsJsonAsync("/api/users", new { name = "Ana", email = "ana@example.com" });
        Assert.Equal(HttpStatusCode.Created, userResult.StatusCode);
        var user = (await userResult.Content.ReadFromJsonAsync<UserResponse>())!;
        var created = await client.PostAsJsonAsync("/api/tasks", new { title = "Entrega", userId = user.Id, additionalData = new { priority = "High", tags = new[] { "test" } } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var task = (await created.Content.ReadFromJsonAsync<TaskResponse>())!;
        Assert.Equal("Pending", task.Status);
        Assert.Equal("High", task.AdditionalData.GetProperty("priority").GetString());
        var denied = await client.PutAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "Done" });
        Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "InProgress" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "Done" })).StatusCode);
        var filtered = await client.GetFromJsonAsync<List<TaskResponse>>($"/api/tasks?userId={user.Id}&status=Done");
        Assert.Single(filtered!);
        Assert.Empty((await client.GetFromJsonAsync<List<TaskResponse>>("/api/tasks?status=Pending"))!);
        Assert.Single((await client.GetFromJsonAsync<List<UserResponse>>("/api/users"))!);
    }
    [Theory]
    [InlineData("", 1)]
    [InlineData("   ", 1)]
    [InlineData("Task", 0)]
    [InlineData("Task", 99)]
    public async Task InvalidTaskReturnsBadRequest(string title, int userId)
    {
        using var factory = new TestApi(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/tasks", new { title, userId })).StatusCode);
    }
    [Fact]
    public async Task InvalidJsonShapeAndInvalidEmailAreRejected()
    {
        using var factory = new TestApi(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/users", new { name = "Ana", email = "bad" })).StatusCode);
        await client.PostAsJsonAsync("/api/users", new { name = "Ana", email = "ana@example.com" });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/tasks", new { title = "Task", userId = 1, additionalData = new[] { "invalid" } })).StatusCode);
    }
    [Fact]
    public async Task UnknownTaskAndInvalidStatusReturnUsefulErrors()
    {
        using var factory = new TestApi(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/tasks/99/status", new { status = "Done" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/tasks?status=Other")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/tasks?userId=-1")).StatusCode);
    }
}
public class TestApi : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services =>
    {
        services.RemoveAll<IWorkRepository>(); services.AddSingleton<IWorkRepository, FakeRepository>();
    });
}
// Only replaces persistence: HTTP routing, DTO validation, service rules and error middleware are real.
public class FakeRepository : IWorkRepository
{
    private readonly List<User> users = [];
    private readonly List<WorkItem> tasks = [];
    public Task<List<User>> ListUsers(CancellationToken ct) => Task.FromResult(users.ToList());
    public Task<bool> UserExists(int id, CancellationToken ct) => Task.FromResult(users.Any(x => x.Id == id));
    public Task<User> AddUser(User user, CancellationToken ct) { user.Id = users.Count + 1; users.Add(user); return Task.FromResult(user); }
    public Task<List<WorkItem>> ListTasks(int? userId, string? status, CancellationToken ct) => Task.FromResult(tasks.Where(x => (!userId.HasValue || x.UserId == userId) && (status == null || x.Status == status)).OrderByDescending(x => x.CreatedAt).ToList());
    public Task<WorkItem?> FindTask(int id, CancellationToken ct) => Task.FromResult(tasks.Find(x => x.Id == id));
    public Task<WorkItem> AddTask(WorkItem task, CancellationToken ct) { task.Id = tasks.Count + 1; task.User = users.Single(x => x.Id == task.UserId); task.CreatedAt = DateTime.UtcNow; tasks.Add(task); return Task.FromResult(task); }
    public Task<bool> UpdateStatus(int id, string expected, string next, CancellationToken ct)
    {
        var item = tasks.Find(x => x.Id == id && x.Status == expected);
        if (item == null) return Task.FromResult(false); item.Status = next; return Task.FromResult(true);
    }
}
