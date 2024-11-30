using DirectShowLib;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

// Code of MediaPortal (www.team-mediaportal.com)

namespace WPFMediaKit.DirectShow.MediaPlayers;

/// <summary>
///  Provides collections of devices and compression codecs
///  installed on the system.
/// </summary>
/// <example>
///  Devices and compression codecs are implemented in DirectShow
///  as filters, see the <see cref="Filter"/> class for more
///  information. To list the available video devices:
///  <code><div style="background-color:whitesmoke;">
///   Filters filters = new Filters();
///   foreach ( Filter f in filters.VideoInputDevices )
///   {
///		Debug.WriteLine( f.Name );
///   }
///  </div></code>
///  <seealso cref="Filter"/>
/// </example>
public class Filters
{
    // ------------------ Public Properties --------------------

    /// <summary> Collection of available video capture devices. </summary>
    public static FilterCollection VideoInputDevices;

    /// <summary> Collection of available audio capture devices. </summary>
    public static FilterCollection AudioInputDevices;

    /// <summary> Collection of available video compressors. </summary>
    public static FilterCollection VideoCompressors;

    /// <summary> Collection of available audio compressors. </summary>
    public static FilterCollection AudioCompressors;

    public static FilterCollection LegacyFilters;
    public static FilterCollection AudioRenderers;
    public static FilterCollection WDMEncoders;
    public static FilterCollection WDMcrossbars;
    public static FilterCollection WDMTVTuners;
    public static FilterCollection BDAReceivers;
    public static FilterCollection AllFilters;

    static Filters()
    {
        VideoInputDevices = new FilterCollection(FilterCategory.VideoInputDevice, true);
        AudioInputDevices = new FilterCollection(FilterCategory.AudioInputDevice, true);
        VideoCompressors = new FilterCollection(FilterCategory.VideoCompressorCategory, true);
        AudioCompressors = new FilterCollection(FilterCategory.AudioCompressorCategory, true);
        LegacyFilters = new FilterCollection(FilterCategory.LegacyAmFilterCategory, true);
        AudioRenderers = new FilterCollection(FilterCategory.AudioRendererDevice, true);
        WDMEncoders = new FilterCollection(FilterCategory.AM_KSEncoder, true);
        WDMcrossbars = new FilterCollection(FilterCategory.AM_KSCrossBar, true);
        WDMTVTuners = new FilterCollection(FilterCategory.AM_KSTvTuner, true);
        BDAReceivers = new FilterCollection(FilterCategory.AM_KS_BDA_RECEIVER_COMPONENT, true);
        AllFilters = new FilterCollection(FilterCategory.ActiveMovieCategory, true);
    }
}

/// <summary>
///	 A collection of Filter objects (DirectShow filters).
///	 This is used by the <see cref="Capture"/> class to provide
///	 lists of capture devices and compression filters. This class
///	 cannot be created directly.
/// </summary>
public class FilterCollection : CollectionBase
{
    /// <summary> Populate the collection with a list of filters from a particular category. </summary>
    public FilterCollection(Guid category)
    {
        getFilters(category);
    }

    /// <summary> Populate the collection with a list of filters from a particular category. </summary>
    public FilterCollection(Guid category, bool resolveNames)
    {
        getFilters(category);
        foreach (Filter f in InnerList)
        {
            f.ResolveName();
        }
    }

    /// <summary> Populate the InnerList with a list of filters from a particular category </summary>
    protected void getFilters(Guid category)
    {
        int hr;
        object comObj = null;
        ICreateDevEnum enumDev = null;
        IEnumMoniker enumMon = null;
        IMoniker[] mon = new IMoniker[1];

        try
        {
            // Get the system device enumerator
            Type srvType = Type.GetTypeFromCLSID(ClassId.SystemDeviceEnum);
            if (srvType == null)
            {
                throw new NotImplementedException("System Device Enumerator");
            }
            comObj = Activator.CreateInstance(srvType);
            enumDev = (ICreateDevEnum)comObj;

            // Create an enumerator to find filters in category
            hr = enumDev.CreateClassEnumerator(category, out enumMon, 0);
            if (hr != 0)
            {
                return; //throw new NotSupportedException( "No devices of the category" );
            }
            // Loop through the enumerator
            IntPtr f = IntPtr.Zero;
            do
            {
                // Next filter
                hr = enumMon.Next(1, mon, f);
                if ((hr != 0) || (mon[0] == null))
                {
                    break;
                }

                // Add the filter
                Filter filter = new Filter(mon[0]);
                InnerList.Add(filter);

                // Release resources
                Marshal.ReleaseComObject(mon[0]);
                mon[0] = null;
            } while (true);

            // Sort
            //InnerList.Sort();
        }
        finally
        {
            enumDev = null;
            if (mon[0] != null)
            {
                Marshal.ReleaseComObject(mon[0]);
            }
            mon[0] = null;
            if (enumMon != null)
            {
                Marshal.ReleaseComObject(enumMon);
            }
            enumMon = null;
            if (comObj != null)
            {
                Marshal.ReleaseComObject(comObj);
            }
            comObj = null;
        }
    }

