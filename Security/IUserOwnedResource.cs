using Microsoft.AspNetCore.Authorization;

namespace TodoBackend.Security;

public class IUserOwnedResource
{
    public int UserId { get; }
}