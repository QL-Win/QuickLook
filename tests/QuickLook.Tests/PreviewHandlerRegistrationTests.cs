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

using Microsoft.Win32;
using QuickLook.Plugin.OfficeViewer;
using System;
using Xunit;

namespace QuickLook.Tests;

/// <summary>
/// Exercises <see cref="PreviewHandlerRegistration"/> against the real registry, using a unique
/// CLSID per test. The 32-bit registration used as the source is written to
/// <c>HKCU\Software\Classes\WOW6432Node\CLSID</c>, which is exactly what the 32-bit registry view
/// of <c>HKEY_CLASSES_ROOT</c> maps to, so the production code path under test is unmodified.
/// Everything a test creates lives under the current user and is deleted again in <see cref="Dispose"/>.
/// </summary>
public sealed class PreviewHandlerRegistrationTests : IDisposable
{
    private const string LocalServer32Key = "LocalServer32";
    private const string InprocServer32Key = "InprocServer32";
    private const string InprocHandler32Key = "InprocHandler32";
    private const string MirrorMarkerValue = "QuickLookMirror";

    private readonly Guid _clsid = Guid.NewGuid();

    public void Dispose()
    {
        DeleteKey(32);
        DeleteKey(64);
    }

    [Fact]
    public void EnsureRegisteredForCurrentView_Mirrors32BitLocalServerHandler()
    {
        Create32BitRegistration("WPS 文字 预览器", @"C:\Program Files (x86)\Kingsoft\WPS Office\wps.exe");

        Assert.True(PreviewHandlerRegistration.EnsureRegisteredForCurrentView(_clsid));
        Assert.True(PreviewHandlerRegistration.IsRegisteredForCurrentView(_clsid));

        using var mirror = OpenMirror();
        Assert.NotNull(mirror);
        Assert.Equal("WPS 文字 预览器", mirror.GetValue(string.Empty) as string);
        Assert.Equal("1", mirror.GetValue(MirrorMarkerValue) as string);
        Assert.Equal("ole32.dll", mirror.OpenSubKey(InprocHandler32Key)?.GetValue(string.Empty) as string);
        Assert.Equal(
            @"C:\Program Files (x86)\Kingsoft\WPS Office\wps.exe",
            mirror.OpenSubKey(LocalServer32Key)?.GetValue(string.Empty) as string);
    }

    [Fact]
    public void EnsureRegisteredForCurrentView_CopiesNamedLocalServer32Values()
    {
        using (var source = Create32BitRegistration("WPS 表格 预览器", @"C:\Program Files (x86)\Kingsoft\WPS Office\et.exe"))
        {
            source.CreateSubKey(LocalServer32Key)?.SetValue("ServerExecutable", @"C:\Program Files (x86)\Kingsoft\WPS Office\et.exe");
        }

        Assert.True(PreviewHandlerRegistration.EnsureRegisteredForCurrentView(_clsid));

        using var mirror = OpenMirror();
        Assert.NotNull(mirror);
        using var localServer = mirror.OpenSubKey(LocalServer32Key);
        Assert.NotNull(localServer);
        Assert.Equal(
            @"C:\Program Files (x86)\Kingsoft\WPS Office\et.exe",
            localServer.GetValue("ServerExecutable") as string);
    }

    [Fact]
    public void EnsureRegisteredForCurrentView_IgnoresNonStringLocalServer32Values()
    {
        using (var source = Create32BitRegistration("WPS 演示 预览器", @"C:\Program Files (x86)\Kingsoft\WPS Office\wpp.exe"))
        {
            source.CreateSubKey(LocalServer32Key)?.SetValue("QuickLookTestDword", 1, RegistryValueKind.DWord);
        }

        Assert.True(PreviewHandlerRegistration.EnsureRegisteredForCurrentView(_clsid));

        using var mirror = OpenMirror();
        Assert.NotNull(mirror);
        using var localServer = mirror.OpenSubKey(LocalServer32Key);
        Assert.NotNull(localServer);
        Assert.Equal(
            @"C:\Program Files (x86)\Kingsoft\WPS Office\wpp.exe",
            localServer.GetValue(string.Empty) as string);
        Assert.DoesNotContain("QuickLookTestDword", localServer.GetValueNames());
    }

    [Fact]
    public void EnsureRegisteredForCurrentView_ReturnsFalseWhenHandlerIsNotRegistered()
    {
        Assert.False(PreviewHandlerRegistration.EnsureRegisteredForCurrentView(_clsid));
        Assert.False(PreviewHandlerRegistration.IsRegisteredForCurrentView(_clsid));
        Assert.Null(OpenMirror());
    }

    [Fact]
    public void EnsureRegisteredForCurrentView_Rejects32BitInProcessOnlyHandlers()
    {
        using (var source = Create32BitRegistration("32-bit in-process previewer", null))
        {
            source.CreateSubKey(InprocServer32Key)?.SetValue(string.Empty, @"C:\fake\preview32.dll");
        }

        Assert.False(PreviewHandlerRegistration.EnsureRegisteredForCurrentView(_clsid));
        Assert.Null(OpenMirror());
    }

