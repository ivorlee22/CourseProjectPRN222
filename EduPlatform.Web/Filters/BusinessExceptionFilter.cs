using EduPlatform.BLL.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EduPlatform.Web.Filters;

public sealed class BusinessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        context.Result = context.Exception switch
        {
            ResourceNotFoundException => new NotFoundResult(),
            ForbiddenOperationException => new StatusCodeResult(StatusCodes.Status403Forbidden),
            ResourceConflictException => new ConflictResult(),
            _ => null
        };

        context.ExceptionHandled = context.Result is not null;
    }
}
