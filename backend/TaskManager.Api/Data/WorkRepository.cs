using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;
namespace TaskManager.Api.Data;

public interface IWorkRepository
{
    Task<List<User>> ListUsers(CancellationToken ct);
    Task<bool> UserExists(int id, CancellationToken ct);
    Task<User> AddUser(User user, CancellationToken ct);
    Task<List<WorkItem>> ListTasks(int? userId, string? status, CancellationToken ct);
    Task<WorkItem?> FindTask(int id, CancellationToken ct);
    Task<WorkItem> AddTask(WorkItem task, CancellationToken ct);
    Task<bool> UpdateStatus(int id, string expected, string next, CancellationToken ct);
}
public class WorkRepository(TaskDbContext db) : IWorkRepository
{
    public Task<List<User>> ListUsers(CancellationToken ct) => db.Users.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
    public Task<bool> UserExists(int id, CancellationToken ct) => db.Users.AnyAsync(x => x.Id == id, ct);
    public async Task<User> AddUser(User user, CancellationToken ct) { db.Users.Add(user); await db.SaveChangesAsync(ct); return user; }
    public Task<List<WorkItem>> ListTasks(int? userId, string? status, CancellationToken ct)
    {
        var query = db.Tasks.AsNoTracking().Include(x => x.User).AsQueryable();
        if (userId.HasValue) query = query.Where(x => x.UserId == userId);
        if (status != null) query = query.Where(x => x.Status == status);
        return query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToListAsync(ct);
    }
    public Task<WorkItem?> FindTask(int id, CancellationToken ct) => db.Tasks.AsNoTracking().Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<WorkItem> AddTask(WorkItem task, CancellationToken ct)
    {
        db.Tasks.Add(task); await db.SaveChangesAsync(ct);
        await db.Entry(task).Reference(x => x.User).LoadAsync(ct); return task;
    }
    public async Task<bool> UpdateStatus(int id, string expected, string next, CancellationToken ct) =>
        await db.Tasks.Where(x => x.Id == id && x.Status == expected).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, next), ct) == 1;
}
