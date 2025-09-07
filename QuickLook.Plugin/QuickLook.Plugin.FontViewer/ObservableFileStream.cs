using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuickLook.Plugin.FontViewer;

public class ObservableFileStream(string path, FileMode mode, FileAccess access, FileShare share) : FileStream(path, mode, access, share)
{
    public bool IsEndOfStream { get; protected set; } = false;

    public override int Read(byte[] array, int offset, int count)
    {
        int result = base.Read(array, offset, count);
        if (result == 0)
            IsEndOfStream = true;
        return result;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int result = await base.ReadAsync(buffer, offset, count, cancellationToken);
        if (result == 0)
            IsEndOfStream = true;
        return result;
    }
}
