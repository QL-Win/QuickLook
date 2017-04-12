namespace QuickLook.Plugin
{
    public interface IViewer
    {
        PluginType Type { get; }
        string[] SupportExtensions { get; }
        bool CheckSupportByContent(byte[] sample);
        void View(string path, ViewContentContainer container);
        void Close();
    }
}