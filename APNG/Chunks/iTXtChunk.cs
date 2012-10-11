using System.IO;
using System.Text;

namespace LibAPNG
{
    public class iTXtChunk : Chunk, ITextChunk
    {
        public iTXtChunk(byte[] bytes)
            : base(bytes)
        {
        }

        public iTXtChunk(MemoryStream ms)
            : base(ms)
        {
        }

        public iTXtChunk(Chunk chunk)
            : base(chunk)
        {
        }

        public string Keyword { get; set; }

        public string Text { get; set; }

        protected override void ParseData(MemoryStream ms)
        {
            Encoding iso = Encoding.UTF8;
            string combined = new StreamReader(ms, iso).ReadToEnd();
            string[] parts = combined.Split('\0');

            if (parts.Length != 2)
            {
                // Warn about invalid chunk?
            }

            if (parts.Length > 0)
            {
                Keyword = parts[0];
            }

            if (parts.Length > 1)
            {
                Text = parts[1];
            }
        }
    }
}