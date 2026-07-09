using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EduPlatform.Web.Hubs;

[Authorize]
public sealed class CourseHub : Hub
{
}
