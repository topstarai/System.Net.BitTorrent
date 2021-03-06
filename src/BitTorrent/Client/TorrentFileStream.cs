using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.BitTorrent.Common;

namespace System.Net.BitTorrent.Client
{
    internal class TorrentFileStream : FileStream
    {
        TorrentFile file;

        public TorrentFile File
        {
            get { return file; }
        }

        public string Path
        {
            get { return file.FullPath; }
        }


        public TorrentFileStream(TorrentFile file, FileMode mode, FileAccess access, FileShare share)
            : base(file.FullPath, mode, access, share, 1)
        {
            this.file = file;
        }
    }
}
