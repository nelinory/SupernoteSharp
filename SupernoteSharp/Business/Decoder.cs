using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SupernoteSharp.Common;
using SupernoteSharp.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text;

namespace SupernoteSharp.Business
{
    internal interface IBaseDecoder
    {
        internal (byte[] imageBytes, (int width, int height) size, int bitsPerPixel) Decode(byte[] data, ColorPalette palette = null, bool allBlank = false);
    }

    internal static class Decoder
    {
        internal class FlateDecoder : IBaseDecoder
        {
            // Decoder for SN_ASA_COMPRESS protocol.
            private const ushort COLORCODE_BLACK = 0x0000;
            private const ushort COLORCODE_BACKGROUND = 0xffff;
            private const ushort COLORCODE_DARK_GRAY = 0x2104;
            private const ushort COLORCODE_GRAY = 0xe1e2;

            private const int INTERNAL_PAGE_HEIGHT = 1888;
            private const int INTERNAL_PAGE_WIDTH = 1404;

            public (byte[] imageBytes, (int width, int height) size, int bitsPerPixel) Decode(byte[] data, ColorPalette palette = null, bool allBlank = false)
            {
                byte[] uncompressed = null;

                using (var compressedStream = new MemoryStream(data))
                {
                    using (var decompressedStream = new MemoryStream())
                    {
                        using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                        {
                            deflateStream.CopyTo(decompressedStream);
                        }

                        uncompressed = decompressedStream.ToArray();
                    }
                }

                ushort[] bitmap = uncompressed.Select(b => (ushort)b).ToArray();

                Array.Resize(ref bitmap, INTERNAL_PAGE_WIDTH * INTERNAL_PAGE_HEIGHT);
                Array.Reverse(bitmap);

                bitmap = bitmap.Select((b, i) => new { Value = b, Index = i })
                               .Where(x => x.Index % INTERNAL_PAGE_WIDTH < INTERNAL_PAGE_HEIGHT)
                               .Select(x => x.Value)
                               .ToArray();

                if (palette == null)
                    palette = DefaultColorPalette.Grayscale;

                int bitsPerPixel;
                if (palette.Mode == Constants.MODE_RGB)
                {
                    bitsPerPixel = 32;
                    uint alpha = 0xff;
                    for (int i = 0; i < bitmap.Length; i++)
                    {
                        if (bitmap[i] == COLORCODE_BLACK)
                            bitmap[i] = (ushort)(((uint)palette.Black << 8) | alpha);
                        else if (bitmap[i] == COLORCODE_DARK_GRAY)
                            bitmap[i] = (ushort)(((uint)palette.DarkGray << 8) | alpha);
                        else if (bitmap[i] == COLORCODE_GRAY)
                            bitmap[i] = (ushort)(((uint)palette.Gray << 8) | alpha);
                        else if (bitmap[i] == COLORCODE_BACKGROUND)
                            bitmap[i] = (ushort)(((uint)palette.White << 8) | alpha);
                    }
                }
                else
                {
                    bitsPerPixel = 8;
                    for (int i = 0; i < bitmap.Length; i++)
                    {
                        if (bitmap[i] == COLORCODE_BLACK)
                            bitmap[i] = (ushort)palette.Black;
                        else if (bitmap[i] == COLORCODE_DARK_GRAY)
                            bitmap[i] = (ushort)palette.DarkGray;
                        else if (bitmap[i] == COLORCODE_GRAY)
                            bitmap[i] = (ushort)palette.Gray;
                        else if (bitmap[i] == COLORCODE_BACKGROUND)
                            bitmap[i] = (ushort)palette.White;
                    }
                }

                byte[] imageBytes = new byte[bitmap.Length * bitsPerPixel / 8];
                Buffer.BlockCopy(bitmap, 0, imageBytes, 0, imageBytes.Length);

                return (imageBytes, (INTERNAL_PAGE_HEIGHT, INTERNAL_PAGE_WIDTH), bitsPerPixel);
            }
        }

        internal class RattaRleDecoder : IBaseDecoder
        {
            // Decoder for RATTA_RLE protocol.
            private const byte COLORCODE_BLACK = 0x61;
            private const byte COLORCODE_BACKGROUND = 0x62;
            private const byte COLORCODE_DARK_GRAY = 0x63;
            private const byte COLORCODE_GRAY = 0x64;
            private const byte COLORCODE_WHITE = 0x65;
            private const byte COLORCODE_MARKER_BLACK = 0x66;
            private const byte COLORCODE_MARKER_DARK_GRAY = 0x67;
            private const byte COLORCODE_MARKER_GRAY = 0x68;

            private const byte SPECIAL_LENGTH_MARKER = 0xff;
            private const int SPECIAL_LENGTH = 0x4000;
            private const int SPECIAL_LENGTH_FOR_BLANK = 0x400;

