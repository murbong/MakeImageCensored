using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeImageCensored
{
    public class ImageBinary : ICloneable
    {
        protected byte[] RawImageData { get; set; }

        /// <summary>
        /// Create a new raw image out of a Image-Object
        /// </summary>
        /// <param name="imagesource"></param>
        public ImageBinary(Image imagesource)
        {
            if (imagesource == null)
            {
                throw new ArgumentNullException("imagesource", "No valid Image");
            }
            LoadFromImage(imagesource);
        }

        /// <summary>
        /// Reads image data from an image stream
        /// </summary>
        /// <param name="imagesource"></param>
        public void LoadFromImage(Image imagesource)
        {
            if (imagesource == null)
            {
                throw new ArgumentNullException("imagesource", "No valid Image");
            }

            MemoryStream imagestream = new MemoryStream();
            imagesource.Save(imagestream, imagesource.RawFormat);
            RawImageData = imagestream.ToArray();

            checkImageData();
        }

        /// <summary>
        /// Create a new raw image out of a file
        /// </summary>
        /// <param name="sourcefilename"></param>
        public ImageBinary(string sourcefilename)
        {
            LoadFromFile(sourcefilename);
        }

        /// <summary>
        /// Reads image dara from a file
        /// </summary>
        /// <param name="filename"></param>
        public void LoadFromFile(string filename)
        {
            if (File.Exists(filename))
            {
                RawImageData = File.ReadAllBytes(filename);
            }

            checkImageData();
        }

        /// <summary>
        /// Check if loaded data is present
        /// </summary>
        private void checkImageData()
        {
            if (RawImageData == null)
            {
                throw new System.ArgumentException("The image data was invalid or could not be opened");
            }
        }

        /// <summary>
        /// Save image data to a file
        /// </summary>
        /// <param name="filename"></param>
        public void Save(string filename)
        {
            File.WriteAllBytes(filename, RawImageData);
        }


        #region ICloneable Member

        public object Clone()
        {
            return this.MemberwiseClone();
        }
        #endregion
    }
    public class Png : ImageBinary
    {
        private const int PAYLOAD_OFFSET = 8;
        private const int LENGTH_OF_LENGTHFIELD = 4;
        private const int LENGTH_OF_TYPEFIELD = 4;
        private const int LENGTH_OF_CHECKSUMFIELD = 4;
        private const int MINIMUM_CHUNK_LENGTH = LENGTH_OF_LENGTHFIELD + LENGTH_OF_TYPEFIELD + LENGTH_OF_CHECKSUMFIELD;

        public enum ChunkType
        {
            ImageData, PaletteTable, ImageTrailer, ImageHeader, Transparency,
            /*ColorSpace*/
            Chromaticities, Gamma, IccProfile, SignificantBits, RgbColorSpace,
            /*Text*/
            IsoText, CompressedText, UnicodeText,
            /*misc*/
            BackgroundColor, Histogram, PhyisicalDimensions, SuggestedPalette,
            Time,
            Unknown
        }

        /// <summary>
        /// Opens any image file and convert it to PNG. Load PNG data into memory.
        /// </summary>
        /// <param name="imagesource">The source image</param>
        public Png(Image imagesource)
            : base(imagesource)
        {

            if (imagesource == null)
            {
                throw new NullReferenceException("The image object passed is null");
            }

            // TODO implement image conversion to png
            throw new System.NotImplementedException("implicit conversion to PNG not possible yet");

        }

        /// <summary>
        /// load a PNG file
        /// </summary>
        /// <param name="sorucefilename"></param>
        public Png(string sourcefilename)
            : base(sourcefilename)
        {
            if (!HasValidPngHeader())
            {
                throw new System.ArgumentException("The file '" + sourcefilename + "' does not match PNG format criteria");
            }
        }

        /// <summary>
        /// Checks if the image data is in png file format
        /// </summary>
        private bool HasValidPngHeader()
        {
            int[] pngsignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

            for (int i = 0; i < PAYLOAD_OFFSET; i++)
            {
                if (pngsignature[i] != (Int32)RawImageData[i])
                {
                    return false;
                }
            }
            return true;
        }

        // TODO Removing multiple chunks at a time would be useful

        /// <summary>
        /// Removes a chunk from the loaded PNG
        /// </summary>
        /// <param name="chunkBeingRemoved"></param>
        public void RemoveChunk(ChunkType chunkBeingRemoved)
        {
            int offset = PAYLOAD_OFFSET;
            int chunkDataLength = 0;
            int imageLength = RawImageData.Length;
            ChunkType chunkType = ChunkType.Unknown;
            int maxLengthToCheck = imageLength;//+ MINIMUM_CHUNK_LENGTH;

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(RawImageData, 0, offset);//png header

                while (offset < maxLengthToCheck)
                {
                    chunkDataLength = ReadChunkLength(offset);
                    chunkType = ReadChunkType(offset);
                    if (chunkType != chunkBeingRemoved)
                    {
                        ms.Write(RawImageData, offset, MINIMUM_CHUNK_LENGTH + chunkDataLength);
                        offset += MINIMUM_CHUNK_LENGTH + chunkDataLength;
                    }
                    else
                    {
                        // jump over the chunk we want to filter
                        offset += MINIMUM_CHUNK_LENGTH + chunkDataLength;
                    }
                }
                RawImageData = ms.ToArray();
            }

        }

        public void InsertChunk(string chunk, int length, int data)
        {
            int offset = PAYLOAD_OFFSET;
            int chunkDataLength = 0;
            int imageLength = RawImageData.Length;
            ChunkType chunkType = ChunkType.Unknown;
            int maxLengthToCheck = imageLength;// - MINIMUM_CHUNK_LENGTH;

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(RawImageData, 0, offset);
                while (offset < maxLengthToCheck)
                {
                    chunkDataLength = ReadChunkLength(offset);
                    chunkType = ReadChunkType(offset);
                    if (chunkType == ChunkType.ImageHeader)
                    {
                        ms.Write(RawImageData, offset, MINIMUM_CHUNK_LENGTH + chunkDataLength);
                        offset += MINIMUM_CHUNK_LENGTH + chunkDataLength;
                    }
                    else
                    {
                        byte[] len = BitConverter.GetBytes(length);
                        byte[] chu = Encoding.ASCII.GetBytes(chunk);
                        byte[] dat = BitConverter.GetBytes(data);
                        Array.Reverse(len);
                        //Array.Reverse(chu);
                        Array.Reverse(dat);

                        uint crc = Global.ComputeChecksum(0xffffffff, chu, 4);
                        Console.WriteLine(crc.ToString("X"));
                        crc = Global.ComputeChecksum(crc, dat, 4);
                        Console.WriteLine(crc.ToString("X"));
                        Console.WriteLine(data.ToString("X"));
                        crc ^= 0xffffffff;
                        Console.WriteLine(crc.ToString("X"));
                        var ccrc = BitConverter.GetBytes(crc);

                        Array.Reverse(ccrc);
                        ms.Write(len, 0, 4);
                        ms.Write(chu, 0, 4);
                        ms.Write(dat, 0, 4);
                        ms.Write(ccrc, 0, 4);
                        
                        ms.Write(RawImageData, offset, RawImageData.Length-offset);
                        offset += MINIMUM_CHUNK_LENGTH + length;
                        break;
                    }
                }
                RawImageData = ms.ToArray();
            }
        }

        public void SetChunk(string chunk, int length, int data)
        {
            int offset = PAYLOAD_OFFSET;
            int chunkDataLength = 0;
            int imageLength = RawImageData.Length;
            ChunkType chunkType = ChunkType.Unknown;
            int maxLengthToCheck = imageLength;// - MINIMUM_CHUNK_LENGTH;

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(RawImageData, 0, offset);

                while (offset < maxLengthToCheck)
                {
                    chunkDataLength = ReadChunkLength(offset);
                    chunkType = ReadChunkType(offset);
                    if (chunkType == GetChunk(chunk))
                    {
                        //ms.Write(RawImageData, offset, MINIMUM_CHUNK_LENGTH + chunkDataLength);
                        byte[] len = BitConverter.GetBytes(length);
                        byte[] chu = Encoding.ASCII.GetBytes(chunk);
                        byte[] dat = BitConverter.GetBytes(data);

                        uint crc = Global.ComputeChecksum(0xffffffff, chu,4);
                        crc = Global.ComputeChecksum(crc, dat,4);
                        crc = crc ^ 0xffffffff;
                        var ccrc = BitConverter.GetBytes(crc);


                        Array.Reverse(len);
                        Array.Reverse(dat);
                        Array.Reverse(ccrc);
                        ms.Write(len, 0, 4);
                        ms.Write(chu, 0, 4);
                        ms.Write(dat, 0, length);
                        ms.Write(ccrc, 0, 4);
                        offset += MINIMUM_CHUNK_LENGTH + chunkDataLength;
                    }
                    else
                    {
                        ms.Write(RawImageData, offset, MINIMUM_CHUNK_LENGTH + chunkDataLength);
                        offset += MINIMUM_CHUNK_LENGTH + chunkDataLength;
                    }
                }

                RawImageData = ms.ToArray();
            }

        }

        /// <summary>
        /// Gets the length data field length of the chunk starting at the offset. 
        /// </summary>
        /// <param name="offset">offset of chunk</param>
        /// <returns>length of chunk data</returns>
        private int ReadChunkLength(int offset)
        {
            byte[] chunkLength = new byte[LENGTH_OF_LENGTHFIELD];

            for (int i = 0; i < LENGTH_OF_LENGTHFIELD; i++)
            {
                chunkLength[i] = RawImageData[offset];
                offset++;
            }

            if (System.BitConverter.IsLittleEndian)
                System.Array.Reverse(chunkLength);

            return System.BitConverter.ToInt32(chunkLength, 0);

        }

        private ChunkType GetChunk(string str)
        {
            switch (str)
            {
                case "gAMA":
                    return ChunkType.Gamma;

                case "IHDR":
                    return ChunkType.ImageHeader;

                case "IDAT":
                    return ChunkType.ImageData;

                case "IEND":
                    return ChunkType.ImageTrailer;

                case "PLTE":
                    return ChunkType.PaletteTable;

                case "tRNS":
                    return ChunkType.Transparency;

                case "cHRM":
                    return ChunkType.Chromaticities;

                case "iCCP":
                    return ChunkType.IccProfile;

                case "sBIT":
                    return ChunkType.SignificantBits;

                case "sRGB":
                    return ChunkType.RgbColorSpace;

                case "tEXt":
                    return ChunkType.IsoText;

                case "zTXt":
                    return ChunkType.CompressedText;

                case "iTXt":
                    return ChunkType.UnicodeText;

                case "bKGD":
                    return ChunkType.BackgroundColor;

                case "hIST":
                    return ChunkType.Histogram;

                case "pHYs":
                    return ChunkType.PhyisicalDimensions;

                case "sPLT":
                    return ChunkType.SuggestedPalette;

                case "tIME":
                    return ChunkType.Time;

                default:
                    return ChunkType.Unknown;
            }
        }


        /// <summary>
        /// Gets the chunk type of the chunk beginning at the offset
        /// </summary>
        /// <param name="offset">offset of chunkoffset of chunk</param>
        /// <returns>type of chunk</returns>
        private ChunkType ReadChunkType(int offset)
        {
            string chunkType = System.Text.Encoding.ASCII.GetString(RawImageData, offset + LENGTH_OF_LENGTHFIELD, LENGTH_OF_TYPEFIELD);

            return GetChunk(chunkType);

        }
    }
}
