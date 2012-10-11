using System.IO;
using System.Text;

namespace LibAPNG
{
    public class tEXtChunk : Chunk, ITextChunk
    {
        public tEXtChunk(byte[] bytes)
            : base(bytes)
        {
        }

        public tEXtChunk(MemoryStream ms)
            : base(ms)
        {
        }

        public tEXtChunk(Chunk chunk)
            : base(chunk)
        {
        }

        public string Keyword { get; set; }

        public string Text { get; set; }

        protected override void ParseData(MemoryStream ms)
        {
            Encoding iso = Encoding.GetEncoding("ISO-8859-1");
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