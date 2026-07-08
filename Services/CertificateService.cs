using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Data.Repositories;
using Savio.MockServer.Models;

namespace Savio.MockServer.Services;

public class CertificateService(IMockCertificateRepository repository)
{
    private readonly IMockCertificateRepository _repository = repository;

    // ── Geração ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gera um certificado X.509 auto-assinado RSA-2048, SHA-256 e persiste no banco.
    /// </summary>
    public async Task<MockCertificate> GenerateAsync(string name, string? password, string? userId)
    {
        using var rsaSeed = RSA.Create(2048);
        var privateKeyPkcs8 = rsaSeed.ExportPkcs8PrivateKey();
        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);

        var request = new CertificateRequest(
            $"CN={name}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));

        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = notBefore.AddYears(1);
        using var cert = request.CreateSelfSigned(notBefore, notAfter);

        var hasPassword = !string.IsNullOrWhiteSpace(password);

        byte[] pfxBytes = hasPassword
            ? cert.Export(X509ContentType.Pfx, password)
            : cert.Export(X509ContentType.Pfx);

        var entity = new MockCertificateEntity
        {
            Name = name,
            Thumbprint = cert.Thumbprint,
            Subject = cert.Subject,
            CertificateData = pfxBytes,
            HasPassword = hasPassword,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = notAfter.DateTime,
            UserId = userId
        };

        await _repository.AddAsync(entity);

        return EntityToModel(entity);
    }

    // ── Consulta ───────────────────────────────────────────────────────────

    public async Task<List<MockCertificate>> GetAllAsync(string? userId = null)
    {
        var entities = await _repository.GetAllAsync(userId);
        return [.. entities.Select(EntityToModel)];
    }

