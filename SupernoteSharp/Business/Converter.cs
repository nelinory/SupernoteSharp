using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SupernoteSharp.Common;
using SupernoteSharp.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using VectSharp;
using VectSharp.PDF;
using VectSharp.SVG;
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

            public Image Convert(int pageNumber, Dictionary<string, VisibilityOverlay> visibilityOverlay)
            {
                Page page = _notebook.Page(pageNumber);

                if (page.IsLayerSupported == true)
                    return ConvertLayeredPage(page, visibilityOverlay);
                else
                    return ConvertNonLayeredPage(page, visibilityOverlay);
            }

            public List<Image> ConvertAll(Dictionary<string, VisibilityOverlay> visibilityOverlay)
            {
                List<Image> images = new List<Image>();

                for (int i = 0; i < _notebook.TotalPages; i++)
                {
                    images.Add(Convert(i, visibilityOverlay));
                }

                return images;
            }

            public static Dictionary<string, VisibilityOverlay> BuildVisibilityOverlay(VisibilityOverlay background = VisibilityOverlay.Default,
                                                                                        VisibilityOverlay main = VisibilityOverlay.Default,
                                                                                        VisibilityOverlay layer1 = VisibilityOverlay.Default,
                                                                                        VisibilityOverlay layer2 = VisibilityOverlay.Default,
                                                                                        VisibilityOverlay layer3 = VisibilityOverlay.Default)
            {
                return new Dictionary<string, VisibilityOverlay>
                {
                    { "BGLAYER", background },
                    { "MAINLAYER", main },
                    { "LAYER1", layer1 },
                    { "LAYER2", layer2 },
                    { "LAYER3", layer3 }
                };
            }

            public static byte[] GetImageBytes(Image image)
            {
                byte[] imageBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgb24>()];
                image.CloneAs<Rgb24>().CopyPixelDataTo(imageBytes);

                return imageBytes;
            }

            private Image ConvertNonLayeredPage(Page page, Dictionary<string, VisibilityOverlay> visibilityOverlay)
            {
                // TODO: Need Supernote A5 test note
                byte[] pageContent = page.Content;
                if (pageContent == null)
                    return new Image<L8>(Constants.PAGE_WIDTH, Constants.PAGE_HEIGHT);

                IBaseDecoder decoder = FindDecoder(page.Protocol);

                return CreateImageFromDecoder(decoder, pageContent);
            }

            private Image ConvertLayeredPage(Page page, Dictionary<string, VisibilityOverlay> visibilityOverlay)
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

            private Image FlattenLayers(Page page, Dictionary<string, Image> images, Dictionary<string, VisibilityOverlay> visibilityOverlay)
            {
                // flatten all layers if any
                Image<Rgba32> flattenImage = new Image<Rgba32>(Constants.PAGE_WIDTH, Constants.PAGE_HEIGHT, Color.White);

                Dictionary<string, bool> visibility = GetLayerVisibility(page);
                List<string> LayerOrder = page.LayerOrder;
                LayerOrder.Reverse();

                foreach (string layerName in LayerOrder)
                {
                    bool isVisible = visibility[layerName];
                    VisibilityOverlay layerOverlay = visibilityOverlay[layerName];
                    if (layerOverlay == VisibilityOverlay.Invisible || (layerOverlay == VisibilityOverlay.Default && isVisible == false))
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
                    // TODO: Implement vectorized image
                    throw new NotImplementedException();
                else
                {
                    ImageConverter converter = new Converter.ImageConverter(_notebook, DefaultColorPalette.Grayscale);
                    if (pageNumber == ALL_PAGES)
                    {
                        List<Image> images = converter.ConvertAll(ImageConverter.BuildVisibilityOverlay());
                        for (int i = 0; i < images.Count; i++)
                            pageImages.Add(i, images[i]);
                    }
                    else
                        pageImages.Add(pageNumber, converter.Convert(pageNumber, ImageConverter.BuildVisibilityOverlay()));
                }

                return CreatePdf(pageImages, vectorize, enableLinks);
            }

            public byte[] ConvertAll(bool vectorize = false, bool enableLinks = false)
            {
                return Convert(ALL_PAGES, vectorize, enableLinks);
            }

            private byte[] CreatePdf(Dictionary<int, Image> pageImages, bool vectorize, bool enableLinks)
            {
                Dictionary<int, VectSharp.Page> pdfPages = new Dictionary<int, VectSharp.Page>();

                // A4 page size is 11.01" x 15.58"
                // For a PDF document, each dot is 1/72nd of an inch
                double pageWidth = 210 * 72 / 25.4;     // width in pixels
                double pageHeight = 297 * 72 / 25.4;    // height in pixels

                foreach (KeyValuePair<int, Image> kvp in pageImages)
                {
                    Image pageImage = kvp.Value;
                    VectSharp.Page pdfPage = new VectSharp.Page(pageWidth, pageHeight);

                    // set image scale to fit the pdf page
                    pdfPage.Graphics.Scale(pageWidth / pageImage.Width, pageHeight / pageImage.Height);

                    // draw the image onto the pdf page
                    pdfPage.Graphics.DrawRasterImage(0, 0, new RasterImage(ImageConverter.GetImageBytes(pageImage), pageImage.Width, pageImage.Height, PixelFormats.RGB, false));

                    // add completed page
                    pdfPages.Add(kvp.Key, pdfPage);
                }

                // add links if requested
                Dictionary<string, string> links = null;
                if (enableLinks == true)
                    links = AddLinks(pdfPages);

                // create the final pdf document
                Document pdfDocument = new Document();
                pdfDocument.Pages.AddRange(pdfPages.Values);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    pdfDocument.SaveAsPDF(memoryStream, linkDestinations: links);
                    return memoryStream.ToArray();
                }
            }

            private Dictionary<string, string> AddLinks(Dictionary<int, VectSharp.Page> pdfPages)
            {
                List<Link> noteLinks = _notebook.Links;
                Dictionary<string, string> links = new Dictionary<string, string>();

                foreach (KeyValuePair<int, VectSharp.Page> kvp in pdfPages)
                {
                    // get all outbound links for the current page
                    List<Link> outboundLinks = noteLinks.Where(x => x.PageNumber == kvp.Key && x.InOut == (int)LinkDirection.Out).ToList();
                    if (outboundLinks.Count == 0)
                        continue;

                    // link all web links
                    List<Link> webLinks = outboundLinks.Where(x => x.Type == (int)LinkType.Web).ToList();
                    foreach (Link webLink in webLinks)
                    {
                        string webLinkTag = $"WebLink_{webLink.Metadata["LINKBITMAP"]}";
                        string webLinkUrl = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(webLink.FilePath));

                        kvp.Value.Graphics.StrokeRectangle(webLink.Rect.left, webLink.Rect.top, webLink.Rect.width, webLink.Rect.height,
                                                                        Colour.FromRgba(0, 0, 0, 0), tag: webLinkTag);

                        links.Add(webLinkTag, webLinkUrl);
                    }

                    // if we only have one page, do not build the links between pages
                    if (pdfPages.Count == 1)
                        continue;

                    // link all internal page links
                    List<Link> sourceLinks = outboundLinks.Where(x => x.Type == (int)LinkType.Page).ToList();
                    foreach (Link sourceLink in sourceLinks)
                    {
                        bool isInternalLink = (sourceLink.FileId == _notebook.FileId);
                        if (isInternalLink == true)
                        {
                            // each internal link is a pair of outbound and inbound, they have the same timestamp and rect coordinates
                            Link targetLink = noteLinks.Where(x => x.InOut == (int)LinkDirection.In && x.Timestamp == sourceLink.Timestamp && x.Rect.Equals(sourceLink.Rect)).FirstOrDefault();

                            string sourceLinkTag = $"SourceLink_{sourceLink.Metadata["LINKBITMAP"]}";
                            string targetLinkTag = $"TargetLink_{targetLink.Metadata["LINKBITMAP"]}";

                            pdfPages[sourceLink.PageNumber].Graphics.StrokeRectangle(sourceLink.Rect.left, sourceLink.Rect.top, sourceLink.Rect.width, sourceLink.Rect.height,
                                                                                                Colour.FromRgba(0, 0, 0, 0), tag: sourceLinkTag);

                            pdfPages[targetLink.PageNumber].Graphics.StrokeRectangle(targetLink.Rect.left, 0, targetLink.Rect.width, targetLink.Rect.height,
                                                                                                Colour.FromRgba(0, 0, 0, 0), tag: targetLinkTag);

                            links.Add(sourceLinkTag, $"#{targetLinkTag}");
                        }
                    }
                }

                return links;
            }
        }

        public class SvgConverter
        {
            private ColorPalette _palette;
            private ImageConverter _imageConverter;

            public SvgConverter(Notebook notebook, ColorPalette palette)
            {
                _palette = palette ?? DefaultColorPalette.Grayscale;
                _imageConverter = new ImageConverter(notebook, DefaultColorPalette.Grayscale);
            }

            public string Convert(int pageNumber)
            {
                //SvgDocument dwg = new SvgDocument()
                //{
                //    Profile = SvgProfile.Full,
                //    Width = fileformat.PAGE_WIDTH,
                //    Height = fileformat.PAGE_HEIGHT
                //};

                Dictionary<string, VisibilityOverlay> voOnlyBackground = ImageConverter.BuildVisibilityOverlay(background: VisibilityOverlay.Default,
                                                                                                                main: VisibilityOverlay.Invisible,
                                                                                                                layer1: VisibilityOverlay.Invisible,
                                                                                                                layer2: VisibilityOverlay.Invisible,
                                                                                                                layer3: VisibilityOverlay.Invisible);
                Image backgroundImage = _imageConverter.Convert(pageNumber, voOnlyBackground);

                VectSharp.Page page = new VectSharp.Page(backgroundImage.Width, backgroundImage.Height);
                page.Graphics.DrawRasterImage(0, 0, new RasterImage(ImageConverter.GetImageBytes(backgroundImage), backgroundImage.Width, backgroundImage.Height, PixelFormats.RGB, true));

                Dictionary<string, VisibilityOverlay> voAllExceptBackground = ImageConverter.BuildVisibilityOverlay(background: VisibilityOverlay.Invisible);
                Image pageImage = _imageConverter.Convert(pageNumber, voAllExceptBackground);

                //VectSharp.Page page1 = VectSharp.SVG.Parser.FromFile();

                page.SaveAsSVG("C:\\Temp\\file.svg");

                //using (MemoryStream buffer = new MemoryStream())
                //{
                //    bgImg.Save(buffer, ImageFormat.Png);
                //    string bgB64Str = Convert.ToBase64String(buffer.ToArray());
                //    dwg.Children.Add(new SvgImage()
                //    {
                //        Href = $"data:image/png;base64,{bgB64Str}",
                //        Width = fileformat.PAGE_WIDTH,
                //        Height = fileformat.PAGE_HEIGHT
                //    });
                //}

                //VisibilityOverlay voExceptBg = ImageConverter.BuildVisibilityOverlay(background: VisibilityOverlay.Invisible);
                //Image img = this.imageConverter.Convert(pageNumber, visibilityOverlay: voExceptBg);

                //Func<Image, Color, Bitmap> generateColorMask = (Image i, Color c) =>
                //{
                //    Bitmap mask = new Bitmap(i.Width, i.Height, PixelFormat.Format32bppArgb);
                //    for (int x = 0; x < i.Width; x++)
                //    {
                //        for (int y = 0; y < i.Height; y++)
                //        {
                //            Color pixelColor = i.GetPixel(x, y);
                //            mask.SetPixel(x, y, pixelColor == c ? Color.Black : Color.White);
                //        }
                //    }
                //    return mask;
                //};

                //        List<Color> defaultColorList = new List<Color>()
                //{
                //    _palette.Black,
                //    _palette.DarkGray,
                //    _palette.Gray,
                //    _palette.White
                //};
                //        List<Color> userColorList = new List<Color>()
                //{
                //    this.palette.Black,
                //    this.palette.DarkGray,
                //    this.palette.Gray,
                //    this.palette.White
                //};
                //        for (int i = 0; i < defaultColorList.Count; i++)
                //        {
                //            Color defaultColor = defaultColorList[i];
                //            Color userColor = userColorList[i];
                //            Bitmap mask = generateColorMask(img, defaultColor);
                //            PotraceWrapper.Bitmap bmp = new PotraceWrapper.Bitmap(mask);
                //            Path path = bmp.Trace();
                //            if (path.Count > 0)
                //            {
                //                SvgPath svgPath = new SvgPath()
                //                {
                //                    Fill = new SvgColourServer(userColor),
                //                    FillRule = SvgFillRule.NonZero
                //                };
                //                foreach (Curve curve in path)
                //                {
                //                    PointD start = curve.StartPoint;
                //                    svgPath.PathData.Add(new SvgMoveToSegment(start));
                //                    foreach (Segment segment in curve)
                //                    {
                //                        PointD end = segment.EndPoint;
                //                        if (segment.IsCorner)
                //                        {
                //                            PointD c = segment.C;
                //                            svgPath.PathData.Add(new SvgLineSegment(c));
                //                            svgPath.PathData.Add(new SvgLineSegment(end));
                //                        }
                //                        else
                //                        {
                //                            PointD c1 = segment.C1;
                //                            PointD c2 = segment.C2;
                //                            svgPath.PathData.Add(new SvgCubicCurveSegment(c1, c2, end));
                //                        }
                //                    }
                //                    svgPath.PathData.Add(new SvgClosePathSegment());
                //                }
                //                dwg.Children.Add(svgPath);
                //            }
                //        }
                //return dwg.GetXML();
                return null;
            }
        }
    }
}