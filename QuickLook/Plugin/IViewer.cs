namespace QuickLook.Plugin
{
    public interface IViewer
    {
        bool CanView(string path, byte[] sample);
        void View(string path, ViewContentContainer container);
        void Close();
    }
}
