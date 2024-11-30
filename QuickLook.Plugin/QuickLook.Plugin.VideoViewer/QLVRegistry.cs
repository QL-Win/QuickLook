using Microsoft.Win32;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Plugin.VideoViewer.Properties;
using RegFileParser;

namespace QuickLook.Plugin.VideoViewer;

internal static class QLVRegistry
{
    internal static void Register()
    {
        var obj = new RegFileObject(Resource.QLV);

        obj.RegValues.ForEach(k => k.Value.ForEach(k2 => ImportKey(k2.Value)));
    }

    private static void ImportKey(RegValueObject key)
    {
        var kind = RegistryValueKind.Unknown;

        if (key.Type == "REG_NONE")
            kind = RegistryValueKind.None;
        else if (key.Type == "REG_SZ")
            kind = RegistryValueKind.String;
        else if (key.Type == "EXPAND_SZ")
            kind = RegistryValueKind.ExpandString;
        else if (key.Type == "REG_BINARY")
            kind = RegistryValueKind.Binary;
        else if (key.Type == "REG_DWORD")
            kind = RegistryValueKind.DWord;
        else if (key.Type == "REG_MULTI_SZ")
            kind = RegistryValueKind.MultiString;
        else if (key.Type == "REG_QWORD")
            kind = RegistryValueKind.QWord;

        Registry.SetValue(key.ParentKey, key.Entry, key.Value, kind);
    }
}
