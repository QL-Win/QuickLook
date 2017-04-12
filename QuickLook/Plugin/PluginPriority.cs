using System;

namespace QuickLook.Plugin
{
    [Flags]
    public enum PluginType
    {
        ByExtension = 0x01,
        ByContent = 0x10
    }
}