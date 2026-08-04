namespace Savio.MockServer.Helpers;

public static class BinaryContentInspector
{
    private const string OctetStream = "application/octet-stream";

    public sealed record Assessment(
        bool CanInlinePreview,
        bool IsPdf,
        string EffectiveContentType,
        string? BlockReason);

    public static Assessment AssessForInlinePreview(byte[] bytes, string? declaredContentType)
    {
        var normalizedDeclared = NormalizeContentType(declaredContentType);
        var effectiveFallbackType = string.IsNullOrWhiteSpace(normalizedDeclared)
            ? OctetStream
            : normalizedDeclared;

        if (bytes.Length == 0)
        {
            return new Assessment(false, false, effectiveFallbackType, "Arquivo vazio.");
        }

        var detectedMimeType = DetectMimeType(bytes);
        if (detectedMimeType == null)
        {
            return new Assessment(false, false, effectiveFallbackType,
                "Assinatura de bytes nao reconhecida para visualizacao segura.");
        }

        var isPdf = string.Equals(detectedMimeType, "application/pdf", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(normalizedDeclared) ||
            string.Equals(normalizedDeclared, OctetStream, StringComparison.OrdinalIgnoreCase))
        {
            return new Assessment(true, isPdf, detectedMimeType, null);
        }

        if (!IsPreviewableMimeType(normalizedDeclared))
        {
            return new Assessment(false, isPdf, effectiveFallbackType,
                "Content-Type nao permitido para visualizacao. Apenas imagem e PDF podem ser exibidos.");
        }

        if (!ContentTypeMatches(normalizedDeclared, detectedMimeType))
        {
            return new Assessment(false, isPdf, effectiveFallbackType,
                "Content-Type informado nao corresponde a assinatura real do arquivo.");
        }

        return new Assessment(true, isPdf, detectedMimeType, null);
    }

    private static bool IsPreviewableMimeType(string contentType)
    {
        return string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
               || contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContentTypeMatches(string declaredContentType, string detectedMimeType)
    {
        if (string.Equals(declaredContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(detectedMimeType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        }

        if (declaredContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            if (!detectedMimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(declaredContentType, "image/*", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(declaredContentType, detectedMimeType, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string? NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        var separatorIndex = contentType.IndexOf(';');
        var normalized = separatorIndex >= 0 ? contentType[..separatorIndex] : contentType;
        return normalized.Trim().ToLowerInvariant();
    }

    private static string? DetectMimeType(ReadOnlySpan<byte> bytes)
    {
        if (HasPrefix(bytes, [0x25, 0x50, 0x44, 0x46, 0x2D]))
        {
            return "application/pdf";
        }

        if (HasPrefix(bytes, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return "image/png";
        }

        if (HasPrefix(bytes, [0xFF, 0xD8, 0xFF]))
        {
            return "image/jpeg";
        }

        if (HasPrefix(bytes, [0x47, 0x49, 0x46, 0x38, 0x37, 0x61]) || HasPrefix(bytes, [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]))
        {
            return "image/gif";
        }

        if (bytes.Length >= 12
            && HasPrefix(bytes, [0x52, 0x49, 0x46, 0x46])
            && bytes[8] == 0x57
            && bytes[9] == 0x45
            && bytes[10] == 0x42
            && bytes[11] == 0x50)
        {
            return "image/webp";
        }

        if (HasPrefix(bytes, [0x42, 0x4D]))
        {
            return "image/bmp";
        }

        if (HasPrefix(bytes, [0x49, 0x49, 0x2A, 0x00]) || HasPrefix(bytes, [0x4D, 0x4D, 0x00, 0x2A]))
        {
            return "image/tiff";
        }

        return null;
    }

    private static bool HasPrefix(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> prefix)
    {
        return bytes.Length >= prefix.Length && bytes[..prefix.Length].SequenceEqual(prefix);
    }
}