using System.Security.Cryptography.X509Certificates;

namespace QuickLook.Plugin.CertViewer;

internal sealed class CertLoadResult
{
    public bool Success { get; }
    public X509Certificate2 Certificate { get; }
    public string Message { get; }
    public string RawContent { get; }

    public CertLoadResult(bool success, X509Certificate2 certificate, string message, string rawContent)
    {
        Success = success;
        Certificate = certificate;
        Message = message;
        RawContent = rawContent;
    }

    public static CertLoadResult From(bool success, X509Certificate2 certificate, string message, string rawContent)
        => new CertLoadResult(success, certificate, message, rawContent);
}
