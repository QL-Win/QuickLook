using System.Collections.Generic;

namespace QuickLook.Plugin.AppViewer.ApkPackageParser;

public class ApkInfo
{
    public string VersionName { get; set; }

    public string VersionCode { get; set; }

    public string TargetSdkVersion { get; set; }

    public List<string> Permissions { get; set; } = [];

    public string PackageName { get; set; }

    public string MinSdkVersion { get; set; }

    public string Icon { get; set; }

    public Dictionary<string, string> Icons { get; set; } = [];

    public byte[] Logo { get; set; }

    public string Label { get; set; }

    public Dictionary<string, string> Labels { get; set; } = [];

    public bool HasIcon
    {
        get
        {
            if (Icons.Count <= 0)
            {
                return !string.IsNullOrEmpty(Icon);
            }

            return true;
        }
    }

    public List<string> Locales { get; set; } = [];

    public List<string> Densities { get; set; } = [];

    public string LaunchableActivity { get; set; }
}
