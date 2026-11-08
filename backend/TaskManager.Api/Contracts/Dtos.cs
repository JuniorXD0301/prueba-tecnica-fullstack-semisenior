using System.ComponentModel.DataAnnotations;
using System.Text.Json;
namespace TaskManager.Api.Contracts;

public record CreateUserRequest([Required, StringLength(120)] string Name, [Required, EmailAddress, StringLength(254)] string Email);
public record UserResponse(int Id, string Name, string Email);
public record CreateTaskRequest([Required, StringLength(200)] string Title, [Range(1, int.MaxValue)] int UserId, JsonElement? AdditionalData);
public record ChangeStatusRequest([Required] string Status);
public record TaskResponse(int Id, string Title, int UserId, string UserName, string Status, DateTime CreatedAt, JsonElement AdditionalData);
