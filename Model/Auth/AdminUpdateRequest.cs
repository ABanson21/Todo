using TodoBackend.Model.Enums;

namespace TodoBackend.Model.Auth;

public record AdminUpdateRequest(
    string? UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    UserRole? Role
);
