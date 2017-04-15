namespace QuickLook.Plugin
{
    public interface IViewer
    {
        int Priority { get; }
        bool CanHandle(string sample);
        void View(string path, ViewContentContainer container);
        void Close();
    }
}