    public async Task<MockCertificate?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? EntityToModel(entity) : null;
    }

    // ── Download ───────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna os bytes do .pfx para download.
    /// </summary>
    public async Task<(byte[] bytes, string fileName)?> GetDownloadAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null || entity.CertificateData.Length == 0)
            return null;

        var safeFileName = string.Concat(entity.Name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_'));
        return (entity.CertificateData, $"{safeFileName}.pfx");
    }

    /// <summary>
    /// Retorna os bytes do certificado público .cer (sem chave privada) para download.
    /// </summary>
    public async Task<(byte[] bytes, string fileName)?> GetCerDownloadAsync(int id, string? password)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null || entity.CertificateData.Length == 0)
            return null;

        X509Certificate2 cert;
        try
        {
            cert = entity.HasPassword && !string.IsNullOrWhiteSpace(password)
                ? new X509Certificate2(entity.CertificateData, password)
                : new X509Certificate2(entity.CertificateData);
        }
        catch
        {
            return null;
        }

        using (cert)
        {
            var cerBytes = cert.Export(X509ContentType.Cert);
            var safeFileName = string.Concat(entity.Name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_'));
            return (cerBytes, $"{safeFileName}.cer");
        }
    }

    /// <summary>
    /// Retorna os bytes dos arquivos .pem (certificado) e .key (chave privada) para clientes como Bruno/Postman.
    /// </summary>
    public async Task<(byte[] certBytes, string certFileName, byte[] keyBytes, string keyFileName)?> GetPemAndKeyDownloadAsync(int id, string? password)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null || entity.CertificateData.Length == 0)
            return null;

        var cert = TryLoadCertificate(entity.CertificateData, password, entity.HasPassword);
        if (cert == null)
            return null;

        using (cert)
        {
            var certPem = cert.ExportCertificatePem();
            var keyPem = TryExportPrivateKeyPem(cert)
                ?? TryExportPrivateKeyPemFromPfx(entity.CertificateData, password);
            if (string.IsNullOrWhiteSpace(keyPem))
                return null;

            var safeFileName = string.Concat(entity.Name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_'));
            return (
                Encoding.UTF8.GetBytes(certPem),
                $"{safeFileName}.pem",
                Encoding.UTF8.GetBytes(keyPem),
                $"{safeFileName}.key");
        }
    }

    private static X509Certificate2? TryLoadCertificate(byte[] pfxBytes, string? password, bool hasPassword)
    {
        var passwordCandidates = new List<string?>();
        if (!string.IsNullOrEmpty(password))
            passwordCandidates.Add(password);

        // Alguns certificados legados podem ter flag de senha inconsistente no banco.
        passwordCandidates.Add(null);
        passwordCandidates.Add(string.Empty);

        var flagsCandidates = new[]
        {
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable,
            X509KeyStorageFlags.Exportable,
            X509KeyStorageFlags.DefaultKeySet
        };

        foreach (var pwd in passwordCandidates.Distinct())
        {
            foreach (var flags in flagsCandidates)
            {
                try
                {
                    var cert = new X509Certificate2(pfxBytes, pwd, flags);
                    if (cert.HasPrivateKey)
                        return cert;

                    cert.Dispose();
                }
                catch
                {
                    // tenta próximo candidato
                }
            }
        }

        return null;
    }

    private static string? TryExportPrivateKeyPem(X509Certificate2 cert)
    {
        try
        {
            using var rsa = cert.GetRSAPrivateKey();
            if (rsa == null)
                return null;

            // 1) PKCS#8 (preferencial para ferramentas HTTP)
            try
            {
                return rsa.ExportPkcs8PrivateKeyPem();
            }
            catch
            {
                // fallback abaixo
            }

            // 2) PKCS#1
            try
            {
                return rsa.ExportRSAPrivateKeyPem();
            }
            catch
            {
                // fallback abaixo
            }

            // 3) Exporta parâmetros e reconstrói em instância exportável em memória
            try
            {
                var parameters = rsa.ExportParameters(true);
                using var exportableRsa = RSA.Create();
                exportableRsa.ImportParameters(parameters);
                return exportableRsa.ExportPkcs8PrivateKeyPem();
            }
            catch
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private static string? TryExportPrivateKeyPemFromPfx(byte[] pfxBytes, string? password)
    {
        var passwordCandidates = new List<string?>
        {
            password,
            string.Empty,
            null
        };

        foreach (var pwd in passwordCandidates.Distinct())
        {
            try
            {
                using var stream = new MemoryStream(pfxBytes);
                var store = new Pkcs12StoreBuilder().Build();
                store.Load(stream, (pwd ?? string.Empty).ToCharArray());

                var alias = store.Aliases.Cast<string>().FirstOrDefault(store.IsKeyEntry);
                if (string.IsNullOrWhiteSpace(alias))
                    continue;

                var keyEntry = store.GetKey(alias);
                if (keyEntry?.Key == null)
                    continue;

                using var writer = new StringWriter();
                var pemWriter = new PemWriter(writer);
                pemWriter.WriteObject(keyEntry.Key);
                pemWriter.Writer.Flush();

                var pem = writer.ToString();
                if (!string.IsNullOrWhiteSpace(pem))
                    return pem;
            }
            catch
            {
                // tenta próximo candidato
            }
        }

        return null;
    }

    // ── Remoção ────────────────────────────────────────────────────────────

    public async Task DeleteAsync(int id) => await _repository.DeleteAsync(id);

    // ── Validação ──────────────────────────────────────────────────────────

    /// <summary>
    /// Valida o certificado de cliente recebido contra o thumbprint esperado.
    /// </summary>
    public static bool ValidateClientCertificate(X509Certificate2 clientCert, string expectedThumbprint)
    {
        return string.Equals(clientCert.Thumbprint, expectedThumbprint, StringComparison.OrdinalIgnoreCase);
    }

    // ── Mapeamento ─────────────────────────────────────────────────────────

    private static MockCertificate EntityToModel(MockCertificateEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Thumbprint = e.Thumbprint,
        Subject = e.Subject,
        HasPassword = e.HasPassword,
        CreatedAt = e.CreatedAt,
        ExpiresAt = e.ExpiresAt
    };
}
