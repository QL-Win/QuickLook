using System;

namespace WPFMediaKit.DirectShow.MediaPlayers;

public struct FilterName
{
    public FilterName(string name, Guid clsid, string filename) : this()
    {
        Name = name;
        CLSID = clsid;
        Filename = filename;
    }

    public string Name { get; set; }
    public Guid CLSID { get; set; }
    public string Filename { get; set; }
}
