using System;
using System.Runtime.InteropServices;
using System.Security;

namespace WPFMediaKit.MediaFoundation.Interop;

[ComImport, SuppressUnmanagedCodeSecurity,
InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
Guid("83E91E85-82C1-4ea7-801D-85DC50B75086")]
public interface IEVRFilterConfig
{
    [PreserveSig]
    int SetNumberOfStreams(int dwMaxStreams);

    [PreserveSig]
    void GetNumberOfStreams(out int pdwMaxStreams);
}

[ComImport, SuppressUnmanagedCodeSecurity,
Guid("DFDFD197-A9CA-43D8-B341-6AF3503792CD"),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMFVideoRenderer
{
    [PreserveSig]
    int InitializeRenderer([In, MarshalAs(UnmanagedType.Interface)] object pVideoMixer, [In, MarshalAs(UnmanagedType.Interface)] IMFVideoPresenter pVideoPresenter);
}

[ComImport, SuppressUnmanagedCodeSecurity,
InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
Guid("F6696E82-74F7-4F3D-A178-8A5E09C3659F")]
public interface IMFClockStateSink
{
    [PreserveSig]
    int OnClockStart([In] long hnsSystemTime, [In] long llClockStartOffset);

    [PreserveSig]
    int OnClockStop([In] long hnsSystemTime);

    [PreserveSig]
    int OnClockPause([In] long hnsSystemTime);

    [PreserveSig]
    int OnClockRestart([In] long hnsSystemTime);

    [PreserveSig]
    int OnClockSetRate([In] long hnsSystemTime, [In] float flRate);
}

[ComImport, SuppressUnmanagedCodeSecurity,
InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
Guid("29AFF080-182A-4A5D-AF3B-448F3A6346CB")]
public interface IMFVideoPresenter : IMFClockStateSink
{
    #region IMFClockStateSink

    [PreserveSig]
    new void OnClockStart([In] long hnsSystemTime, [In] long llClockStartOffset);

    [PreserveSig]
    new void OnClockStop([In] long hnsSystemTime);

    [PreserveSig]
    new void OnClockPause([In] long hnsSystemTime);

    [PreserveSig]
    new void OnClockRestart([In] long hnsSystemTime);

    [PreserveSig]
    new void OnClockSetRate([In] long hnsSystemTime, [In] float flRate);

    #endregion IMFClockStateSink

    [PreserveSig]
    int ProcessMessage();

    [PreserveSig]
    int GetCurrentMediaType();
}

[ComImport, SuppressUnmanagedCodeSecurity,
Guid("A490B1E4-AB84-4D31-A1B2-181E03B1077A"),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMFVideoDisplayControl
{
    [PreserveSig]
    int GetNativeVideoSize(/* not impl */);

    [PreserveSig]
    int GetIdealVideoSize(/* not impl */);

    [PreserveSig]
    int SetVideoPosition(/* not impl */);

    [PreserveSig]
    int GetVideoPosition(/* not impl */);

    [PreserveSig]
    int SetAspectRatioMode(/* not impl */);

    [PreserveSig]
    int GetAspectRatioMode(/* not impl */);

    [PreserveSig]
    int SetVideoWindow([In] IntPtr hwndVideo);

    [PreserveSig]
    int GetVideoWindow(out IntPtr phwndVideo);

    [PreserveSig]
    int RepaintVideo();

    [PreserveSig]
    int GetCurrentImage(/* not impl */);

    [PreserveSig]
    int SetBorderColor([In] int Clr);

    [PreserveSig]
    int GetBorderColor(out int pClr);

    [PreserveSig]
    int SetRenderingPrefs(/* not impl */);

    [PreserveSig]
    int GetRenderingPrefs(/* not impl */);

    [PreserveSig]
    int SetFullscreen(/* not impl */);

    [PreserveSig]
    int GetFullscreen(/* not impl */);
}
