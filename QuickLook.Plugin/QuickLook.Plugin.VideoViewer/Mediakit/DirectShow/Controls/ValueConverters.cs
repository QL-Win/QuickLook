using System;
using System.Globalization;
using System.Windows.Data;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace WPFMediaKit.DirectShow.Controls;

/// <summary>
/// Converter used to convert MediaTime format to seconds
/// if the current MediaSeekingElement is using MediaTime
/// format.  If the MediaSeekingElement is not using
/// MediaTime, then the converter will return the input
/// passed as the MediaTime.
/// </summary>
public class MediaTimeToSeconds : IMultiValueConverter
{
    #region IMultiValueConverter Members

    /// <summary>
    /// Converts MediaTime to seconds
    /// </summary>
    /// <param name="values">There are two parameters to pass.  The first is a MediaSeekingElement,
    /// the second is a long of the MediaTime</param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns>The time in seconds if MediaTime is being used, else the second input parameter is passed back</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        /* We only take two arguments */
        if (values.Length != 2)
            return 0;

        /* We only support a MediaSeekingElement */
        var mediaPlayer = values[0] as MediaSeekingElement;

        /* If you didn't send us a MediaSeekingElement, then bugger off */
        if (mediaPlayer == null)
            return 0;

        /* Our second param should be a long */
        long value;

        try
        {
            value = (long)values[1];
        }
        catch (Exception)
        {
            /* Just return what was given to us */
            return values[1];
        }

        /* Only convert if we are dealing with MediaTime */
        if (mediaPlayer.CurrentPositionFormat == MediaPositionFormat.MediaTime)
        {
            double seconds = (double)value / MediaPlayerBase.DSHOW_ONE_SECOND_UNIT;
            return seconds;
        }

        return value;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    #endregion IMultiValueConverter Members
}
