namespace QuickLook.Plugin
{
    /// <summary>
    ///     Interface implemented by every QuickLook.Plugin
    /// </summary>
    public interface IViewer
    {
        /// <summary>
        ///     Set the priority of this plugin. A plugin with a higher priority may override one with lower priority.
        ///     Set this to int.MaxValue for a maximum priority, int.MinValue for minimum.
        /// </summary>
        int Priority { get; }

        /// <summary>
        ///     Determine whether this plugin can open this file. Please also check the file header, if applicable.
        /// </summary>
        /// <param name="path">The full path of the target file.</param>
        bool CanHandle(string path);

        /// <summary>
        ///     Tell QuickLook the desired window size. Please not do any work that costs a lot of time.
        /// </summary>
        /// <param name="path">The full path of the target file.</param>
        /// <param name="context">A runtime object which allows interaction between this plugin and QuickLook.</param>
        void BoundViewSize(string path, ViewerObject context);

        /// <summary>
        ///     Start the loading process. During the process a busy indicator will be shown. Finish by setting context.IsBusy to
        ///     false.
        /// </summary>
        /// <param name="path">The full path of the target file.</param>
        /// <param name="context">A runtime object which allows interaction between this plugin and QuickLook.</param>
        void View(string path, ViewerObject context);

        /// <summary>
        ///     Release any unmanaged resource here.
        /// </summary>
        void Dispose();
    }
}