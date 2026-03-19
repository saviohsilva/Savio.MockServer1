using System.Text;
using Microsoft.AspNetCore.Http;
using Savio.MockServer.Models;
using System.Text.Json;

namespace Savio.MockServer.Services;

public static class HttpRequestCaptureService
{
    public sealed record CapturedRequest(
        string? TextBody,
        string? FormJson,
        string? BodyBase64,
        string BodyContentType,
        string? BodyFileName);

    public static async Task<CapturedRequest> CaptureAsync(HttpRequest request)
    {
        request.EnableBuffering();

        var requestContentType = request.ContentType ?? string.Empty;

        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();

            var payload = new MultipartPayload();

            foreach (var kv in form)
            {
                foreach (var value in kv.Value)
                {
                    payload.Fields.Add(new MultipartPayload.FormField { Name = kv.Key, Value = value ?? string.Empty });
                }
            }

            foreach (var file in form.Files)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);

                var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : (file.ContentType ?? "application/octet-stream");

                payload.Files.Add(new MultipartPayload.FormFilePart
                {
                    Name = file.Name,
                    FileName = file.FileName,
                    ContentType = contentType,
                    Length = file.Length,
                    Base64 = Convert.ToBase64String(ms.ToArray())
                });
            }

            request.Body.Position = 0;

            return new CapturedRequest(
                TextBody: null,
                FormJson: JsonSerializer.Serialize(payload),
                BodyBase64: null,
                BodyContentType: requestContentType,
                BodyFileName: null);
        }

        try
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var text = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            if (text.Any(ch => ch == '\0'))
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                return new CapturedRequest(
                    TextBody: null,
                    FormJson: null,
                    BodyBase64: Convert.ToBase64String(bytes),
                    BodyContentType: requestContentType,
                    BodyFileName: null);
            }

            return new CapturedRequest(
                TextBody: text,
                FormJson: null,
                BodyBase64: null,
                BodyContentType: requestContentType,
                BodyFileName: null);
        }
        catch
        {
            request.Body.Position = 0;
            using var ms = new MemoryStream();
            await request.Body.CopyToAsync(ms);
            request.Body.Position = 0;

            return new CapturedRequest(
                TextBody: null,
                FormJson: null,
                BodyBase64: Convert.ToBase64String(ms.ToArray()),
                BodyContentType: requestContentType,
                BodyFileName: null);
        }
    }
}
