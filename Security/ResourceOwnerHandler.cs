using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace TodoBackend.Security;

public class ResourceOwnerHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<IResourceOwnerRequirement>
{
    protected override async  Task HandleRequirementAsync(AuthorizationHandlerContext context,
        IResourceOwnerRequirement requirement)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdClaim = context.User.FindFirst("UserId")?.Value;
     
        if (userIdClaim == null)
            return;
        
        // enable the buffer before you interact with the httpContext request. else there will be issues
        httpContextAccessor!.HttpContext.Request.EnableBuffering();

        var requestBody = httpContextAccessor.HttpContext!.Request.Body;
        using var reader = new StreamReader(requestBody, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        requestBody.Position = 0;
        
        if(string.IsNullOrEmpty(body))
            return;

        var jsonParse =  JsonDocument.Parse(body).RootElement;
        var bodyElement = jsonParse.TryGetProperty(requirement.RouteParameterName, out var userId);
        
        if (userId.ToString() == userIdClaim)
        {
            context.Succeed(requirement);
        }
    }
}