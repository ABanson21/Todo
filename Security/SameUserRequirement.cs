using Microsoft.AspNetCore.Authorization;

namespace TodoBackend.Security;

public class SameUserRequirement(string routeParamName) : IResourceOwnerRequirement
{
    public string RouteParameterName { get; } = routeParamName;
}