using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SupernoteSharp.Common;
using SupernoteSharp.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using VectSharp;
using VectSharp.PDF;
using Page = SupernoteSharp.Entities.Page;

namespace SupernoteSharp.Business
{
    public static class Converter
    {
        public class ImageConverter
        {
            private const uint SPECIAL_WHITE_STYLE_BLOCK_SIZE = 0x140e;

            private Notebook _notebook;
            private ColorPalette _palette;

            public ImageConverter(Notebook notebook, ColorPalette palette)
            {
                _notebook = notebook;
                _palette = palette;
            }

            public Image Convert(int pageNumber, VisibilityOverlay visibilityOverlay)
            {
                Page page = _notebook.Page(pageNumber);

                if (page.IsLayerSupported == true)
                    return ConvertLayeredPage(page, visibilityOverlay);
                else
                    return ConvertNonLayeredPage(page, visibilityOverlay);
            }

            public List<Image> ConvertAll(VisibilityOverlay visibilityOverlay)
            {
                List<Image> images = new List<Image>();

                for (int i = 0; i < _notebook.TotalPages; i++)
                {
                    images.Add(Convert(i, visibilityOverlay));
                }

                return images;
            }

            private Image ConvertNonLayeredPage(Page page, VisibilityOverlay visibilityOverlay)
            {
                // TODO: Need Supernote A5 test note
                byte[] pageContent = page.Content;
                if (pageContent == null)
                    return new Image<L8>(Constants.PAGE_WIDTH, Constants.PAGE_HEIGHT);

                IBaseDecoder decoder = FindDecoder(page.Protocol);

                return CreateImageFromDecoder(decoder, pageContent);
            }

            private Image ConvertLayeredPage(Page page, VisibilityOverlay visibilityOverlay)
            {
                // TODO: Do we need the workaround ?
                // page = utils.WorkaroundPageWrapper.from_page(page)
                Dictionary<string, Image> images = new Dictionary<string, Image>();

                foreach (Layer layer in page.Layers)
                {
                    // unused layers will be null
                    if (layer == null)
                        continue;

                    string layerName = layer.Name;
                    byte[] layerContent = layer.Content;

                    if (layerContent == null)
                    {
                        images[layerName] = null;
                        continue;
                    }

                    IBaseDecoder decoder = FindDecoder(layer.Protocol);
                    string pageStyle = page.Style;
                    bool allBlank = (layerName == "BGLAYER" && pageStyle != null && pageStyle == "style_white" && layerContent.Length == SPECIAL_WHITE_STYLE_BLOCK_SIZE);
                    bool customBackground = (layerName == "BGLAYER" && pageStyle != null && pageStyle.StartsWith("user_"));

                    if (customBackground == true)
                        decoder = new Decoder.PngDecoder();

                    images.Add(layerName, CreateImageFromDecoder(decoder, layerContent, allBlank));
                }

                return FlattenLayers(page, images, visibilityOverlay);
            }

            private IBaseDecoder FindDecoder(string protocol)
            {
                switch (protocol)
                {
                    case "SN_ASA_COMPRESS":
                        return new Decoder.FlateDecoder();
                    case "RATTA_RLE":
                        return new Decoder.RattaRleDecoder();
                    default:
                        throw new UnknownDecodeProtocolException($"unknown decode protocol: {protocol}");
                }
            }

            private Image CreateImageFromDecoder(IBaseDecoder decoder, byte[] binaryImage, bool allBlank = false)
            {
                (byte[] imageBytes, (int width, int height) size, int bitsPerPixel) decodedLayer = decoder.Decode(binaryImage, _palette, allBlank);

                // Loading raw pixel data into an Image
                int width = decodedLayer.size.width;
                int height = decodedLayer.size.height;

                return Image.LoadPixelData<L8>(decodedLayer.imageBytes, width, height);
            }

            private Image FlattenLayers(Page page, Dictionary<string, Image> images, VisibilityOverlay visibilityOverlay)
            {
                // flatten all layers if any
                Image<Rgba32> flattenImage = new Image<Rgba32>(Constants.PAGE_WIDTH, Constants.PAGE_HEIGHT, Color.White);

                Dictionary<string, bool> visibility = GetLayerVisibility(page);
                List<string> LayerOrder = page.LayerOrder;
                LayerOrder.Reverse();

                foreach (string layerName in LayerOrder)
                {
                    bool isVisible = visibility[layerName];
                    if (visibilityOverlay == VisibilityOverlay.Invisible || (visibilityOverlay == VisibilityOverlay.Default && isVisible == false))
                    {
                        continue;
                    }
                    else
                    {
                        if (isVisible == false)
                            continue;
                    }

                    Image<Rgba32> imageLayer = images[layerName].CloneAs<Rgba32>();
                    if (imageLayer != null)
                    {
                        if (layerName == "BGLAYER")
                        {
                            // convert transparent to white for template
                            imageLayer.Mutate(x => x.BackgroundColor(Color.White));
                        }

                        flattenImage = FlattenImage(imageLayer, flattenImage);
                    }
                }

                return flattenImage.CloneAs<L8>();
            }

