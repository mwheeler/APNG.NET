using System;
using System.Collections.Generic;
using System.IO;

namespace LibAPNG
{
    public class APNG
    {
        private readonly Frame defaultImage = new Frame();
        private readonly List<Frame> frames = new List<Frame>();
        private readonly MemoryStream ms;
        private readonly Dictionary<string, ITextChunk> textdata = new Dictionary<string, ITextChunk>();

        public APNG(string fileName)
            : this(File.ReadAllBytes(fileName))
        {
        }

        public APNG(byte[] fileBytes)
        {
            ms = new MemoryStream(fileBytes);

            // check file signature.
            if (!Helper.IsBytesEqual(ms.ReadBytes(Frame.Signature.Length), Frame.Signature))
                throw new Exception("File signature incorrect.");

            // Read IHDR chunk.
            IHDRChunk = new IHDRChunk(ms);
            if (IHDRChunk.ChunkType != "IHDR")
                throw new Exception("IHDR chunk must located before any other chunks.");

            // Now let's loop in chunks
            Chunk chunk;
            Frame frame = null;
            List<ITextChunk> text_chunks = new List<ITextChunk>();
            bool isIDATAlreadyParsed = false;
            do
            {
                if (ms.Position == ms.Length)
                    throw new Exception("IEND chunk expected.");

                chunk = new Chunk(ms);

                switch (chunk.ChunkType)
                {
                    case "IHDR":
                        throw new Exception("Only single IHDR is allowed.");
                        break;

                    case "acTL":
                        if (IsSimplePNG)
                            throw new Exception("acTL chunk must located before any IDAT and fdAT");

                        TextChunks = text_chunks.ToArray();
                        text_chunks.Clear();

                        acTLChunk = new acTLChunk(chunk);
                        break;

                    case "IDAT":
                        // To be an APNG, acTL must located before any IDAT and fdAT.
                        if (acTLChunk == null)
                        {
                            TextChunks = text_chunks.ToArray();
                            text_chunks.Clear();
                            IsSimplePNG = true;
                        }

                        // Only default image has IDAT.
                        defaultImage.IHDRChunk = IHDRChunk;
                        defaultImage.AddIDATChunk(new IDATChunk(chunk));
                        isIDATAlreadyParsed = true;
                        break;

                    case "fcTL":
                        // Simple PNG should ignore this.
                        if (IsSimplePNG)
                            continue;

                        if (frame != null && frame.IDATChunks.Count == 0)
                            throw new Exception("One frame must have only one fcTL chunk.");

                        // IDAT already parsed means this fcTL is used by FRAME IMAGE.
                        if (isIDATAlreadyParsed)
                        {
                            // register current frame object and build a new frame object
                            // for next use
                            if (frame != null)
                            {
                                frame.TextChunks = text_chunks.ToArray();
                                frames.Add(frame);
                                text_chunks.Clear();
                            }

                            frame = new Frame
                                        {
                                            IHDRChunk = IHDRChunk,
                                            fcTLChunk = new fcTLChunk(chunk)
                                        };
                        }
                            // Otherwise this fcTL is used by the DEFAULT IMAGE.
                        else
                        {
                            defaultImage.fcTLChunk = new fcTLChunk(chunk);
                        }
                        break;
                    case "fdAT":
                        // Simple PNG should ignore this.
                        if (IsSimplePNG)
                            continue;

                        // fdAT is only used by frame image.
                        if (frame == null || frame.fcTLChunk == null)
                            throw new Exception("fcTL chunk expected.");

                        frame.AddIDATChunk(new fdATChunk(chunk).ToIDATChunk());
                        break;

                    case "IEND":
                        // register last frame object
                        if (frame != null)
                        {
                            frame.TextChunks = text_chunks.ToArray();
                            frames.Add(frame);
                            text_chunks.Clear();
                        }

                        if (DefaultImage.IDATChunks.Count != 0)
                            DefaultImage.IENDChunk = new IENDChunk(chunk);
                        foreach (Frame f in frames)
                        {
                            f.IENDChunk = new IENDChunk(chunk);
                        }
                        break;

                    case "iTXt":
                        text_chunks.Add(new iTXtChunk(chunk));
                        break;

                    case "tEXt":
                        text_chunks.Add(new tEXtChunk(chunk));
                        break;

                    case "zTXt":
                        text_chunks.Add(new zTXtChunk(chunk));
                        break;

                    default:
                        //TODO: Handle other chunks.
                        break;
                }
            } while (chunk.ChunkType != "IEND");

            // We have one more thing to do:
            // If the default image if part of the animation,
            // we should insert it into frames list.
            if (defaultImage.fcTLChunk != null)
            {
                frames.Insert(0, defaultImage);
                DefaultImageIsAnimeated = true;
            }
        }

