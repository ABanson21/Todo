using Microsoft.AspNetCore.Authorization;
using TodoBackend.Model.Enums;

namespace TodoBackend.Security;

public class ResourceOwnerHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<ResourceOwnerRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
            return Task.CompletedTask;
        
        if (context.User.IsInRole(nameof(UserRole.Admin)))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;;
        }

        var userIdClaim = context.User.FindFirst(AppConstants.UserId)?.Value;
     
        if (userIdClaim == null)
            return Task.CompletedTask;;
        
        if (!int.TryParse(userIdClaim, out var authenticatedUser))
            return Task.CompletedTask;;
        
        var routeUserId = httpContext.GetRouteValue("userId")?.ToString();
        if (routeUserId == null)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        
        if (int.TryParse(routeUserId, out var targetUserId) && targetUserId == authenticatedUser)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}