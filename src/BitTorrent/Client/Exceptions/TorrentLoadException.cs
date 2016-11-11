using System;
using System.Text;
using System.Net.BitTorrent.Common;

namespace System.Net.BitTorrent.Client
{
    public class TorrentLoadException : TorrentException
    {

        public TorrentLoadException()
            : base()
        {
        }


        public TorrentLoadException(string message)
            : base(message)
        {
        }


        public TorrentLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


#if IS_CORECLR
#else
        public TorrentLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}