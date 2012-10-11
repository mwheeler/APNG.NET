using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace LibAPNG
{
    public class zTXtChunk : Chunk, ITextChunk
    {
        public zTXtChunk(byte[] bytes)
            : base(bytes)
        {
        }

        public zTXtChunk(MemoryStream ms)
            : base(ms)
        {
        }

        public zTXtChunk(Chunk chunk)
            : base(chunk)
        {
        }

        public string Keyword { get; set; }

        public string Text { get; set; }

        protected override void ParseData(MemoryStream ms)
        {
            Encoding iso = Encoding.GetEncoding("ISO-8859-1");
            using(StreamReader reader = new StreamReader(ms, iso))
            {
                char[] buffer = new char[80];
                reader.Read(buffer, 0, 80);
                Keyword = new String(buffer);
            }

            int compression_method = ms.ReadByte();

            switch (compression_method)
            {
                case 0: // zlib Inflate/Deflate
                    DeflateStream stream = new DeflateStream(ms, CompressionMode.Decompress);
                    using(StreamReader reader = new StreamReader(stream, iso))
                    {
                        Text = reader.ReadToEnd();
                    }
                    break;

                default:
                    // Warn about unknown compression method!
                    break;
            }
        }
    }
}