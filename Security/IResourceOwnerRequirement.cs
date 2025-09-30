using Microsoft.AspNetCore.Authorization;

namespace TodoBackend.Security;

public interface IResourceOwnerRequirement: IAuthorizationRequirement
{
    string RouteParameterName { get; }
}