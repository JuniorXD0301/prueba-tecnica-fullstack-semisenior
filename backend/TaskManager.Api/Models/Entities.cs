namespace TaskManager.Api.Models;

public static class WorkStatus
{
    public static readonly string[] Values = ["Pending", "InProgress", "Done"];
    public static bool IsValid(string value) => Values.Contains(value, StringComparer.Ordinal);
    public static bool CanTransition(string current, string next) => IsValid(next) && !(current == "Pending" && next == "Done");
}
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
public class WorkItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public string AdditionalData { get; set; } = "{}";
}
