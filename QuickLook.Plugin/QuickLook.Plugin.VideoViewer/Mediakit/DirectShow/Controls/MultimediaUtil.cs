using DirectShowLib;
using System;
using System.Linq;

namespace WPFMediaKit.DirectShow.Controls;

public class MultimediaUtil
{
    #region Audio Renderer Methods

    /// <summary>
    /// The private cache of the audio renderer names
    /// </summary>
    private static string[] m_audioRendererNames;

    /// <summary>
    /// An array of audio renderer device names
    /// on the current system
    /// </summary>
    public static string[] AudioRendererNames
    {
        get
        {
            if (m_audioRendererNames == null)
            {
                m_audioRendererNames = (from a in GetDevices(FilterCategory.AudioRendererCategory)
                                        select a.Name).ToArray();
            }
            return m_audioRendererNames;
        }
    }

    #endregion Audio Renderer Methods

    #region Video Input Devices

    /// <summary>
    /// The private cache of the video input names
    /// </summary>
    private static string[] m_videoInputNames;

    /// <summary>
    /// An array of video input device names
    /// on the current system
    /// </summary>
    public static string[] VideoInputNames
    {
        get
        {
            if (m_videoInputNames == null)
            {
                m_videoInputNames = (from d in VideoInputDevices
                                     select d.Name).ToArray();
            }
            return m_videoInputNames;
        }
    }

    #endregion Video Input Devices

    private static DsDevice[] GetDevices(Guid filterCategory)
    {
        return (from d in DsDevice.GetDevicesOfCat(filterCategory)
                select d).ToArray();
    }

    public static DsDevice[] VideoInputDevices
    {
        get
        {
            if (m_videoInputDevices == null)
            {
                m_videoInputDevices = GetDevices(FilterCategory.VideoInputDevice);
            }
            return m_videoInputDevices;
        }
    }

    private static DsDevice[] m_videoInputDevices;

    public static string[] VideoInputsDevicePaths
    {
        get
        {
            if (m_videoInputsDevicePaths == null)
            {
                m_videoInputsDevicePaths = (from d in VideoInputDevices
                                            select d.DevicePath).ToArray();
            }
            return m_videoInputsDevicePaths;
        }
    }

    private static string[] m_videoInputsDevicePaths;
}
