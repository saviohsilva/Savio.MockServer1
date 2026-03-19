using System.Text;
using System.Text.Json;
using Savio.MockServer.Data.Repositories;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Services;

namespace Savio.MockServer.Middleware;

public class RequestHistoryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestHistoryMiddleware> _logger;
    // Limite de 5MB para captura de texto do response body
    private const int MaxResponseBodySizeForCapture = 5 * 1024 * 1024;

    public RequestHistoryMiddleware(RequestDelegate next, ILogger<RequestHistoryMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    private static bool IsInternalRoute(string path)
    {
        if (path == "/")
            return true;

        foreach (var prefix in MockEndpointMiddleware.InternalPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public async Task InvokeAsync(HttpContext context,
        IMockRepository mockRepo,
        IRequestHistoryRepository historyRepo,
        IUnmockedRequestRepository unmockedRepo)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Ignorar rotas internas (Blazor, assets, páginas de gerenciamento)
        if (IsInternalRoute(path))
        {
            await _next(context);
            return;
        }

        // Capturar request (inclui multipart/form-data)
        var capturedRequest = await HttpRequestCaptureService.CaptureAsync(context.Request);
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        // Restaurar o stream original do response body
        context.Response.Body = originalBodyStream;

        // Capturar response (texto apenas aqui; binário será tratado no middleware do mock)
        var responseBodyText = await ReadResponseBodyAsTextAsync(responseBody);
        
        // Copiar response body de volta para o stream original
        try
        {
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao copiar response body para o stream original");
        }

        var mock = await mockRepo.GetActiveByRouteAndMethodAsync(
            context.Request.Path,
            context.Request.Method);

        if (mock != null)
        {
            try
            {
                var history = new RequestHistoryEntity
                {
                    MockEndpointId = mock.Id,
                    Method = context.Request.Method,
                    Route = context.Request.Path,
                    RequestHeadersJson = JsonSerializer.Serialize(
                        context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
                    RequestBody = capturedRequest.TextBody,
                    RequestFormJson = capturedRequest.FormJson,
                    RequestBodyBase64 = capturedRequest.BodyBase64,
                    RequestBodyContentType = capturedRequest.BodyContentType,
                    RequestBodyFileName = capturedRequest.BodyFileName,
                    ResponseStatusCode = context.Response.StatusCode,
                    ResponseHeadersJson = JsonSerializer.Serialize(
                        context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
                    ResponseBody = responseBodyText,
                    DelayMs = mock.DelayMs,
                    ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                    RequestedAt = DateTime.UtcNow
                };

                await historyRepo.AddAsync(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar histórico para mock {Method} {Route}", context.Request.Method, context.Request.Path);
            }
        }
        else
        {
            try
            {
                // Usar a rota sem prefixo de alias (resolvida pelo MockEndpointMiddleware)
                var routeForRecord = context.Items.TryGetValue("MockRoute", out var mockRouteObj) && mockRouteObj is string mockRoute
                    ? mockRoute
                    : context.Request.Path.Value ?? string.Empty;

                var unmockedRequest = new UnmockedRequestEntity
                {
                    Method = context.Request.Method,
                    Route = routeForRecord,
                    RequestHeadersJson = JsonSerializer.Serialize(
                        context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
                    RequestBody = capturedRequest.TextBody,
                    RequestFormJson = capturedRequest.FormJson,
                    RequestBodyBase64 = capturedRequest.BodyBase64,
                    RequestBodyContentType = capturedRequest.BodyContentType,
                    RequestBodyFileName = capturedRequest.BodyFileName,
                    LastClientIp = context.Connection.RemoteIpAddress?.ToString(),
                    LastSeenAt = DateTime.UtcNow
                };

                await unmockedRepo.AddOrUpdateAsync(unmockedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar unmocked request {Method} {Route}", context.Request.Method, context.Request.Path);
            }
        }
    }

    private async Task<string> ReadResponseBodyAsTextAsync(Stream responseStream)
    {
        try
        {
            if (responseStream.Length > MaxResponseBodySizeForCapture)
            {
                _logger.LogWarning("Response body muito grande ({Size} bytes), não será capturado completamente", responseStream.Length);
                return $"[Response muito grande: {responseStream.Length} bytes - não capturado]";
            }

            responseStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseStream, Encoding.UTF8, leaveOpen: true);
            var text = await reader.ReadToEndAsync();
            responseStream.Seek(0, SeekOrigin.Begin);
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao ler response body como texto");
            try
            {
                responseStream.Seek(0, SeekOrigin.Begin);
            }
            catch
            {
                // Ignora erro ao tentar resetar posição
            }
            return string.Empty;
        }
    }
}
