using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace QuickLook.Plugin.CertViewer;

internal static class CertUtils
{
    /// <summary>
    /// TryLoadCertificate returns a <see cref="CertLoadResult"/> containing:
    /// - Success: whether loading/parsing succeeded
    /// - Certificate: the parsed X509Certificate2 (may be null)
    /// - Message: an informational or error message
    /// - RawContent: original file text or hex when parsing failed
    /// </summary>
    public static CertLoadResult TryLoadCertificate(string path)
    {
        try
        {
            var ext = Path.GetExtension(path)?.ToLowerInvariant();

            if (ext == ".pfx" || ext == ".p12")
            {
                try
                {
                    var cert = new X509Certificate2(path);
                    return new CertLoadResult(true, cert, string.Empty, null);
                }
                catch (Exception ex)
                {
                    return new CertLoadResult(false, null, "Failed to load PFX/P12: " + ex.Message, null);
                }
            }

            // Try DER/PEM style cert (.cer/.crt/.pem)
            var text = File.ReadAllText(path);

            const string begin = "-----BEGIN CERTIFICATE-----";
            const string end = "-----END CERTIFICATE-----";

            if (text.Contains(begin))
            {
                var startIdx = text.IndexOf(begin, StringComparison.Ordinal);
                var endIdx = text.IndexOf(end, StringComparison.Ordinal);
                if (startIdx >= 0 && endIdx > startIdx)
                {
                    var b64 = text.Substring(startIdx + begin.Length, endIdx - (startIdx + begin.Length));
                    b64 = new string(b64.Where(c => !char.IsWhiteSpace(c)).ToArray());
                    try
                    {
                        var raw = Convert.FromBase64String(b64);
                        var cert = new X509Certificate2(raw);
                        return new CertLoadResult(true, cert, string.Empty, text);
                    }
                    catch (Exception ex)
                    {
                        return new CertLoadResult(false, null, "PEM decode failed: " + ex.Message, text);
                    }
                }
            }

            // Try raw DER
            try
            {
                var bytes = File.ReadAllBytes(path);
                // heuristics: if starts with 0x30 (ASN.1 SEQUENCE) it's likely DER encoded
                if (bytes.Length > 0 && bytes[0] == 0x30)
                {
                    try
                    {
                        var cert = new X509Certificate2(bytes);
                        return new CertLoadResult(true, cert, string.Empty, null);
                    }
                    catch
                    {
                        // not a certificate DER
                    }
                }
            }
            catch
            {
            }

            // Unsupported or not parseable: return raw text or hex
            try
            {
                var rawText = File.ReadAllText(path);
                return new CertLoadResult(false, null, "Could not parse as certificate; showing raw content.", rawText);
            }
            catch
            {
                // fallback to hex
                try
                {
                    var bytes = File.ReadAllBytes(path);
                    var hex = BitConverter.ToString(bytes).Replace("-", " ");
                    return new CertLoadResult(false, null, "Could not parse as certificate; showing hex.", hex);
                }
                catch (Exception ex)
                {
                    return new CertLoadResult(false, null, "Failed to read file: " + ex.Message, null);
                }
            }
        }
        catch (Exception ex)
        {
            return new CertLoadResult(false, null, "Internal error: " + ex.Message, null);
        }
    }
}
