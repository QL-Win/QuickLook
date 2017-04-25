namespace QuickLook.Plugin
{
    public interface IViewer
    {
        int Priority { get; }
        bool CanHandle(string path);
        void Prepare(string path, ViewContentContainer container);
        void View(string path, ViewContentContainer container);
        void Close();
    }
}