        /// <summary>
        /// Indicate whether the file is a simple PNG.
        /// </summary>
        public bool IsSimplePNG { get; private set; }

        /// <summary>
        /// Indicate whether the default image is part of the animation
        /// </summary>
        public bool DefaultImageIsAnimeated { get; private set; }

        /// <summary>
        /// Gets the base image.
        /// If IsSimplePNG = True, returns the only image;
        /// if False, returns the default image
        /// </summary>
        public Frame DefaultImage
        {
            get { return defaultImage; }
        }

        /// <summary>
        /// Gets the frame array.
        /// If IsSimplePNG = True, returns empty
        /// </summary>
        public Frame[] Frames
        {
            get { return frames.ToArray(); }
        }

        /// <summary>
        /// Gets the IHDR Chunk
        /// </summary>
        public IHDRChunk IHDRChunk { get; private set; }

        /// <summary>
        /// Gets the acTL Chunk
        /// </summary>
        public acTLChunk acTLChunk { get; private set; }

        /// <summary>
        /// Gets the text chunks
        /// </summary>
        public ITextChunk[] TextChunks { get; private set; }

        /// <summary>
        /// Gets the title of the PNG file.
        /// </summary>
        public string Title
        {
            get
            {
                string[] values = GetTextByKeyword("Title");
                return values.Length > 0 ? values[0] : String.Empty;
            }
        }

        /// <summary>
        /// Gets the description of the PNG file.
        /// </summary>
        public string Description
        {
            get
            {
                string[] values = GetTextByKeyword("Description");
                return values.Length > 0 ? values[0] : String.Empty;
            }
        }

        /// <summary>
        /// Gets the authors of the PNG file.
        /// </summary>
        public string[] Authors
        {
            get { return GetTextByKeyword("Author"); }
        }

        /// <summary>
        /// Gets the copyright attributions of the PNG file.
        /// </summary>
        public string[] Copyright
        {
            get { return GetTextByKeyword("Copyright"); }
        }

        /// <summary>
        /// Gets the legal disclaimer attributions of the PNG file.
        /// </summary>
        public string[] Disclaimer
        {
            get { return GetTextByKeyword("Disclaimer"); }
        }

        /// <summary>
        /// Gets a set of comments associated w/ the frame.
        /// </summary>
        public string[] Comments
        {
            get { return GetTextByKeyword("Comment"); }
        }

        /// <summary>
        /// Get a set of chunks in the frame with a specified keyword.
        /// </summary>
        /// <param name="keyword">The keyword of the chunks to retrieve.</param>
        /// <returns>An array of chunks matching the keyword</returns>
        public ITextChunk[] GetTextChunksByKeyword(string keyword)
        {
            List<ITextChunk> result = new List<ITextChunk>();

            foreach (ITextChunk chunk in TextChunks)
            {
                if (chunk.Keyword != keyword)
                {
                    continue;
                }

                result.Add(chunk);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Get a set of chunks in the frame with a specified keyword.
        /// </summary>
        /// <param name="keyword">The keyword of the chunks to retrieve.</param>
        /// <returns>An array of chunks matching the keyword</returns>
        public string[] GetTextByKeyword(string keyword)
        {
            List<string> result = new List<string>();

            foreach (ITextChunk chunk in TextChunks)
            {
                if (chunk.Keyword != keyword)
                {
                    continue;
                }

                result.Add(chunk.Text);
            }

            return result.ToArray();
        }
    }
}