    [Fact]
    public void EnsureRegisteredForCurrentView_DoesNotOverwriteRegistrationItDidNotCreate()
    {
        using (var current = OpenKey64(writable: true))
        {
            current.SetValue(string.Empty, "Microsoft Word Previewer");
            current.CreateSubKey(LocalServer32Key)?.SetValue(string.Empty, @"C:\Office\wordprev.exe");
        }

        Create32BitRegistration("WPS 文字 预览器", @"C:\Program Files (x86)\Kingsoft\WPS Office\wps.exe");

        Assert.True(PreviewHandlerRegistration.EnsureRegisteredForCurrentView(_clsid));

        using var untouched = OpenKey64(writable: false);
        Assert.NotNull(untouched);
        Assert.Equal("Microsoft Word Previewer", untouched.GetValue(string.Empty) as string);
        Assert.Null(untouched.GetValue(MirrorMarkerValue));
        Assert.Equal(
            @"C:\Office\wordprev.exe",
            untouched.OpenSubKey(LocalServer32Key)?.GetValue(string.Empty) as string);
    }

    [Fact]
    public void EnsureRegisteredForCurrentView_RefreshesMirrorWhen32BitSourceChanges()
    {
        Create32BitRegistration("WPS 文字 预览器", @"C:\Program Files (x86)\Kingsoft\WPS Office\wps.exe");
        Assert.True(PreviewHandlerRegistration.EnsureRegisteredForCurrentView(_clsid));

        using (var source = OpenKey32(writable: true))
        {
            source.CreateSubKey(LocalServer32Key).SetValue(string.Empty, @"D:\WPS\wps.exe");
            source.SetValue(string.Empty, "WPS 文字 预览器 (更新)");
        }

        Assert.True(PreviewHandlerRegistration.EnsureRegisteredForCurrentView(_clsid));

        using var mirror = OpenMirror();
        Assert.NotNull(mirror);
        Assert.Equal("WPS 文字 预览器 (更新)", mirror.GetValue(string.Empty) as string);
        Assert.Equal(@"D:\WPS\wps.exe", mirror.OpenSubKey(LocalServer32Key)?.GetValue(string.Empty) as string);
    }

    [Fact]
    public void EnsureRegisteredForCurrentView_IsIdempotent()
    {
        Create32BitRegistration("WPS 文字 预览器", @"C:\Program Files (x86)\Kingsoft\WPS Office\wps.exe");

        Assert.True(PreviewHandlerRegistration.EnsureRegisteredForCurrentView(_clsid));
        Assert.True(PreviewHandlerRegistration.EnsureRegisteredForCurrentView(_clsid));

        using var mirror = OpenMirror();
        Assert.NotNull(mirror);
        Assert.Equal("1", mirror.GetValue(MirrorMarkerValue) as string);
    }

    [Fact]
    public void EnsureRegisteredForCurrentView_ReturnsFalseWhen32BitRegistrationHasNoDisplayName()
    {
        using var source = Create32BitRegistration(null, @"C:\Program Files (x86)\Kingsoft\WPS Office\wps.exe");

        Assert.False(PreviewHandlerRegistration.EnsureRegisteredForCurrentView(_clsid));
        Assert.Null(OpenMirror());
    }

    [Fact]
    public void IsRegisteredForCurrentView_RequiresDisplayNameAndActivation()
    {
        using (var current = OpenKey64(writable: true))
        {
            current.SetValue(string.Empty, "Name only");
        }

        Assert.False(PreviewHandlerRegistration.IsRegisteredForCurrentView(_clsid));

        using (var current = OpenKey64(writable: true))
        {
            current.SetValue(string.Empty, string.Empty);
            current.CreateSubKey(LocalServer32Key)?.SetValue(string.Empty, @"C:\fake\server.exe");
        }

        Assert.False(PreviewHandlerRegistration.IsRegisteredForCurrentView(_clsid));

        using (var current = OpenKey64(writable: true))
        {
            current.SetValue(string.Empty, "Complete registration");
        }

        Assert.True(PreviewHandlerRegistration.IsRegisteredForCurrentView(_clsid));
    }

    private RegistryKey Create32BitRegistration(string displayName, string localServerCommand)
    {
        using var classes = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        var key = classes.CreateSubKey(KeyPath32);

        if (displayName != null)
            key.SetValue(string.Empty, displayName);

        if (localServerCommand != null)
            key.CreateSubKey(LocalServer32Key)?.SetValue(string.Empty, localServerCommand);

        return key;
    }

    private RegistryKey OpenKey32(bool writable)
    {
        using var classes = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        return writable
            ? classes.CreateSubKey(KeyPath32)
            : classes.OpenSubKey(KeyPath32);
    }

    private RegistryKey OpenKey64(bool writable)
    {
        using var classes = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        return writable
            ? classes.CreateSubKey(KeyPath64)
            : classes.OpenSubKey(KeyPath64);
    }

    private RegistryKey OpenMirror() => OpenKey64(writable: false);

    private void DeleteKey(int view)
    {
        var path = view == 32 ? KeyPath32 : KeyPath64;

        try
        {
            using var classes = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            classes.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
        }
        catch (Exception)
        {
            // A test must not fail because cleanup raced with another test process.
        }
    }

    private string KeyPath32 => $@"Software\Classes\WOW6432Node\CLSID\{_clsid:B}";

    private string KeyPath64 => $@"Software\Classes\CLSID\{_clsid:B}";
}
