using MediaInfoLib;
using System.ComponentModel.Composition;

namespace QuickLook.Plugin.MediaInfoViewer;

[Export]
public static class Exporter
{
    public static MediaInfo Open(string path)
    {
        MediaInfo lib = new MediaInfo()
           .WithOpen(path);

        return lib;
    }
}