            public (byte[] imageBytes, (int width, int height) size, int bitsPerPixel) Decode(byte[] data, ColorPalette palette = null, bool allBlank = false)
            {
                if (palette == null)
                    palette = DefaultColorPalette.Grayscale;

                int bitsPerPixel = (palette.Mode == Constants.MODE_RGB) ? 24 : 8;

                Dictionary<byte, int> colormap = new Dictionary<byte, int>()
                {
                    { COLORCODE_BLACK, palette.Black },
                    { COLORCODE_BACKGROUND, palette.Transparent },
                    { COLORCODE_DARK_GRAY, palette.DarkGray },
                    { COLORCODE_GRAY, palette.Gray },
                    { COLORCODE_WHITE, palette.White },
                    { COLORCODE_MARKER_BLACK, palette.Black },
                    { COLORCODE_MARKER_DARK_GRAY, palette.DarkGray },
                    { COLORCODE_MARKER_GRAY, palette.Gray }
                };

                int expectedLength = Constants.PAGE_HEIGHT * Constants.PAGE_WIDTH * (bitsPerPixel / 8);

                (byte colorcode, int length) holder = (0, 0);
                List<byte> uncompressed = new List<byte>();
                IEnumerator<byte> bin = data.AsEnumerable().GetEnumerator();
                try
                {
                    Queue<(byte, int)> waiting = new Queue<(byte, int)>();
                    while (true)
                    {
                        byte colorcode = bin.MoveNext() ? bin.Current : (byte)0;
                        int length = bin.MoveNext() ? bin.Current : 0;
                        bool dataPushed = false;

                        // we reach the end of the enumeration due to colorcode being 0
                        if (colorcode == 0)
                            break;

                        if (holder.Item2 > 0)
                        {
                            (byte prevColorcode, int prevLength) = holder;
                            holder = (0, 0);
                            if (colorcode == prevColorcode)
                            {
                                length = (1 + length + (((prevLength & 0x7f) + 1) << 7));
                                waiting.Enqueue((colorcode, length));
                                dataPushed = true;
                            }
                            else
                            {
                                prevLength = (((prevLength & 0x7f) + 1) << 7);
                                waiting.Enqueue((prevColorcode, prevLength));
                            }
                        }

                        if (dataPushed == false)
                        {
                            if (length == SPECIAL_LENGTH_MARKER)
                            {
                                if (allBlank == true)
                                    length = SPECIAL_LENGTH_FOR_BLANK;
                                else
                                    length = SPECIAL_LENGTH;

                                waiting.Enqueue((colorcode, length));
                                dataPushed = true;
                            }
                            else if ((length & 0x80) != 0)
                            {
                                holder = (colorcode, length);
                            }
                            else
                            {
                                length += 1;
                                waiting.Enqueue((colorcode, length));
                                dataPushed = true;
                            }
                        }

                        while (waiting.Count > 0)
                        {
                            (byte colorCode, int len) = waiting.Dequeue();
                            uncompressed.AddRange(CreateColorByteArray(palette.Mode, colormap, colorCode, len));
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // TODO: Need Supernote A5 test note
                    if (holder.length > 0)
                    {
                        (byte colorcode, int length) = holder;
                        length = AdjustTailLength(length, uncompressed.Count, expectedLength);
                        if (length > 0)
                            uncompressed.AddRange(CreateColorByteArray(palette.Mode, colormap, colorcode, length));
                    }
                }

                if (uncompressed.Count != expectedLength)
                    throw new DecoderException($"uncompressed bitmap length = {uncompressed.Count}, expected = {expectedLength}");

                return (uncompressed.ToArray(), (Constants.PAGE_WIDTH, Constants.PAGE_HEIGHT), bitsPerPixel);
            }

            private byte[] CreateColorByteArray(string mode, Dictionary<byte, int> colormap, byte colorCode, int length)
            {
                if (mode == Constants.MODE_RGB)
                {
                    (byte r, byte g, byte b) = ColorUtilities.GetRgb(colormap[colorCode]);

                    return Enumerable.Repeat(new byte[] { r, g, b }, length).SelectMany(x => x).ToArray();
                }
                else
                {
                    byte c = (byte)colormap[colorCode];

                    return Enumerable.Repeat(c, length).ToArray();
                }
            }

            private int AdjustTailLength(int tailLength, int currentLength, int totalLength)
            {
                int gap = totalLength - currentLength;

                for (int i = 7; i >= 0; i--)
                {
                    int l = ((tailLength & 0x7f) + 1) << i;
                    if (l <= gap)
                        return l;
                }

                return 0;
            }
        }

        internal class PngDecoder : IBaseDecoder
        {
            public (byte[] imageBytes, (int width, int height) size, int bitsPerPixel) Decode(byte[] data, ColorPalette palette = null, bool allBlank = false)
            {
                using (Image<L8> pngImage = Image.Load<L8>(new MemoryStream(data)))
                {
                    int width = pngImage.Width;
                    int height = pngImage.Height;

                    byte[] bitmapPixels = new byte[width * height * Unsafe.SizeOf<L8>()];
                    pngImage.CopyPixelDataTo(bitmapPixels);

                    return (bitmapPixels, (Constants.PAGE_WIDTH, Constants.PAGE_HEIGHT), pngImage.PixelType.BitsPerPixel);
                }
            }
        }

        internal class TxtDecoder
        {
            public List<string> Decode(byte[] data, ColorPalette palette, bool allBlank = false)
            {
                List<string> noteText = new List<string>();

                if (data == null || data.Length == 0)
                    return noteText;

                string recognText = Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(data)));
                Recogn recogn = JsonSerializer.Deserialize<Recogn>(recognText);

                return recogn.elements.Where(p => p.type == "Text").Select(t => t.label.ToString()).ToList();
            }
        }
    }
}
