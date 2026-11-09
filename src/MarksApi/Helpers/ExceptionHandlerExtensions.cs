using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MarksApi.Helpers;

public static class ExceptionHandlerExtensions
{
    public static void UseAppExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = feature?.Error;

                if (exception is AppException appException)
                {
                    context.Response.StatusCode = appException.StatusCode;
                    var problem = new ProblemDetails
                    {
                        Title = appException.Title,
                        Status = appException.StatusCode,
                        Detail = appException.Message
                    };

                    if (appException.Errors is not null)
                    {
                        problem.Extensions["errors"] = appException.Errors;
                    }

                    await context.Response.WriteAsJsonAsync(problem);
                    return;
                }

                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GlobalExceptionHandler");
                logger.LogError(exception, "Unhandled exception");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "Please try again later."
                });
            });
        });
    }
}
