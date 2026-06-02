using System.Net;
using System.Text.Json;
using FluentValidation;

namespace Toptanci.Api.Middleware;

/// <summary>
/// Yakalanmayan tüm exception'ları standart bir JSON hata gövdesine çevirir.
/// FluentValidation hatalarını 400 + alan bazlı hata listesi olarak döndürür.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Doğrulama hatası");
            await WriteResponseAsync(context, HttpStatusCode.BadRequest, "Validation",
                "Bir veya daha fazla doğrulama hatası oluştu.",
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlenmeyen istisna");
            var detail = _environment.IsDevelopment() ? ex.ToString() : "Beklenmeyen bir hata oluştu.";
            await WriteResponseAsync(context, HttpStatusCode.InternalServerError, "Failure", detail, null);
        }
    }

    private static Task WriteResponseAsync(
        HttpContext context, HttpStatusCode statusCode, string code, string message,
        IReadOnlyDictionary<string, string[]>? errors)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = new ErrorResponse(code, message, errors, context.TraceIdentifier);
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return context.Response.WriteAsync(json);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

/// <summary>Standart hata response gövdesi.</summary>
public sealed record ErrorResponse(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Errors,
    string TraceId);