    /// <summary> Get the filter at the specified index. </summary>
    public Filter this[int index]
    {
        get
        {
            if (index >= InnerList.Count)
            {
                return null;
            }
            return ((Filter)InnerList[index]);
        }
    }
}

/// <summary>
///  Represents a DirectShow filter (e.g. video capture device,
///  compression codec).
/// </summary>
/// <remarks>
///  To save a chosen filer for later recall
///  save the MonikerString property on the filter:
///  <code><div style="background-color:whitesmoke;">
///   string savedMonikerString = myFilter.MonikerString;
///  </div></code>
///
///  To recall the filter create a new Filter class and pass the
///  string to the constructor:
///  <code><div style="background-color:whitesmoke;">
///   Filter mySelectedFilter = new Filter( savedMonikerString );
///  </div></code>
/// </remarks>
public class Filter : IComparable
{
    /// <summary> Human-readable name of the filter </summary>
    private string _name = string.Empty;

    private bool _nameResolved = false;

    /// <summary> Unique string referencing this filter. This string can be used to recreate this filter. </summary>
    public string MonikerString;

    /// <summary> getAnyMoniker take very long time, so use a cached value </summary>
    private static IMoniker[] mon = null;

    /// <summary> Create a new filter from its moniker string. </summary>
    public Filter(string monikerString)
    {
        MonikerString = monikerString;
    }

    /// <summary> Create a new filter from its moniker </summary>
    internal Filter(IMoniker moniker)
    {
        MonikerString = getMonikerString(moniker);
    }

    public string Name
    {
        get
        {
            if (_nameResolved)
            {
                return _name;
            }
            _name = getName(MonikerString);
            return _name;
        }
    }

    public Guid CLSID { get; protected set; }

    public void ResolveName()
    {
        if (_nameResolved)
        {
            return;
        }
        _name = getName(MonikerString);
    }

    /// <summary> Retrieve the a moniker's display name (i.e. it's unique string) </summary>
    protected string getMonikerString(IMoniker moniker)
    {
        string s;
        moniker.GetDisplayName(null, null, out s);
        return (s);
    }

    /// <summary> Retrieve the human-readable name of the filter </summary>
    protected string getName(IMoniker moniker)
    {
        object bagObj = null;
        IPropertyBag bag = null;
        try
        {
            Guid bagId = typeof(IPropertyBag).GUID;
            moniker.BindToStorage(null, null, ref bagId, out bagObj);
            bag = (IPropertyBag)bagObj;
            object val = "";
            int hr = bag.Read("FriendlyName", out val, null);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            string ret = val as string;
            if ((ret == null) || (ret.Length < 1))
            {
                throw new NotImplementedException("Device FriendlyName");
            }

            hr = bag.Read("CLSID", out val, null);
            if (hr == 0)
            {
                CLSID = new Guid(val.ToString());
            }

            return (ret);
        }
        catch (Exception)
        {
            return ("");
        }
        finally
        {
            bag = null;
            if (bagObj != null)
            {
                Marshal.ReleaseComObject(bagObj);
            }
            bagObj = null;

            _nameResolved = true;
        }
    }

    /// <summary> Get a moniker's human-readable name based on a moniker string. </summary>
    protected string getName(string monikerString)
    {
        IMoniker parser = null;
        IMoniker moniker = null;
        try
        {
            parser = getAnyMoniker();
            int eaten;
            parser.ParseDisplayName(null, null, monikerString, out eaten, out moniker);
            return (getName(parser));
        }
        finally
        {
            if (moniker != null)
            {
                Marshal.ReleaseComObject(moniker);
            }
            moniker = null;
            _nameResolved = true;
        }
    }

