// Copyright © 2017-2026 QL-Win Contributors
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using QuickLook.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace QuickLook.Helpers;

/// <summary>
/// Registers the Windows 11 top-level File Explorer command.
/// QuickLook.config <ContextMenu>False</ContextMenu> removes it.
/// </summary>
internal static class ContextMenuRegistration
{
    internal const string PackageName = "QL-Win.QuickLook.Shell";

    const string CertificateSubject = "CN=QuickLook Shell";
    const string CommandClsid = "A7C3E4B1-5D28-4F6A-9B30-8E1F2A4C6D70";
    const string CodeSigningEku = "1.3.6.1.5.5.7.3.3";
    const int CertUntrustedRoot = unchecked((int)0x800B0109);

    internal static void EnsureRegistered()
    {
        if (!App.IsWin11 || App.IsUWP)
            return;

        var thread = new Thread(() =>
        {
            try
            {
                RegisterCore(false);
            }
            catch (Exception ex)
            {
                ProcessHelper.WriteLog(ex.ToString());
            }
        })
        {
            IsBackground = true,
            Name = "QuickLook.ContextMenu",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    internal static void RegisterSynchronously()
    {
        RegisterCore(true);
    }

    static void RegisterCore(bool force)
    {
        if (!App.IsWin11 || App.IsUWP)
            return;

        var enabled = SettingHelper.Get("ContextMenu", true);
        if (!enabled)
        {
            RemovePackage();
            DeleteMarker();
            ProcessHelper.WriteLog("QuickLook context menu unregistered.");
            return;
        }

        var dllName = NativeDllName();
        var dllPath = dllName == null ? null : Path.Combine(App.AppPath, dllName);
        if (dllPath == null || !File.Exists(dllPath) || !File.Exists(App.AppFullPath))
        {
            if (MarkerMatchesThisInstall())
            {
                RemovePackage();
                DeleteMarker();
            }

            ProcessHelper.WriteLog("QuickLook context menu binaries were not found next to QuickLook.exe.");
            return;
        }

        if (!ArchitectureMatches(dllPath))
        {
            ProcessHelper.WriteLog("QuickLook context menu binary does not match this PC architecture.");
            return;
        }

        var identity = ReadIdentity(dllPath);
        var marker = ReadMarker();
        if (!force && marker != null && marker.Matches(identity) && marker.FailedUntil.HasValue && marker.FailedUntil.Value > DateTime.UtcNow)
            return;

        if (!force && marker != null && marker.Matches(identity) && !marker.FailedUntil.HasValue && PackageInstalled())
            return;

        X509Certificate2 certificate = null;
        try
        {
            certificate = GetOrCreateCertificate();
            Trust(certificate, StoreName.TrustedPeople);
            EnsureMachineTrust(certificate);
            RemovePackage();

            var packagePath = Path.Combine(SettingHelper.LocalDataPath, "QuickLook.Shell.msix");
            Directory.CreateDirectory(SettingHelper.LocalDataPath);
            Pack(packagePath, certificate.Subject, identity);
            Sign(dllPath, packagePath, certificate);

            try
            {
                AddPackage(packagePath);
            }
            catch (Exception ex) when (IsUntrusted(ex))
            {
                EnsureMachineTrust(certificate);
                AddPackage(packagePath);
            }

            WriteMarker(identity, null);
            ProcessHelper.WriteLog("QuickLook context menu registered for " + identity.AppDir);
        }
        catch (Exception ex)
        {
            WriteMarker(identity, DateTime.UtcNow.AddDays(1));
            ProcessHelper.WriteLog("QuickLook context menu registration failed: " + ex);
            if (force)
                throw;
        }
        finally
        {
            certificate?.Dispose();
        }
    }

    static string NativeDllName()
    {
        if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            return "QuickLook.NativeArm64.dll";
        if (RuntimeInformation.OSArchitecture == Architecture.X64)
            return "QuickLook.Native64.dll";
        return null;
    }

    static InstallIdentity ReadIdentity(string dllPath)
    {
        var dll = new FileInfo(dllPath);
        var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
        return new InstallIdentity
        {
            AppDir = App.AppPath.TrimEnd('\\'),
            DllName = dll.Name,
            DllTicks = dll.LastWriteTimeUtc.Ticks,
            DllLength = dll.Length,
            Version = version.Major + "." + version.Minor + "." + Math.Max(version.Build, 0) + "." + Math.Max(version.Revision, 0),
            Arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64",
        };
    }

    static bool ArchitectureMatches(string path)
    {
        ushort machine = ReadPeMachine(path);
        if (RuntimeInformation.OSArchitecture == Architecture.X64)
            return machine == 0x8664;
        if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            return machine == 0xAA64;
        return false;
    }

    static ushort ReadPeMachine(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var reader = new BinaryReader(stream))
        {
            stream.Position = 0x3C;
            var peOffset = reader.ReadInt32();
            stream.Position = peOffset;
            if (reader.ReadUInt32() != 0x4550)
                return 0;
            return reader.ReadUInt16();
        }
    }

    static void Pack(string packagePath, string publisher, InstallIdentity identity)
    {
        if (File.Exists(packagePath))
            File.Delete(packagePath);

        var manifest = Encoding.UTF8.GetBytes(BuildManifest(publisher, identity));
        var logos = new[]
        {
            new Logo("Assets\\storelogo.png", CreatePng(50, 0x1F, 0x6F, 0xEB)),
            new Logo("Assets\\Square44x44Logo.png", CreatePng(44, 0x1F, 0x6F, 0xEB)),
            new Logo("Assets\\Square150x150Logo.png", CreatePng(150, 0x1F, 0x6F, 0xEB)),
        };

        IStream output = null;
        IAppxPackageWriter writer = null;
        object factory = null;
        try
        {
            IntPtr hashUri;
            Check(CreateUri("http://www.w3.org/2001/04/xmlenc#sha256", 0x100, IntPtr.Zero, out hashUri), "CreateUri");
            try
            {
                var settings = new AppxPackageSettings { hashMethod = hashUri };
                // The package writer reads, writes, and seeks the output stream.
                output = CreateFileStream(packagePath, 0x00001012);
                factory = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("5842a140-ff9f-4166-8f5c-62f5b7b0c781")));
                var appx = (IAppxFactory)factory;
                Check(appx.CreatePackageWriter(output, ref settings, out writer), "CreatePackageWriter");

                foreach (var logo in logos)
                {
                    var payload = CreateMemoryStream(logo.Bytes);
                    try
                    {
                        Check(writer.AddPayloadFile(logo.Name, "image/png", 1, payload), "AddPayloadFile");
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(payload);
                    }
                }

                var manifestStream = CreateMemoryStream(manifest);
                try
                {
                    Check(writer.Close(manifestStream), "ClosePackage");
                }
                finally
                {
                    Marshal.ReleaseComObject(manifestStream);
                }
            }
            finally
            {
                if (hashUri != IntPtr.Zero)
                    Marshal.Release(hashUri);
            }
        }
        finally
        {
            if (writer != null)
                Marshal.ReleaseComObject(writer);
            if (output != null)
                Marshal.ReleaseComObject(output);
            if (factory != null)
                Marshal.ReleaseComObject(factory);
        }
    }

    static string BuildManifest(string publisher, InstallIdentity identity)
    {
        var escapedPublisher = SecurityElement.Escape(publisher);
        return
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<Package xmlns=\"http://schemas.microsoft.com/appx/manifest/foundation/windows10\"" +
            " xmlns:uap=\"http://schemas.microsoft.com/appx/manifest/uap/windows10\"" +
            " xmlns:uap10=\"http://schemas.microsoft.com/appx/manifest/uap/windows10/10\"" +
            " xmlns:rescap=\"http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities\"" +
            " xmlns:com=\"http://schemas.microsoft.com/appx/manifest/com/windows10\"" +
            " xmlns:desktop4=\"http://schemas.microsoft.com/appx/manifest/desktop/windows10/4\"" +
            " xmlns:desktop5=\"http://schemas.microsoft.com/appx/manifest/desktop/windows10/5\"" +
            " IgnorableNamespaces=\"uap10 rescap com desktop4 desktop5\">" +
            "<Identity Name=\"" + PackageName + "\" Publisher=\"" + escapedPublisher + "\" Version=\"" + identity.Version + "\" ProcessorArchitecture=\"" + identity.Arch + "\" />" +
            "<Properties><DisplayName>QuickLook</DisplayName><PublisherDisplayName>QL-Win</PublisherDisplayName><Logo>Assets\\storelogo.png</Logo><uap10:AllowExternalContent>true</uap10:AllowExternalContent></Properties>" +
            "<Resources><Resource Language=\"en-us\" /></Resources>" +
            "<Dependencies><TargetDeviceFamily Name=\"Windows.Desktop\" MinVersion=\"10.0.22000.0\" MaxVersionTested=\"10.0.26100.0\" /></Dependencies>" +
            "<Capabilities><rescap:Capability Name=\"runFullTrust\" /><rescap:Capability Name=\"unvirtualizedResources\" /></Capabilities>" +
            "<Applications><Application Id=\"QuickLook\" Executable=\"QuickLook.exe\" uap10:TrustLevel=\"mediumIL\" uap10:RuntimeBehavior=\"win32App\">" +
            "<uap:VisualElements AppListEntry=\"none\" DisplayName=\"QuickLook\" Description=\"QuickLook\" BackgroundColor=\"transparent\" Square150x150Logo=\"Assets\\Square150x150Logo.png\" Square44x44Logo=\"Assets\\Square44x44Logo.png\" />" +
            "<Extensions>" +
            "<com:Extension Category=\"windows.comServer\"><com:ComServer><com:SurrogateServer DisplayName=\"QuickLook\">" +
            "<com:Class Id=\"" + CommandClsid + "\" Path=\"" + identity.DllName + "\" ThreadingModel=\"STA\" />" +
            "</com:SurrogateServer></com:ComServer></com:Extension>" +
            "<desktop4:Extension Category=\"windows.fileExplorerContextMenus\"><desktop4:FileExplorerContextMenus>" +
            "<desktop5:ItemType Type=\"*\"><desktop5:Verb Id=\"QuickLookFile\" Clsid=\"" + CommandClsid + "\" /></desktop5:ItemType>" +
            "<desktop5:ItemType Type=\"Directory\"><desktop5:Verb Id=\"QuickLookDirectory\" Clsid=\"" + CommandClsid + "\" /></desktop5:ItemType>" +
            "<desktop5:ItemType Type=\"Directory\\Background\"><desktop5:Verb Id=\"QuickLookBackground\" Clsid=\"" + CommandClsid + "\" /></desktop5:ItemType>" +
            "</desktop4:FileExplorerContextMenus></desktop4:Extension>" +
            "</Extensions></Application></Applications></Package>";
    }

    static void Sign(string dllPath, string packagePath, X509Certificate2 certificate)
    {
        var module = LoadLibrary(dllPath);
        if (module == IntPtr.Zero)
            throw new InvalidOperationException("Loading " + dllPath + " failed: 0x" + Marshal.GetLastWin32Error().ToString("X8"));

        try
        {
            var proc = GetProcAddress(module, "SignAppxPackage");
            if (proc == IntPtr.Zero)
                throw new InvalidOperationException(dllPath + " does not export SignAppxPackage.");

            var sign = Marshal.GetDelegateForFunctionPointer<SignAppxPackageDelegate>(proc);
            var hr = sign(certificate.Handle, packagePath);
            if (hr < 0)
                throw new InvalidOperationException("Signing the context menu package failed: 0x" + hr.ToString("X8"));
        }
        finally
        {
            FreeLibrary(module);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    delegate int SignAppxPackageDelegate(IntPtr certContext, string packagePath);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr LoadLibrary(string fileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr module, string procName);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool FreeLibrary(IntPtr module);

    static void Check(int hr, string step)
    {
        if (hr < 0)
            throw new InvalidOperationException(step + " failed: 0x" + hr.ToString("X8"));
    }

    static void AddPackage(string packagePath)
    {
        var manager = new PackageManager();
        var options = new AddPackageOptions
        {
            ExternalLocationUri = new Uri(App.AppPath),
            ForceUpdateFromAnyVersion = true,
        };
        var operation = manager.AddPackageByUriAsync(new Uri(packagePath), options);
        Wait(operation);
        var result = operation.GetResults();
        if (result.ExtendedErrorCode != null)
            throw result.ExtendedErrorCode;
    }

    static void RemovePackage()
    {
        try
        {
            var manager = new PackageManager();
            foreach (var package in manager.FindPackagesForUser(string.Empty))
            {
                if (!string.Equals(package.Id.Name, PackageName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var operation = manager.RemovePackageAsync(package.Id.FullName);
                Wait(operation);
                var result = operation.GetResults();
                if (result != null && result.ExtendedErrorCode != null)
                    throw result.ExtendedErrorCode;
            }
        }
        catch (Exception ex)
        {
            ProcessHelper.WriteLog(ex.ToString());
        }
    }

    static bool PackageInstalled()
    {
        var manager = new PackageManager();
        return manager.FindPackagesForUser(string.Empty).Any(package =>
            string.Equals(package.Id.Name, PackageName, StringComparison.OrdinalIgnoreCase));
    }

    static void Wait(IAsyncInfo operation)
    {
        while (operation.Status == AsyncStatus.Started)
            Thread.Sleep(50);

        if (operation.Status == AsyncStatus.Error)
            throw operation.ErrorCode ?? new InvalidOperationException("Package deployment failed.");
        if (operation.Status == AsyncStatus.Canceled)
            throw new OperationCanceledException("Package deployment was canceled.");
    }

    static bool IsUntrusted(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current.HResult == CertUntrustedRoot)
                return true;
            if (current.Message != null && current.Message.IndexOf("800B0109", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    static X509Certificate2 GetOrCreateCertificate()
    {
        using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadOnly);
            foreach (var cert in store.Certificates.Cast<X509Certificate2>())
            {
                if (cert.HasPrivateKey
                    && cert.NotAfter > DateTime.UtcNow.AddDays(30)
                    && cert.Subject.IndexOf("QuickLook Shell", StringComparison.OrdinalIgnoreCase) >= 0
                    && HasCodeSigningEku(cert))
                {
                    return new X509Certificate2(cert);
                }
            }
        }

        var privateKey = CreateCom("X509Enrollment.CX509PrivateKey");
        privateKey.ProviderName = "Microsoft Software Key Storage Provider";
        privateKey.Length = 2048;
        privateKey.KeySpec = 0;
        privateKey.MachineContext = false;
        privateKey.Create();

        var name = CreateCom("X509Enrollment.CX500DistinguishedName");
        name.Encode(CertificateSubject, 0);

        var request = CreateCom("X509Enrollment.CX509CertificateRequestCertificate");
        request.InitializeFromPrivateKey(1, privateKey, string.Empty);
        request.Subject = name;
        request.Issuer = name;
        request.NotBefore = DateTime.UtcNow.AddDays(-1);
        request.NotAfter = DateTime.UtcNow.AddYears(10);

        var hash = CreateCom("X509Enrollment.CObjectId");
        hash.InitializeFromAlgorithmName(1, 0, 0, "SHA256");
        request.HashAlgorithm = hash;

        var ekuOid = CreateCom("X509Enrollment.CObjectId");
        ekuOid.InitializeFromValue(CodeSigningEku);
        var ekuOids = CreateCom("X509Enrollment.CObjectIds");
        ekuOids.Add(ekuOid);
        var eku = CreateCom("X509Enrollment.CX509ExtensionEnhancedKeyUsage");
        eku.InitializeEncode(ekuOids);

        var keyUsage = CreateCom("X509Enrollment.CX509ExtensionKeyUsage");
        keyUsage.InitializeEncode(0x80);

        request.X509Extensions.Add(keyUsage);
        request.X509Extensions.Add(eku);
        request.Encode();

        var enrollment = CreateCom("X509Enrollment.CX509Enrollment");
        enrollment.InitializeFromRequest(request);
        string encodedRequest = enrollment.CreateRequest(1);
        enrollment.InstallResponse(2, encodedRequest, 1, string.Empty);

        var installed = new X509Certificate2(Convert.FromBase64String((string)enrollment.Certificate));
        using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadOnly);
            foreach (var cert in store.Certificates.Cast<X509Certificate2>())
            {
                if (string.Equals(cert.Thumbprint, installed.Thumbprint, StringComparison.OrdinalIgnoreCase) && cert.HasPrivateKey)
                    return new X509Certificate2(cert);
            }
        }

        throw new InvalidOperationException("The context menu signing certificate was created but has no private key.");
    }

    static void EnsureMachineTrust(X509Certificate2 certificate)
    {
        if (IsInStore(StoreName.Root, StoreLocation.LocalMachine, certificate))
            return;

        var cerPath = Path.Combine(SettingHelper.LocalDataPath, "QuickLook.Shell.cer");
        Directory.CreateDirectory(SettingHelper.LocalDataPath);
        File.WriteAllBytes(cerPath, certificate.Export(X509ContentType.Cert));

        if (!IsElevated())
        {
            var script = "$ErrorActionPreference='Stop'; Import-Certificate -FilePath '" + cerPath + "' -CertStoreLocation Cert:\\LocalMachine\\Root | Out-Null; Import-Certificate -FilePath '" + cerPath + "' -CertStoreLocation Cert:\\LocalMachine\\TrustedPeople | Out-Null";
            var start = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script + "\"",
                UseShellExecute = true,
                Verb = "runas",
            };
            using (var process = Process.Start(start))
            {
                if (process == null)
                    throw new InvalidOperationException("Could not start the elevated certificate import.");
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("Elevated certificate import failed: " + process.ExitCode);
            }
        }
        else
        {
            AddPublicCert(certificate, StoreName.Root, StoreLocation.LocalMachine);
            AddPublicCert(certificate, StoreName.TrustedPeople, StoreLocation.LocalMachine);
        }

        if (!IsInStore(StoreName.Root, StoreLocation.LocalMachine, certificate))
            throw new InvalidOperationException("The context menu certificate is not in the machine root store.");
    }

    static bool IsElevated()
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    static bool IsInStore(StoreName storeName, StoreLocation location, X509Certificate2 certificate)
    {
        using (var store = new X509Store(storeName, location))
        {
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            return store.Certificates.Cast<X509Certificate2>().Any(cert =>
                string.Equals(cert.Thumbprint, certificate.Thumbprint, StringComparison.OrdinalIgnoreCase));
        }
    }

    static void AddPublicCert(X509Certificate2 certificate, StoreName storeName, StoreLocation location)
    {
        var publicCert = new X509Certificate2(certificate.Export(X509ContentType.Cert));
        using (var store = new X509Store(storeName, location))
        {
            store.Open(OpenFlags.ReadWrite);
            if (!store.Certificates.Cast<X509Certificate2>().Any(cert =>
                string.Equals(cert.Thumbprint, publicCert.Thumbprint, StringComparison.OrdinalIgnoreCase)))
            {
                store.Add(publicCert);
            }
        }
    }

    static void Trust(X509Certificate2 certificate, StoreName storeName)
    {
        var publicCert = new X509Certificate2(certificate.Export(X509ContentType.Cert));
        using (var store = new X509Store(storeName, StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadWrite);
            var exists = store.Certificates.Cast<X509Certificate2>()
                .Any(cert => string.Equals(cert.Thumbprint, publicCert.Thumbprint, StringComparison.OrdinalIgnoreCase));
            if (!exists)
                store.Add(publicCert);
        }
    }

    static bool HasCodeSigningEku(X509Certificate2 certificate)
    {
        foreach (var extension in certificate.Extensions)
        {
            var eku = extension as X509EnhancedKeyUsageExtension;
            if (eku == null)
                continue;

            foreach (var oid in eku.EnhancedKeyUsages)
            {
                if (oid.Value == CodeSigningEku)
                    return true;
            }
        }

        return false;
    }

    static dynamic CreateCom(string progId)
    {
        var type = Type.GetTypeFromProgID(progId, true);
        return Activator.CreateInstance(type);
    }

    static string MarkerPath => Path.Combine(SettingHelper.LocalDataPath, "ContextMenu.registration");

    static bool MarkerMatchesThisInstall()
    {
        var marker = ReadMarker();
        return marker != null && string.Equals(marker.Identity.AppDir, App.AppPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
    }

    static Marker ReadMarker()
    {
        try
        {
            if (!File.Exists(MarkerPath))
                return null;

            var lines = File.ReadAllLines(MarkerPath);
            if (lines.Length < 7)
                return null;

            var identity = new InstallIdentity
            {
                AppDir = lines[1],
                DllTicks = long.Parse(lines[2]),
                DllLength = long.Parse(lines[3]),
                Version = lines[4],
                Arch = lines[5],
                DllName = lines[6],
            };
            DateTime? failedUntil = null;
            if (lines[0] == "failed" && lines.Length >= 8)
                failedUntil = new DateTime(long.Parse(lines[7]), DateTimeKind.Utc);

            return new Marker { Identity = identity, FailedUntil = failedUntil };
        }
        catch (Exception ex)
        {
            ProcessHelper.WriteLog(ex.ToString());
            return null;
        }
    }

    static void WriteMarker(InstallIdentity identity, DateTime? failedUntil)
    {
        Directory.CreateDirectory(SettingHelper.LocalDataPath);
        var lines = new List<string>
        {
            failedUntil.HasValue ? "failed" : "ok",
            identity.AppDir,
            identity.DllTicks.ToString(),
            identity.DllLength.ToString(),
            identity.Version,
            identity.Arch,
            identity.DllName,
        };
        if (failedUntil.HasValue)
            lines.Add(failedUntil.Value.Ticks.ToString());

        File.WriteAllLines(MarkerPath, lines);
    }

    static void DeleteMarker()
    {
        try
        {
            if (File.Exists(MarkerPath))
                File.Delete(MarkerPath);
        }
        catch (Exception ex)
        {
            ProcessHelper.WriteLog(ex.ToString());
        }
    }

    static byte[] CreatePng(int size, byte red, byte green, byte blue)
    {
        var rowBytes = 1 + (size * 3);
        var raw = new byte[rowBytes * size];
        for (var y = 0; y < size; y++)
        {
            var row = y * rowBytes;
            raw[row] = 0;
            for (var x = 0; x < size; x++)
            {
                var pixel = row + 1 + (x * 3);
                raw[pixel] = red;
                raw[pixel + 1] = green;
                raw[pixel + 2] = blue;
            }
        }

        using (var png = new MemoryStream())
        {
            png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
            var ihdr = new byte[13];
            WriteBigEndian(ihdr, 0, size);
            WriteBigEndian(ihdr, 4, size);
            ihdr[8] = 8;
            ihdr[9] = 2;
            WriteChunk(png, "IHDR", ihdr);
            WriteChunk(png, "IDAT", Zlib(raw));
            WriteChunk(png, "IEND", Array.Empty<byte>());
            return png.ToArray();
        }
    }

    static byte[] Zlib(byte[] raw)
    {
        using (var stream = new MemoryStream())
        {
            stream.WriteByte(0x78);
            stream.WriteByte(0x9C);
            using (var deflate = new DeflateStream(stream, CompressionLevel.Fastest, true))
                deflate.Write(raw, 0, raw.Length);

            var adler = Adler32(raw);
            stream.WriteByte((byte)(adler >> 24));
            stream.WriteByte((byte)(adler >> 16));
            stream.WriteByte((byte)(adler >> 8));
            stream.WriteByte((byte)adler);
            return stream.ToArray();
        }
    }

    static uint Adler32(byte[] data)
    {
        uint a = 1;
        uint b = 0;
        foreach (var value in data)
        {
            a = (a + value) % 65521;
            b = (b + a) % 65521;
        }

        return (b << 16) | a;
    }

    static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        var crcSource = new byte[4 + data.Length];
        Buffer.BlockCopy(typeBytes, 0, crcSource, 0, 4);
        if (data.Length > 0)
            Buffer.BlockCopy(data, 0, crcSource, 4, data.Length);

        WriteBigEndian(stream, data.Length);
        stream.Write(typeBytes, 0, 4);
        if (data.Length > 0)
            stream.Write(data, 0, data.Length);
        WriteBigEndian(stream, unchecked((int)Crc32(crcSource)));
    }

    static readonly uint[] CrcTable = BuildCrcTable();

    static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < table.Length; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }

        return table;
    }

    static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }

    static void WriteBigEndian(Stream stream, int value)
    {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    static void WriteBigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    static IStream CreateFileStream(string path, uint mode)
    {
        IStream stream;
        var hr = SHCreateStreamOnFileEx(path, mode, 0, true, IntPtr.Zero, out stream);
        Marshal.ThrowExceptionForHR(hr);
        return stream;
    }

    static IStream CreateMemoryStream(byte[] data)
    {
        IStream stream;
        Marshal.ThrowExceptionForHR(CreateStreamOnHGlobal(IntPtr.Zero, true, out stream));
        stream.Write(data, data.Length, IntPtr.Zero);
        stream.Seek(0, 0, IntPtr.Zero);
        return stream;
    }

    [DllImport("urlmon.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    static extern int CreateUri(string uri, uint flags, IntPtr reserved, out IntPtr uriObject);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    static extern int SHCreateStreamOnFileEx(string fileName, uint mode, uint attributes, bool create, IntPtr template, out IStream stream);

    [DllImport("ole32.dll", PreserveSig = true)]
    static extern int CreateStreamOnHGlobal(IntPtr global, bool deleteOnRelease, out IStream stream);

    [StructLayout(LayoutKind.Sequential)]
    struct AppxPackageSettings
    {
        public int forceZip32;
        public IntPtr hashMethod;
    }

    [ComImport]
    [Guid("beb94909-e451-438b-b5a7-d79e767b75d8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAppxFactory
    {
        [PreserveSig]
        int CreatePackageWriter(IStream outputStream, ref AppxPackageSettings settings, out IAppxPackageWriter packageWriter);
    }

    [ComImport]
    [Guid("9099e33b-246f-41e4-881a-008eb613f858")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAppxPackageWriter
    {
        [PreserveSig]
        int AddPayloadFile([MarshalAs(UnmanagedType.LPWStr)] string fileName, [MarshalAs(UnmanagedType.LPWStr)] string contentType, int compression, IStream inputStream);

        [PreserveSig]
        int Close(IStream manifest);
    }

    sealed class InstallIdentity
    {
        public string AppDir;
        public string DllName;
        public long DllTicks;
        public long DllLength;
        public string Version;
        public string Arch;

        public bool Matches(InstallIdentity other)
        {
            return other != null
                && string.Equals(AppDir, other.AppDir, StringComparison.OrdinalIgnoreCase)
                && string.Equals(DllName, other.DllName, StringComparison.OrdinalIgnoreCase)
                && DllTicks == other.DllTicks
                && DllLength == other.DllLength
                && Version == other.Version
                && Arch == other.Arch;
        }
    }

    sealed class Marker
    {
        public InstallIdentity Identity;
        public DateTime? FailedUntil;

        public bool Matches(InstallIdentity identity)
        {
            return Identity != null && Identity.Matches(identity);
        }
    }

    sealed class Logo
    {
        public Logo(string name, byte[] bytes)
        {
            Name = name;
            Bytes = bytes;
        }

        public string Name { get; }
        public byte[] Bytes { get; }
    }
}
