using DirectShowLib;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;

namespace WPFMediaKit.DirectX;

[StructLayout(LayoutKind.Sequential)]
public class RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}

/// <summary>
/// CLSID_IDirect3DDevice9
/// </summary>
[ComImport, Guid("D0223B96-BF7A-43fd-92BD-A43B0D82B9EB"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
public interface IDirect3DDevice9
{
    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int TestCooperativeLevel();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    uint GetAvailableTextureMem();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int EvictManagedResources();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetDirect3D([Out] out IDirect3D9 ppD3D9);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetDeviceCaps([In, Out] ref D3DCAPS9 pCaps);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetDisplayMode(uint iSwapChain, D3DDISPLAYMODE pMode);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetCreationParameters([In, Out] ref D3DDEVICE_CREATION_PARAMETERS pParameters);

    int SetCursorProperties();

    int SetCursorPosition();

    int ShowCursor(bool bShow);

    int CreateAdditionalSwapChain();

    int GetSwapChain();

    uint GetNumberOfSwapChains();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int Reset([In, Out] ref D3DPRESENT_PARAMETERS pPresentationParameters);

    int Present();

    int GetBackBuffer();

    int GetRasterStatus();

    int SetDialogBoxMode();

    int SetGammaRamp();

    int GetGammaRamp();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int CreateTexture(int Width, int Height, int Levels, int Usage, D3DFORMAT Format, int Pool,
                      out IDirect3DTexture9 ppTexture, IntPtr pSharedHandle);

    int CreateVolumeTexture();

    int CreateCubeTexture();

    int CreateVertexBuffer();

    int CreateIndexBuffer();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int CreateRenderTarget(int width, int height, D3DFORMAT Format, D3DMULTISAMPLE_TYPE MultiSample,
                             uint MultisampleQuality, [MarshalAs(UnmanagedType.Bool)] bool Lockable, [Out] out IntPtr pSurface,
                             IntPtr pSharedSurface);

    int CreateDepthStencilSurface();

    int UpdateSurface();

    int UpdateTexture();

    int GetRenderTargetData();

    int GetFrontBufferData();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int StretchRect(IntPtr pSourceSurface, DsRect pSourceRect, IDirect3DSurface9 pDestSurface, DsRect pDestRect, int Filter);

    int ColorFill();

    int CreateOffscreenPlainSurface();

    int SetRenderTarget();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetRenderTarget([Out] out IntPtr pSurface);

    int SetDepthStencilSurface();

    int GetDepthStencilSurface();

    int BeginScene();

    int EndScene();

    int Clear();

    int SetTransform();

    int GetTransform();

    int MultiplyTransform();

    int SetViewport();

    int GetViewport();

    int SetMaterial();

    int GetMaterial();

    int SetLight();

    int GetLight();

    int LightEnable();

    int GetLightEnable();

    int SetClipPlane();

    int GetClipPlane();

    int SetRenderState();

    int GetRenderState();

    int CreateStateBlock();

    int BeginStateBlock();

    int EndStateBlock();

    int SetClipStatus();

    int GetClipStatus();

    int GetTexture();

    int SetTexture();

    int GetTextureStageState();

    int SetTextureStageState();

    int GetSamplerState();

    int SetSamplerState();

    int ValidateDevice();

    int SetPaletteEntries();

    int GetPaletteEntries();

    int SetCurrentTexturePalette();

    int GetCurrentTexturePalette();

    int SetScissorRect();

    int GetScissorRect();

    int SetSoftwareVertexProcessing(bool bSoftware);

    bool GetSoftwareVertexProcessing();

    int SetNPatchMode(float nSegments);

    float GetNPatchMode();

    int DrawPrimitive();

    int DrawIndexedPrimitive();

    int DrawPrimitiveUP();

    int DrawIndexedPrimitiveUP();

    int ProcessVertices();

    int CreateVertexDeclaration();

    int SetVertexDeclaration();

    int GetVertexDeclaration();

    int SetFVF();

    int GetFVF();

    int CreateVertexShader();

    int SetVertexShader();

    int GetVertexShader();

    int SetVertexShaderConstantF();

    int GetVertexShaderConstantF();

    int SetVertexShaderConstantI();

    int GetVertexShaderConstantI();

    int SetVertexShaderConstantB();

    int GetVertexShaderConstantB();

    int SetStreamSource();

    int GetStreamSource();

    int SetStreamSourceFreq();

    int GetStreamSourceFreq();

    int SetIndices();

    int GetIndices();

    int CreatePixelShader();

    int SetPixelShader();

    int GetPixelShader();

    int SetPixelShaderConstantF();

    int GetPixelShaderConstantF();

    int SetPixelShaderConstantI();

    int GetPixelShaderConstantI();

    int SetPixelShaderConstantB();

    int GetPixelShaderConstantB();

    int DrawRectPatch();

    int DrawTriPatch();

    int DeletePatch(uint Handle);

    int CreateQuery();
}

[ComImport, Guid("B18B10CE-2649-405a-870F-95F777D4313A"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
public interface IDirect3DDevice9Ex : IDirect3DDevice9
{
    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int TestCooperativeLevel();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int GetAvailableTextureMem();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int EvictManagedResources();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int GetDirect3D([Out] out IDirect3D9 ppD3D9);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int GetDeviceCaps([In, Out] ref D3DCAPS9 pCaps);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int GetDisplayMode(uint iSwapChain, D3DDISPLAYMODE pMode);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int GetCreationParameters([In, Out] ref D3DDEVICE_CREATION_PARAMETERS pParameters);

    new int SetCursorProperties();

    new int SetCursorPosition();

    new int ShowCursor(bool bShow);

    new int CreateAdditionalSwapChain();

    new int GetSwapChain();

    new int GetNumberOfSwapChains();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int Reset([In, Out] ref D3DPRESENT_PARAMETERS pPresentationParameters);

    new int Present();

    new int GetBackBuffer();

    new int GetRasterStatus();

    new int SetDialogBoxMode();

    new int SetGammaRamp();

    new int GetGammaRamp();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int CreateTexture(int Width, int Height, int Levels, int Usage, D3DFORMAT Format, int Pool,
                      out IDirect3DTexture9 ppTexture, IntPtr pSharedHandle);

    new int CreateVolumeTexture();

    new int CreateCubeTexture();

    new int CreateVertexBuffer();

    new int CreateIndexBuffer();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int CreateRenderTarget(int width, int height, D3DFORMAT Format, D3DMULTISAMPLE_TYPE MultiSample,
                             uint MultisampleQuality, [MarshalAs(UnmanagedType.Bool)] bool Lockable, [Out] out IntPtr pSurface,
                             IntPtr pSharedSurface);

    new int CreateDepthStencilSurface();

    new int UpdateSurface();

    new int UpdateTexture();

    new int GetRenderTargetData();

    new int GetFrontBufferData();

    new int StretchRect(IntPtr pSourceSurface, DsRect pSourceRect, IDirect3DSurface9 pDestSurface, DsRect pDestRect, int Filter);

    new int ColorFill();

    new int CreateOffscreenPlainSurface();

    new int SetRenderTarget();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int GetRenderTarget([Out] out IntPtr pSurface);

    new int SetDepthStencilSurface();

    new int GetDepthStencilSurface();

    new int BeginScene();

    new int EndScene();

    new int Clear();

    new int SetTransform();

    new int GetTransform();

    new int MultiplyTransform();

    new int SetViewport();

    new int GetViewport();

    new int SetMaterial();

    new int GetMaterial();

    new int SetLight();

    new int GetLight();

    new int LightEnable();

    new int GetLightEnable();

    new int SetClipPlane();

    new int GetClipPlane();

    new int SetRenderState();

    new int GetRenderState();

    new int CreateStateBlock();

    new int BeginStateBlock();

    new int EndStateBlock();

    new int SetClipStatus();

    new int GetClipStatus();

    new int GetTexture();

    new int SetTexture();

    new int GetTextureStageState();

    new int SetTextureStageState();

    new int GetSamplerState();

    new int SetSamplerState();

    new int ValidateDevice();

    new int SetPaletteEntries();

    new int GetPaletteEntries();

    new int SetCurrentTexturePalette();

    new int GetCurrentTexturePalette();

    new int SetScissorRect();

    new int GetScissorRect();

    new int SetSoftwareVertexProcessing(bool bSoftware);

    new bool GetSoftwareVertexProcessing();

    new int SetNPatchMode(float nSegments);

    new float GetNPatchMode();

    new int DrawPrimitive();

    new int DrawIndexedPrimitive();

    new int DrawPrimitiveUP();

    new int DrawIndexedPrimitiveUP();

    new int ProcessVertices();

    new int CreateVertexDeclaration();

    new int SetVertexDeclaration();

    new int GetVertexDeclaration();

    new int SetFVF();

    new int GetFVF();

    new int CreateVertexShader();

    new int SetVertexShader();

    new int GetVertexShader();

    new int SetVertexShaderConstantF();

    new int GetVertexShaderConstantF();

    new int SetVertexShaderConstantI();

    new int GetVertexShaderConstantI();

    new int SetVertexShaderConstantB();

    new int GetVertexShaderConstantB();

    new int SetStreamSource();

    new int GetStreamSource();

    new int SetStreamSourceFreq();

    new int GetStreamSourceFreq();

    new int SetIndices();

    new int GetIndices();

    new int CreatePixelShader();

    new int SetPixelShader();

    new int GetPixelShader();

    new int SetPixelShaderConstantF();

    new int GetPixelShaderConstantF();

    new int SetPixelShaderConstantI();

    new int GetPixelShaderConstantI();

    new int SetPixelShaderConstantB();

    new int GetPixelShaderConstantB();

    new int DrawRectPatch();

    new int DrawTriPatch();

    new int DeletePatch(uint Handle);

    new int CreateQuery();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int SetConvolutionMonoKernel(int width, int height, IntPtr rows, IntPtr columns);

    int ComposeRects();

    int PresentEx();

    int GetGPUThreadPriority();

    int SetGPUThreadPriority();

    int WaitForVBlank();

    int CheckResourceResidency();

    int SetMaximumFrameLatency();

    int GetMaximumFrameLatency();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int CheckDeviceState(IntPtr hWnd);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int CreateRenderTargetEx(int width, int height, D3DFORMAT Format, D3DMULTISAMPLE_TYPE MultiSample,
                             uint MultisampleQuality, [MarshalAs(UnmanagedType.Bool)] bool Lockable, [Out] out IntPtr pSurface,
                             [In, Out] ref IntPtr pSharedSurface, uint Usage);

    /*
     STDMETHOD(SetConvolutionMonoKernel)(THIS_ UINT width,UINT height,float* rows,float* columns) PURE;
STDMETHOD(ComposeRects)(THIS_ IDirect3DSurface9* pSrc,IDirect3DSurface9* pDst,IDirect3DVertexBuffer9* pSrcRectDescs,UINT NumRects,IDirect3DVertexBuffer9* pDstRectDescs,D3DCOMPOSERECTSOP Operation,int Xoffset,int Yoffset) PURE;
STDMETHOD(PresentEx)(THIS_ CONST RECT* pSourceRect,CONST RECT* pDestRect,HWND hDestWindowOverride,CONST RGNDATA* pDirtyRegion,DWORD dwFlags) PURE;
STDMETHOD(GetGPUThreadPriority)(THIS_ INT* pPriority) PURE;
STDMETHOD(SetGPUThreadPriority)(THIS_ INT Priority) PURE;
STDMETHOD(WaitForVBlank)(THIS_ UINT iSwapChain) PURE;
STDMETHOD(CheckResourceResidency)(THIS_ IDirect3DResource9** pResourceArray,UINT32 NumResources) PURE;
STDMETHOD(SetMaximumFrameLatency)(THIS_ UINT MaxLatency) PURE;
STDMETHOD(GetMaximumFrameLatency)(THIS_ UINT* pMaxLatency) PURE;
STDMETHOD(CheckDeviceState)(THIS_ HWND hDestinationWindow) PURE;
STDMETHOD(CreateRenderTargetEx)(THIS_ UINT Width,UINT Height,D3DFORMAT Format,D3DMULTISAMPLE_TYPE MultiSample,DWORD MultisampleQuality,BOOL Lockable,IDirect3DSurface9** ppSurface,HANDLE* pSharedHandle,DWORD Usage) PURE;
STDMETHOD(CreateOffscreenPlainSurfaceEx)(THIS_ UINT Width,UINT Height,D3DFORMAT Format,D3DPOOL Pool,IDirect3DSurface9** ppSurface,HANDLE* pSharedHandle,DWORD Usage) PURE;
STDMETHOD(CreateDepthStencilSurfaceEx)(THIS_ UINT Width,UINT Height,D3DFORMAT Format,D3DMULTISAMPLE_TYPE MultiSample,DWORD MultisampleQuality,BOOL Discard,IDirect3DSurface9** ppSurface,HANDLE* pSharedHandle,DWORD Usage) PURE;
STDMETHOD(ResetEx)(THIS_ D3DPRESENT_PARAMETERS* pPresentationParameters,D3DDISPLAYMODEEX *pFullscreenDisplayMode) PURE;
STDMETHOD(GetDisplayModeEx)(THIS_ UINT iSwapChain,D3DDISPLAYMODEEX* pMode,D3DDISPLAYROTATION* pRotation) PURE;
     */
}

[ComImport, SuppressUnmanagedCodeSecurity,
Guid("0CFBAF3A-9FF6-429a-99B3-A2796AF8B89B"),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
public interface IDirect3DSurface9
{
    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void GetDevice(out IDirect3DDevice9 ppDevice);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void SetPrivateData(Guid refguid, IntPtr pData, int SizeOfData, int Flags);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void GetPrivateData(Guid refguid, IntPtr pData, out int pSizeOfData);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void FreePrivateData(Guid refguid);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int SetPriority(int PriorityNew);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetPriority();

    void PreLoad();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    D3DRESOURCETYPE GetType();

    void GetContainer(Guid riid, out object ppContainer);

    void GetDesc(out D3DSURFACE_DESC pDesc);

    void LockRect(D3DLOCKED_RECT pLockedRect, Rectangle pRect, int Flags);

    void UnlockRect();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetDC(out IntPtr phdc);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int ReleaseDC(IntPtr hdc);
}

public enum D3DRESOURCETYPE
{
    D3DRTYPE_SURFACE = 1,
    D3DRTYPE_VOLUME = 2,
    D3DRTYPE_TEXTURE = 3,
    D3DRTYPE_VOLUMETEXTURE = 4,
    D3DRTYPE_CUBETEXTURE = 5,
    D3DRTYPE_VERTEXBUFFER = 6,
    D3DRTYPE_INDEXBUFFER = 7,           //if this changes, change _D3DDEVINFO_RESOURCEMANAGER definition

    D3DRTYPE_FORCE_DWORD = 0x7fffffff
}

[StructLayout(LayoutKind.Sequential)]
public struct D3DLOCKED_RECT
{
}

[StructLayout(LayoutKind.Sequential)]
public struct D3DSURFACE_DESC
{
}

[StructLayout(LayoutKind.Sequential)]
public struct D3DDEVICE_CREATION_PARAMETERS
{
    private uint AdapterOrdinal;
    private D3DDEVTYPE DeviceType;
    private IntPtr hFocusWindow;
    private int BehaviorFlags;
}

[StructLayout(LayoutKind.Sequential)]
public struct D3DCAPS9
{
    /* Device Info */
    public D3DDEVTYPE DeviceType;
    public uint AdapterOrdinal;

    /* Caps from DX7 Draw */
    public int Caps;
    public int Caps2;
    public int Caps3;
    public int PresentationIntervals;

    /* Cursor Caps */
    public int CursorCaps;

    /* 3D Device Caps */
    public int DevCaps;

    public int PrimitiveMiscCaps;
    public int RasterCaps;
    public int ZCmpCaps;
    public int SrcBlendCaps;
    public int DestBlendCaps;
    public int AlphaCmpCaps;
    public int ShadeCaps;
    public int TextureCaps;
    public int TextureFilterCaps;          // D3DPTFILTERCAPS for IDirect3DTexture9's
    public int CubeTextureFilterCaps;      // D3DPTFILTERCAPS for IDirect3DCubeTexture9's
    public int VolumeTextureFilterCaps;    // D3DPTFILTERCAPS for IDirect3DVolumeTexture9's
    public int TextureAddressCaps;         // D3DPTADDRESSCAPS for IDirect3DTexture9's
    public int VolumeTextureAddressCaps;   // D3DPTADDRESSCAPS for IDirect3DVolumeTexture9's

    public int LineCaps;                   // D3DLINECAPS

    public int MaxTextureWidth, MaxTextureHeight;
    public int MaxVolumeExtent;

    public int MaxTextureRepeat;
    public int MaxTextureAspectRatio;
    public int MaxAnisotropy;
    private float MaxVertexW;

    private float GuardBandLeft;
    private float GuardBandTop;
    private float GuardBandRight;
    private float GuardBandBottom;

    private float ExtentsAdjust;
    public int StencilCaps;

    public int FVFCaps;
    public int TextureOpCaps;
    public int MaxTextureBlendStages;
    public int MaxSimultaneousTextures;

    public int VertexProcessingCaps;
    public int MaxActiveLights;
    public int MaxUserClipPlanes;
    public int MaxVertexBlendMatrices;
    public int MaxVertexBlendMatrixIndex;

    private float MaxPointSize;

    public int MaxPrimitiveCount;          // max number of primitives per DrawPrimitive call
    public int MaxVertexIndex;
    public int MaxStreams;
    public int MaxStreamStride;            // max stride for SetStreamSource

    public int VertexShaderVersion;
    public int MaxVertexShaderConst;       // number of vertex shader constant registers

    public int PixelShaderVersion;
    private float PixelShader1xMaxValue;      // max value storable in registers of ps.1.x shaders

    // Here are the DX9 specific ones
    public int DevCaps2;

    private float MaxNpatchTessellationLevel;
    public int Reserved5;

    public uint MasterAdapterOrdinal;       // ordinal of master adaptor for adapter group
    public uint AdapterOrdinalInGroup;      // ordinal inside the adapter group
    public uint NumberOfAdaptersInGroup;    // number of adapters in this adapter group (only if master)
    public int DeclTypes;                  // Data types, supported in vertex declarations
    public int NumSimultaneousRTs;         // Will be at least 1
    public int StretchRectFilterCaps;      // Filter caps supported by StretchRect
    private D3DVSHADERCAPS2_0 VS20Caps;
    private D3DPSHADERCAPS2_0 PS20Caps;
    public int VertexTextureFilterCaps;    // D3DPTFILTERCAPS for IDirect3DTexture9's for texture, used in vertex shaders
    public int MaxVShaderInstructionsExecuted; // maximum number of vertex shader instructions that can be executed
    public int MaxPShaderInstructionsExecuted; // maximum number of pixel shader instructions that can be executed
    public int MaxVertexShader30InstructionSlots;
    public int MaxPixelShader30InstructionSlots;
}

[StructLayout(LayoutKind.Sequential)]
public struct D3DVSHADERCAPS2_0
{
    private int Caps;
    private int DynamicFlowControlDepth;
    private int NumTemps;
    private int StaticFlowControlDepth;
}

[StructLayout(LayoutKind.Sequential)]
public struct D3DPSHADERCAPS2_0
{
    private int Caps;
    private int DynamicFlowControlDepth;
    private int NumTemps;
    private int StaticFlowControlDepth;
    private int NumInstructionSlots;
}

public enum D3DSCANLINEORDERING
{
    D3DSCANLINEORDERING_UNKNOWN = 0,
    D3DSCANLINEORDERING_PROGRESSIVE = 1,
    D3DSCANLINEORDERING_INTERLACED = 2,
}

[StructLayout(LayoutKind.Sequential)]
public struct D3DDISPLAYMODEEX
{
    public uint Size;
    public uint Width;
    public uint Height;
    public uint RefreshRate;
    public D3DFORMAT Format;
    public D3DSCANLINEORDERING ScanLineOrdering;
}

[ComImport, SuppressUnmanagedCodeSecurity,
Guid("85C31227-3DE5-4f00-9B3A-F11AC38C18B5"),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
public interface IDirect3DTexture9
{
    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void GetDevice();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void SetPrivateData(Guid refguid, IntPtr pData, int SizeOfData, int Flags);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void GetPrivateData(Guid refguid, IntPtr pData, IntPtr pSizeOfData);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void FreePrivateData(Guid refguid);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void SetPriority(int PriorityNew);

    void GetPriority();

    void PreLoad();

    void GetType();

    void SetLOD(int LODNew);

    void GetLOD();

    void GetLevelCount();

    void SetAutoGenFilterType(int FilterType);

    int GetAutoGenFilterType();

    void GenerateMipSubLevels();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void GetLevelDesc(int Level, IntPtr pDesc);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetSurfaceLevel(int Level, out IDirect3DSurface9 ppSurfaceLevel);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void LockRect(int Level, ref D3DLOCKED_RECT pLockedRect, RECT pRect, int Flags);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void UnlockRect(int Level);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    void AddDirtyRect(RECT pDirtyRect);
}

[ComImport, SuppressUnmanagedCodeSecurity,
Guid("02177241-69FC-400C-8FF1-93A44DF6861D"),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
public interface IDirect3D9Ex : IDirect3D9
{
    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int RegisterSoftwareDevice([In, Out] IntPtr pInitializeFunction);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int GetAdapterCount();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int GetAdapterIdentifier(uint Adapter, uint Flags, uint pIdentifier);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new uint GetAdapterModeCount(uint Adapter, D3DFORMAT Format);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int EnumAdapterModes(uint Adapter, D3DFORMAT Format, uint Mode, [Out] out D3DDISPLAYMODE pMode);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int GetAdapterDisplayMode(ushort Adapter, [Out] out D3DFORMAT Format);

    #region Method Placeholders

    [PreserveSig]
    new int CheckDeviceType();

    [PreserveSig]
    new int CheckDeviceFormat();

    [PreserveSig]
    new int CheckDeviceMultiSampleType();

    [PreserveSig]
    new int CheckDepthStencilMatch();

    [PreserveSig]
    new int CheckDeviceFormatConversion();

    [PreserveSig]
    new int GetDeviceCaps();

    #endregion Method Placeholders

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new IntPtr GetAdapterMonitor(uint Adapter);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    new int CreateDevice(int Adapter,
                      D3DDEVTYPE DeviceType,
                      IntPtr hFocusWindow,
                      CreateFlags BehaviorFlags,
                      [In, Out]
                      ref D3DPRESENT_PARAMETERS pPresentationParameters,
                      [Out] out IntPtr ppReturnedDeviceInterface);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    uint GetAdapterModeCountEx();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int EnumAdapterModesEx();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetAdapterDisplayModeEx();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int CreateDeviceEx(int Adapter,
                      D3DDEVTYPE DeviceType,
                      IntPtr hFocusWindow,
                      CreateFlags BehaviorFlags,
                      [In, Out]
                      ref D3DPRESENT_PARAMETERS pPresentationParameters,
                      [In, Out]
                      IntPtr pFullscreenDisplayMode,
                      [Out] out IntPtr ppReturnedDeviceInterface);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetAdapterLUID();
}

[ComImport, SuppressUnmanagedCodeSecurity,
Guid("81BDCBCA-64D4-426d-AE8D-AD0147F4275C"),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
public interface IDirect3D9
{
    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int RegisterSoftwareDevice([In, Out] IntPtr pInitializeFunction);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetAdapterCount();

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetAdapterIdentifier(uint Adapter, uint Flags, uint pIdentifier);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    uint GetAdapterModeCount(uint Adapter, D3DFORMAT Format);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int EnumAdapterModes(uint Adapter, D3DFORMAT Format, uint Mode, [Out] out D3DDISPLAYMODE pMode);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int GetAdapterDisplayMode(ushort Adapter, [Out] out D3DFORMAT Format);

    #region Method Placeholders

    [PreserveSig]
    int CheckDeviceType();

    [PreserveSig]
    int CheckDeviceFormat();

    [PreserveSig]
    int CheckDeviceMultiSampleType();

    [PreserveSig]
    int CheckDepthStencilMatch();

    [PreserveSig]
    int CheckDeviceFormatConversion();

    [PreserveSig]
    int GetDeviceCaps();

    #endregion Method Placeholders

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    IntPtr GetAdapterMonitor(uint Adapter);

    [PreserveSig, SuppressUnmanagedCodeSecurity]
    int CreateDevice(int Adapter,
                      D3DDEVTYPE DeviceType,
                      IntPtr hFocusWindow,
                      CreateFlags BehaviorFlags,
                      [In, Out]
                      ref D3DPRESENT_PARAMETERS pPresentationParameters,
                      [Out] out IntPtr ppReturnedDeviceInterface);
}

[StructLayout(LayoutKind.Sequential)]
public struct D3DDISPLAYMODE
{
    public uint Width;
    public uint Height;
    public uint RefreshRate;
    public D3DFORMAT Format;
}

[Flags]
public enum CreateFlags
{
    D3DCREATE_FPU_PRESERVE = 0x00000002,
    D3DCREATE_MULTITHREADED = 0x00000004,
    D3DCREATE_PUREDEVICE = 0x00000010,
    D3DCREATE_SOFTWARE_VERTEXPROCESSING = 0x00000020,
    D3DCREATE_HARDWARE_VERTEXPROCESSING = 0x00000040,
    D3DCREATE_MIXED_VERTEXPROCESSING = 0x00000080,
    D3DCREATE_DISABLE_DRIVER_MANAGEMENT = 0x00000100,
    D3DCREATE_ADAPTERGROUP_DEVICE = 0x00000200,
    D3DCREATE_DISABLE_DRIVER_MANAGEMENT_EX = 0x00000400
}

[Flags]
public enum D3DDEVTYPE
{
    D3DDEVTYPE_HAL = 1,
    D3DDEVTYPE_REF = 2,
    D3DDEVTYPE_SW = 3,
    D3DDEVTYPE_NULLREF = 4,
}

[Flags]
public enum D3DFORMAT
{
    D3DFMT_UNKNOWN = 0,
    D3DFMT_R8G8B8 = 20,
    D3DFMT_A8R8G8B8 = 21,
    D3DFMT_X8R8G8B8 = 22,
    D3DFMT_R5G6B5 = 23,
    D3DFMT_X1R5G5B5 = 24,
    D3DFMT_A1R5G5B5 = 25,
    D3DFMT_A4R4G4B4 = 26,
    D3DFMT_R3G3B2 = 27,
    D3DFMT_A8 = 28,
    D3DFMT_A8R3G3B2 = 29,
    D3DFMT_X4R4G4B4 = 30,
    D3DFMT_A2B10G10R10 = 31,
    D3DFMT_A8B8G8R8 = 32,
    D3DFMT_X8B8G8R8 = 33,
    D3DFMT_G16R16 = 34,
    D3DFMT_A2R10G10B10 = 35,
    D3DFMT_A16B16G16R16 = 36,
    D3DFMT_A8P8 = 40,
    D3DFMT_P8 = 41,
    D3DFMT_L8 = 50,
    D3DFMT_A8L8 = 51,
    D3DFMT_A4L4 = 52,
    D3DFMT_V8U8 = 60,
    D3DFMT_L6V5U5 = 61,
    D3DFMT_X8L8V8U8 = 62,
    D3DFMT_Q8W8V8U8 = 63,
    D3DFMT_V16U16 = 64,
    D3DFMT_A2W10V10U10 = 67,
    D3DFMT_D16_LOCKABLE = 70,
    D3DFMT_D32 = 71,
    D3DFMT_D15S1 = 73,
    D3DFMT_D24S8 = 75,
    D3DFMT_D24X8 = 77,
    D3DFMT_D24X4S4 = 79,
    D3DFMT_D16 = 80,
    D3DFMT_D32F_LOCKABLE = 82,
    D3DFMT_D24FS8 = 83,
    /* Z-Stencil formats valid for CPU access */
    D3DFMT_D32_LOCKABLE = 84,
    D3DFMT_S8_LOCKABLE = 85,
    D3DFMT_L16 = 81,
    D3DFMT_VERTEXDATA = 100,
    D3DFMT_INDEX16 = 101,
    D3DFMT_INDEX32 = 102,
    D3DFMT_Q16W16V16U16 = 110,

    // Floating point surface formats
    // s10e5 formats (16-bits per channel)
    D3DFMT_R16F = 111,

    D3DFMT_G16R16F = 112,
    D3DFMT_A16B16G16R16F = 113,

    // IEEE s23e8 formats (32-bits per channel)
    D3DFMT_R32F = 114,

    D3DFMT_G32R32F = 115,
    D3DFMT_A32B32G32R32F = 116,
    D3DFMT_CxV8U8 = 117,

    // Monochrome 1 bit per pixel format
    D3DFMT_A1 = 118,

    // Binary format indicating that the data has no inherent type
    D3DFMT_BINARYBUFFER = 199,
}

[Flags]
public enum D3DSWAPEFFECT
{
    D3DSWAPEFFECT_DISCARD = 1,
    D3DSWAPEFFECT_FLIP = 2,
    D3DSWAPEFFECT_COPY = 3,
}

[Flags]
public enum D3DMULTISAMPLE_TYPE
{
    D3DMULTISAMPLE_NONE = 0,
    D3DMULTISAMPLE_NONMASKABLE = 1,
    D3DMULTISAMPLE_2_SAMPLES = 2,
    D3DMULTISAMPLE_3_SAMPLES = 3,
    D3DMULTISAMPLE_4_SAMPLES = 4,
    D3DMULTISAMPLE_5_SAMPLES = 5,
    D3DMULTISAMPLE_6_SAMPLES = 6,
    D3DMULTISAMPLE_7_SAMPLES = 7,
    D3DMULTISAMPLE_8_SAMPLES = 8,
    D3DMULTISAMPLE_9_SAMPLES = 9,
    D3DMULTISAMPLE_10_SAMPLES = 10,
    D3DMULTISAMPLE_11_SAMPLES = 11,
    D3DMULTISAMPLE_12_SAMPLES = 12,
    D3DMULTISAMPLE_13_SAMPLES = 13,
    D3DMULTISAMPLE_14_SAMPLES = 14,
    D3DMULTISAMPLE_15_SAMPLES = 15,
    D3DMULTISAMPLE_16_SAMPLES = 16,
}

[Flags]
public enum D3DPRESENTFLAG
{
    D3DPRESENTFLAG_LOCKABLE_BACKBUFFER = 0x00000001,
    D3DPRESENTFLAG_DISCARD_DEPTHSTENCIL = 0x00000002,
    D3DPRESENTFLAG_DEVICECLIP = 0x00000004,
    D3DPRESENTFLAG_VIDEO = 0x00000010
}

[StructLayout(LayoutKind.Sequential)]
public struct D3DPRESENT_PARAMETERS
{
    public uint BackBufferWidth;
    public uint BackBufferHeight;
    public D3DFORMAT BackBufferFormat;
    public uint BackBufferCount;
    public D3DMULTISAMPLE_TYPE MultiSampleType;
    public int MultiSampleQuality;
    public D3DSWAPEFFECT SwapEffect;
    public IntPtr hDeviceWindow;
    public int Windowed;
    public int EnableAutoDepthStencil;
    public D3DFORMAT AutoDepthStencilFormat;
    public int Flags;
    /* FullScreen_RefreshRateInHz must be zero for Windowed mode */
    public uint FullScreen_RefreshRateInHz;
    public uint PresentationInterval;
}

public class Direct3D
{
    [DllImport("d3d9.dll", EntryPoint = "Direct3DCreate9", CallingConvention = CallingConvention.StdCall),
    SuppressUnmanagedCodeSecurity]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern IDirect3D9 Direct3DCreate9(ushort SDKVersion);

    [DllImport("d3d9.dll", EntryPoint = "Direct3DCreate9Ex", CallingConvention = CallingConvention.StdCall),
    SuppressUnmanagedCodeSecurity]
    public static extern int Direct3DCreate9Ex(ushort SDKVersion, [Out] out IDirect3D9Ex ex);
}