    /// <summary>
    ///  This method gets a System.Runtime.InteropServices.ComTypes.IMoniker object.
    ///
    ///  HACK: The only way to create a System.Runtime.InteropServices.ComTypes.IMoniker from a moniker
    ///  string is to use System.Runtime.InteropServices.ComTypes.IMoniker.ParseDisplayName(). So I
    ///  need ANY System.Runtime.InteropServices.ComTypes.IMoniker object so that I can call
    ///  ParseDisplayName(). Does anyone have a better solution?
    ///
    ///  This assumes there is at least one video compressor filter
    ///  installed on the system.
    /// </summary>
    protected IMoniker getAnyMoniker()
    {
        Guid category = FilterCategory.VideoCompressorCategory;
        int hr;
        object comObj = null;
        ICreateDevEnum enumDev = null;
        IEnumMoniker enumMon = null;

        if (mon != null)
        {
            return mon[0];
        }

        mon = new IMoniker[1];

        try
        {
            // Get the system device enumerator
            Type srvType = Type.GetTypeFromCLSID(ClassId.SystemDeviceEnum);
            if (srvType == null)
            {
                throw new NotImplementedException("System Device Enumerator");
            }
            comObj = Activator.CreateInstance(srvType);
            enumDev = (ICreateDevEnum)comObj;

            // Create an enumerator to find filters in category
            hr = enumDev.CreateClassEnumerator(category, out enumMon, 0);
            if (hr != 0)
            {
                throw new NotSupportedException("No devices of the category");
            }

            // Get first filter
            IntPtr f = IntPtr.Zero;
            hr = enumMon.Next(1, mon, f);
            if ((hr != 0))
            {
                mon[0] = null;
            }

            return (mon[0]);
        }
        finally
        {
            enumDev = null;
            if (enumMon != null)
            {
                Marshal.ReleaseComObject(enumMon);
            }
            enumMon = null;
            if (comObj != null)
            {
                Marshal.ReleaseComObject(comObj);
            }
            comObj = null;
        }
    }

    /// <summary>
    ///  Compares the current instance with another object of
    ///  the same type.
    /// </summary>
    public int CompareTo(object obj)
    {
        if (obj == null)
        {
            return (1);
        }
        Filter f = (Filter)obj;
        return (this.Name.CompareTo(f.Name));
    }
}

public class FilterCategory // uuids.h  :  CLSID_*
{
    /// <summary> CLSID_AudioRendererCategory, audio render category </summary>
    public static readonly Guid AudioRendererDevice = new Guid(0xe0f158e1, 0xcb04, 0x11d0, 0xbd, 0x4e, 0x0, 0xa0, 0xc9,
                                                               0x11, 0xce, 0x86);

    /// <summary> CLSID_AudioInputDeviceCategory, audio capture category </summary>
    public static readonly Guid AudioInputDevice = new Guid(0x33d9a762, 0x90c8, 0x11d0, 0xbd, 0x43, 0x00, 0xa0, 0xc9,
                                                            0x11, 0xce, 0x86);

    /// <summary> CLSID_VideoInputDeviceCategory, video capture category </summary>
    public static readonly Guid VideoInputDevice = new Guid(0x860BB310, 0x5D01, 0x11d0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9,
                                                            0x11, 0xCE, 0x86);

    /// <summary> CLSID_VideoCompressorCategory, Video compressor category </summary>
    public static readonly Guid VideoCompressorCategory = new Guid(0x33d9a760, 0x90c8, 0x11d0, 0xbd, 0x43, 0x0, 0xa0,
                                                                   0xc9, 0x11, 0xce, 0x86);

    public static readonly Guid AM_KSTvTuner = new Guid(0xA799A800, 0xA46D, 0x11D0, 0xA1, 0x8C, 0x00, 0xA0, 0x24, 0x01,
                                                        0xDC, 0xD4);

    public static readonly Guid AM_KS_BDA_RECEIVER_COMPONENT = new Guid(0xFD0A5AF4, 0xB41D, 0x11d2, 0x9c, 0x95, 0x00,
                                                                        0xc0, 0x4f, 0x79, 0x71, 0xe0);

    public static readonly Guid AM_KSCrossBar = new Guid(0xA799A801, 0xA46D, 0x11D0, 0xA1, 0x8C, 0x00, 0xA0, 0x24, 0x01,
                                                         0xDC, 0xD4);

    public static readonly Guid AM_KSEncoder = new Guid(0x19689bf6, 0xc384, 0x48fd, 0xad, 0x51, 0x90, 0xe5, 0x8c, 0x79,
                                                        0xf7, 0xb);

    /// <summary> CLSID_AudioCompressorCategory, audio compressor category </summary>
    public static readonly Guid AudioCompressorCategory = new Guid(0x33d9a761, 0x90c8, 0x11d0, 0xbd, 0x43, 0x0, 0xa0,
                                                                   0xc9, 0x11, 0xce, 0x86);

    /// <summary> CLSID_LegacyAmFilterCategory, legacy filters </summary>
    public static readonly Guid LegacyAmFilterCategory = new Guid(0x083863F1, 0x70DE, 0x11d0, 0xBD, 0x40, 0x00, 0xA0,
                                                                  0xC9, 0x11, 0xCE, 0x86);

    /// <summary>
    /// #MW# CLSID_ActiveMovieCategory, a superset of all the available filters
    /// </summary>
    public static readonly Guid ActiveMovieCategory = new Guid(0xda4e3da0, 0xd07d, 0x11d0, 0xbd, 0x50, 0x0, 0xa0, 0xc9,
                                                               0x11, 0xce, 0x86);

    public static readonly Guid IID_IKsPropertySet = new Guid(0x31efac30, 0x515c, 0x11d0, 0xa9, 0xaa, 0x00, 0xaa, 0x00,
                                                              0x61, 0xbe, 0x93);
}
