using DirectShowLib;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WPFMediaKit.DirectShow.MediaPlayers;

public class PropertyPageHelper : IDisposable
{
    private const string NO_PROPERTY_PAGE_FOUND = "No property page found.";

    [DllImport("olepro32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int OleCreatePropertyFrame(IntPtr hwndOwner,
                                                     int x,
                                                     int y,
                                                     string lpszCaption,
                                                     int cObjects,
                                                     [In, MarshalAs(UnmanagedType.Interface)] ref object ppUnk,
                                                     int cPages,
                                                     IntPtr pPageClsId,
                                                     int lcid,
                                                     int dwReserved,
                                                     IntPtr pvReserved);

    private ISpecifyPropertyPages m_specifyPropertyPages;

    public PropertyPageHelper(IBaseFilter filter)
    {
        m_specifyPropertyPages = filter as ISpecifyPropertyPages;
    }

    public PropertyPageHelper(DsDevice dev)
    {
        try
        {
            object source;
            var id = typeof(IBaseFilter).GUID;
            dev.Mon.BindToObject(null, null, ref id, out source);
            if (source != null)
            {
                var filter = (IBaseFilter)source;
                m_specifyPropertyPages = filter as ISpecifyPropertyPages;
            }
        }
        catch
        {
            MessageBox.Show(NO_PROPERTY_PAGE_FOUND);
        }
    }

    #region IDisposable Members

    public void Dispose()
    {
        m_specifyPropertyPages = null;
    }

    #endregion IDisposable Members

    public void Show(IntPtr hWnd)
    {
        var cauuid = new DsCAUUID();
        try
        {
            int hr = m_specifyPropertyPages.GetPages(out cauuid);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            object objRef = m_specifyPropertyPages;
            hr = OleCreatePropertyFrame(hWnd,
                                        30,
                                        30,
                                        null,
                                        1,
                                        ref objRef,
                                        cauuid.cElems,
                                        cauuid.pElems,
                                        0,
                                        0,
                                        IntPtr.Zero);

            DsError.ThrowExceptionForHR(hr);
        }
        catch (Exception)
        {
            MessageBox.Show(NO_PROPERTY_PAGE_FOUND);
        }
        finally
        {
            if (cauuid.pElems != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(cauuid.pElems);
            }
        }
    }

    public void Show(Control owner)
    {
        Show(owner.Handle);
    }
}
