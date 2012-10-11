﻿using System.Collections.Generic;
using System.IO;

namespace LibAPNG
{
    /// <summary>
    /// Describe a single frame.
    /// </summary>
    public class Frame
    {
        public static byte[] Signature = new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A};

        private List<IDATChunk> idatChunks = new List<IDATChunk>();
        private ITextChunk[] textChunks = new ITextChunk[0];

        /// <summary>
        /// Gets or Sets the acTL chunk
        /// </summary>
        public IHDRChunk IHDRChunk { get; set; }

        /// <summary>
        /// Gets or Sets the fcTL chunk
        /// </summary>
        public fcTLChunk fcTLChunk { get; set; }

        /// <summary>
        /// Gets or Sets the IEND chunk
        /// </summary>
        public IENDChunk IENDChunk { get; set; }

        /// <summary>
        /// Gets or Sets the IDAT chunks
        /// </summary>
        public List<IDATChunk> IDATChunks
        {
            get { return idatChunks; }
            set { idatChunks = value; }
        }

        /// <summary>
        /// Gets the text chunks.
        /// </summary>
        public ITextChunk[] TextChunks
        {
            get { return textChunks; }
            internal set { textChunks = value; }
        }

        /// <summary>
        /// Add an IDAT Chunk to end end of existing list.
        /// </summary>
        public void AddIDATChunk(IDATChunk chunk)
        {
            idatChunks.Add(chunk);
        }

        /// <summary>
        /// Gets the frame as PNG FileStream.
        /// </summary>
        public MemoryStream GetStream()
        {
            var ihdrChunk = new IHDRChunk(IHDRChunk);
            if (fcTLChunk != null)
            {
                // Fix frame size with fcTL data.
                ihdrChunk.ModifyChunkData(0, Helper.ConvertEndian(fcTLChunk.Width));
                ihdrChunk.ModifyChunkData(4, Helper.ConvertEndian(fcTLChunk.Height));
            }

            // Write image data
            using (var ms = new MemoryStream())
            {
                ms.WriteBytes(Signature);
                ms.WriteBytes(ihdrChunk.RawData);

                foreach (IDATChunk idatChunk in idatChunks)
                    ms.WriteBytes(idatChunk.RawData);

                foreach (ITextChunk chunk in TextChunks)
                    ms.WriteBytes((chunk as Chunk).RawData);

                ms.WriteBytes(IENDChunk.RawData);

                ms.Position = 0;
                return ms;
            }
        }
    }
}