            private Dictionary<string, bool> GetLayerVisibility(Page page)
            {
                Dictionary<string, bool> layerVisibility = new Dictionary<string, bool>();
                string layerInfo = page.LayerInfo;

                if (layerInfo == null)
                    return layerVisibility;

                List<Dictionary<string, object>> infoArray = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(layerInfo);
                foreach (var layer in infoArray)
                {
                    bool isBackgroundLayer = Boolean.Parse(layer["isBackgroundLayer"].ToString());
                    int layerId = Int32.Parse(layer["layerId"].ToString());
                    bool isMainLayer = (layerId == 0) && (isBackgroundLayer == false);
                    bool isVisible = Boolean.Parse(layer["isVisible"].ToString());

                    if (isBackgroundLayer == true)
                        layerVisibility["BGLAYER"] = isVisible;
                    else if (isMainLayer == true)
                        layerVisibility["MAINLAYER"] = isVisible;
                    else
                        layerVisibility["LAYER" + layerId] = isVisible;
                }

                // some old files don't include MAINLAYER info, so we set MAINLAYER visible
                if (layerVisibility.ContainsKey("MAINLAYER") == false)
                    layerVisibility["MAINLAYER"] = true;

                return layerVisibility;
            }

            private Image<Rgba32> FlattenImage(Image<Rgba32> foreground, Image<Rgba32> background)
            {
                // make all foreground white pixels transparent by setting the alpha channel to 0
                foreground.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            ref Rgba32 pixel = ref pixelRow[x];
                            if (pixel == new Rgba32(255, 255, 255, 255))
                                pixel.A = 0;
                        }
                    }
                });

                background.Mutate(x => x.DrawImage(foreground, new GraphicsOptions
                {
                    AlphaCompositionMode = PixelAlphaCompositionMode.SrcAtop // paste foreground over background
                }));

                return background;
            }
        }

        public class PdfConverter
        {
            private const int ALL_PAGES = -1;

            private Notebook _notebook;
            private ColorPalette _palette;

            public PdfConverter(Notebook notebook, ColorPalette palette)
            {
                _notebook = notebook;
                _palette = palette;
            }

            public byte[] Convert(int pageNumber, bool vectorize = false, bool enableLinks = false)
            {
                Dictionary<int, Image> pageImages = new Dictionary<int, Image>();

                if (vectorize == true)
                    throw new NotImplementedException();
                else
                {
                    ImageConverter converter = new Converter.ImageConverter(_notebook, DefaultColorPalette.Grayscale);
                    if (pageNumber == ALL_PAGES)
                    {
                        List<Image> images = converter.ConvertAll(VisibilityOverlay.Default);
                        for (int i = 0; i < images.Count; i++)
                            pageImages.Add(i, images[i]);
                    }
                    else
                        pageImages.Add(pageNumber, converter.Convert(pageNumber, VisibilityOverlay.Default));
                }

                return CreatePdf(pageImages, enableLinks);
            }

            public byte[] ConvertAll(bool vectorize = false, bool enableLinks = false)
            {
                return Convert(ALL_PAGES, vectorize, enableLinks);
            }

            private byte[] CreatePdf(Dictionary<int, Image> pageImages, bool enableLinks)
            {
                Document pdfDocument = new Document();

                // A4 page size, for a PDF document, each dot is 1/72nd of an inch
                double pageWidth = 210 * 72 / 25.4;
                double pageHeight = 297 * 72 / 25.4;

                foreach (KeyValuePair<int, Image> kvp in pageImages)
                {
                    Image pageImage = kvp.Value;
                    VectSharp.Page pdfPage = new VectSharp.Page(pageWidth, pageHeight);

                    // set image scale to fit the pdf page
                    pdfPage.Graphics.Scale(pageWidth / pageImage.Width, pageHeight / pageImage.Height);

                    // convert image to byte[]
                    byte[] imageBytes = new byte[pageImage.Width * pageImage.Height * Unsafe.SizeOf<Rgba32>()];
                    pageImage.CloneAs<Rgba32>().CopyPixelDataTo(imageBytes);

                    // draw the image onto the pdf page
                    pdfPage.Graphics.DrawRasterImage(0, 0, new RasterImage(imageBytes, pageImage.Width, pageImage.Height, PixelFormats.RGBA, false));

                    // add  links if requested
                    if (enableLinks == true)
                    {
                        string pageId = _notebook.Page(kvp.Key).PageId;
                        if (String.IsNullOrEmpty(pageId) == false)
                            AddLinks(kvp.Key, pdfPage);
                    }

                    // add completed page
                    pdfDocument.Pages.Add(pdfPage);
                }

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    pdfDocument.SaveAsPDF(memoryStream);
                    return memoryStream.ToArray();
                }
            }

            private void AddLinks(int pageNumber, VectSharp.Page pdfPage)
            {
                throw new NotImplementedException();

                // TODO: Complete

                List<Link> links = _notebook.Links;
                for (int i = 0; i < links.Count; i++)
                {
                    Link link = links[i];
                    if (link.PageNumber != pageNumber)
                        continue;

                    if (link.InOut == (int)LinkDirection.In)
                        continue; // ignore incoming links

                    int linkType = link.Type;
                    bool isInternalLink = (link.FileId == _notebook.FileId);

                    if (linkType == (int)LinkType.Page && isInternalLink == true)
                    {
                        string tag = link.PageId;

                        // links are transparent
                        pdfPage.Graphics.StrokeRectangle(link.Rect.left, link.Rect.top, link.Rect.width, link.Rect.height, Colours.Blue /*Colour.FromRgba(0, 0, 0, 0)*/, tag: $"");
                    }
                }
            }
        }
    }
}