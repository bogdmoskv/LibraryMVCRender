using LibraryMVC.WebMVC.Models.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryMVC.WebMVC.Filters;

//Неочіквана помилка REST-методу стає HTTP 500 з JSON/XML, а не HTML-сторінкою
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled)
        {
            return;
        }

        if (!context.HttpContext.Request.Path.StartsWithSegments("/api/books"))
        {
            return;
        }

        _logger.LogError(context.Exception, "Помилка REST API видань");
        context.Result = new ObjectResult(new ApiMessageDto
        {
            Message = "Внутрішня помилка сервера"
        })
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
        context.ExceptionHandled = true;
    }
}
