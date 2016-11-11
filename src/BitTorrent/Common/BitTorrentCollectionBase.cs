using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace System.Net.BitTorrent.Common
{
#if IS_CORECLR
    public class BitTorrentCollection<T> : List<T>
#else
    public class BitTorrentCollection<T> : List<T>, ICloneable
#endif
    {
        public BitTorrentCollection()
            : base()
        {

        }

        public BitTorrentCollection(IEnumerable<T> collection)
            : base(collection)
        {

        }

        public BitTorrentCollection(int capacity)
            : base(capacity)
        {

        }

#if IS_CORECLR
#else
        object ICloneable.Clone()
        {
            return Clone();
        }
#endif

        public BitTorrentCollection<T> Clone()
        {
            return new BitTorrentCollection<T>(this);
        }

        public T Dequeue()
        {
            T result = this[0];
            RemoveAt(0);
            return result;
        }
    }
}
