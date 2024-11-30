using System;
using System.Runtime.Serialization;

namespace WPFMediaKit;

public class WPFMediaKitException : Exception
{
    public WPFMediaKitException()
        : base()
    {
    }

    public WPFMediaKitException(string message)
        : base(message)
    {
    }

    public WPFMediaKitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected WPFMediaKitException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
