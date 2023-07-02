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
using System.Text;
using System.Text.Json;
using System.Xml;
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
                _palette = palette ?? DefaultColorPalette.Grayscale;
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

            public static Dictionary<int, VectSharp.Page> ImagesToPages(Dictionary<int, Image> pageImages, double pageWidth, double pageHeight)
            {
                Dictionary<int, VectSharp.Page> pages = new Dictionary<int, VectSharp.Page>();

                foreach (KeyValuePair<int, Image> kvp in pageImages)
                {
                    Image pageImage = kvp.Value;
                    VectSharp.Page page = new VectSharp.Page(pageWidth, pageHeight);

                    // set image scale to fit the page
                    page.Graphics.Scale(pageWidth / pageImage.Width, pageHeight / pageImage.Height);

                    // draw the image onto the page
                    page.Graphics.DrawRasterImage(0, 0, new RasterImage(ImageConverter.GetImageBytes(pageImage), pageImage.Width, pageImage.Height, PixelFormats.RGB, false));

                    // add completed page
                    pages.Add(kvp.Key, page);
                }

                return pages;
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
                        continue;
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
                _palette = palette ?? DefaultColorPalette.Grayscale;
            }

            public byte[] Convert(int pageNumber, bool vectorize = false, bool enableLinks = false)
            {
                // A4 page size is 11.01" x 15.58"/ 210mm x 297mm
                // For a PDF document, each dot is 1/72nd of an inch
                double pageWidth = 210 * 72 / 25.4;     // width in pixels = ~595.27
                double pageHeight = 297 * 72 / 25.4;    // height in pixels = ~841.88

                Dictionary<int, VectSharp.Page> pdfPages;

                if (vectorize == true)
                {
                    Dictionary<int, string> pageSvgs = new Dictionary<int, string>();

                    // convert page svgs
                    SvgConverter converter = new Converter.SvgConverter(_notebook, _palette);
                    if (pageNumber == ALL_PAGES)
                    {
                        List<string> svgs = converter.ConvertAll();
                        for (int i = 0; i < svgs.Count; i++)
                            pageSvgs.Add(i, svgs[i]);
                    }
                    else
                        pageSvgs.Add(pageNumber, converter.Convert(pageNumber));

                    // assemble pdf pages
                    pdfPages = SvgConverter.SvgsToPages(pageSvgs, pageWidth, pageHeight);
                }
                else
                {
                    Dictionary<int, Image> pageImages = new Dictionary<int, Image>();

                    // convert page images
                    ImageConverter converter = new Converter.ImageConverter(_notebook, _palette);
                    if (pageNumber == ALL_PAGES)
                    {
                        List<Image> images = converter.ConvertAll(ImageConverter.BuildVisibilityOverlay());
                        for (int i = 0; i < images.Count; i++)
                            pageImages.Add(i, images[i]);
                    }
                    else
                        pageImages.Add(pageNumber, converter.Convert(pageNumber, ImageConverter.BuildVisibilityOverlay()));

                    // assemble pdf pages
                    pdfPages = ImageConverter.ImagesToPages(pageImages, pageWidth, pageHeight);
                }

                return CreatePdf(pdfPages, enableLinks);
            }

            public byte[] ConvertAll(bool vectorize = false, bool enableLinks = false)
            {
                return Convert(ALL_PAGES, vectorize, enableLinks);
            }

            private byte[] CreatePdf(Dictionary<int, VectSharp.Page> pdfPages, bool enableLinks)
            {
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
                        string webLinkUrl = Encoding.UTF8.GetString(System.Convert.FromBase64String(webLink.FilePath));

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
            private Notebook _notebook;
            private ColorPalette _palette;
            private ImageConverter _imageConverter;

            public SvgConverter(Notebook notebook, ColorPalette palette)
            {
                _notebook = notebook;
                _palette = palette ?? DefaultColorPalette.Grayscale;
                _imageConverter = new ImageConverter(notebook, _palette);
            }

            public string Convert(int pageNumber)
            {
                // page background
                Dictionary<string, VisibilityOverlay> voOnlyBackground = ImageConverter.BuildVisibilityOverlay(background: VisibilityOverlay.Default,
                                                                                                                main: VisibilityOverlay.Invisible,
                                                                                                                layer1: VisibilityOverlay.Invisible,
                                                                                                                layer2: VisibilityOverlay.Invisible,
                                                                                                                layer3: VisibilityOverlay.Invisible);
                Image backgroundImage = _imageConverter.Convert(pageNumber, voOnlyBackground);
                string backgroundImageBase64;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    backgroundImage.SaveAsPng(memoryStream);
                    backgroundImageBase64 = System.Convert.ToBase64String(memoryStream.ToArray());
                }

                // page pen drawings
                Dictionary<string, VisibilityOverlay> voAllExceptBackground = ImageConverter.BuildVisibilityOverlay(background: VisibilityOverlay.Invisible);
                Image pageImage = _imageConverter.Convert(pageNumber, voAllExceptBackground);

                // Potrace works only with two colors, black & white or on & off
                // Supernote uses four colors, so to preserve the colors create a mask image for each color and trace it,
                // then set fill color for each traced path to the correct color
                List<int> paletteColorList = new List<int>()
                {
                    _palette.Black,
                    _palette.DarkGray,
                    _palette.Gray,
                    _palette.White
                };

                StringBuilder pageImagePath = new StringBuilder();
                for (int c = 0; c < paletteColorList.Count; c++)
                {
                    Image<Rgb24> imageMask = GenerateColorMask(pageImage, (byte)paletteColorList[c]);

                    List<List<Curve>> ListOfPathes = new List<List<Curve>>();
                    Potrace.Clear();
                    Potrace.Potrace_Trace(imageMask, ListOfPathes);

                    if (ListOfPathes.Count > 0)
                        pageImagePath.Append(Potrace.getPathTag(ColorUtilities.WebString(paletteColorList[c], "grayscale")));
                }

                // create the final svg document
                string svgTemplate =
                    $"<svg id=\"svg\" version=\"1.1\" width=\"{backgroundImage.Width}\" height=\"{backgroundImage.Height}\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\">" +
                    $"<image width=\"{backgroundImage.Width}\" height=\"{backgroundImage.Height}\" xlink:href=\"data:image/png;base64,{backgroundImageBase64}\"/>" +
                    $"{pageImagePath}" +
                    $"</svg>";

                return svgTemplate;
            }

            public List<string> ConvertAll()
            {
                List<string> svgPages = new List<string>();

                for (int i = 0; i < _notebook.TotalPages; i++)
                {
                    svgPages.Add(Convert(i));
                }

                return svgPages;
            }

            public static Dictionary<int, VectSharp.Page> SvgsToPages(Dictionary<int, string> pageSvgs, double pageWidth, double pageHeight)
            {
                Dictionary<int, VectSharp.Page> pages = new Dictionary<int, VectSharp.Page>();

                foreach (KeyValuePair<int, string> kvp in pageSvgs)
                {
                    string pageSvg = kvp.Value;

                    // extract background image
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(pageSvg);
                    XmlNode node = xmlDocument.GetElementsByTagName("image")[0]; // there should be one image only
                    Image background = Image.Load(System.Convert.FromBase64String(node.Attributes["xlink:href"].InnerText.Substring("data:image/png;base64,".Length)));

                    VectSharp.Page backgroundPage = new VectSharp.Page(background.Width, background.Height);

                    // draw the background onto the page
                    backgroundPage.Graphics.DrawRasterImage(0, 0, new RasterImage(ImageConverter.GetImageBytes(background), background.Width, background.Height, PixelFormats.RGB, false));

                    // extract svg layer
                    xmlDocument.DocumentElement.RemoveChild(node); // remove background node from svg document
                    VectSharp.Page penDrawingsPage = new VectSharp.Page(background.Width, background.Height);

                    // load svg onto the page
                    penDrawingsPage.Graphics.DrawGraphics(0, 0, VectSharp.SVG.Parser.FromString(xmlDocument.InnerXml).Graphics);

                    VectSharp.Page page = new VectSharp.Page(pageWidth, pageHeight);

                    // set svg scale to fit the page
                    page.Graphics.Scale(pageWidth / background.Width, pageHeight / background.Height);

                    // merge both pages
                    page.Graphics.DrawGraphics(0, 0, backgroundPage.Graphics);
                    page.Graphics.DrawGraphics(0, 0, penDrawingsPage.Graphics);

                    // add completed page
                    pages.Add(kvp.Key, page);
                }

                return pages;
            }

            private Image<Rgb24> GenerateColorMask(Image image, byte maskColor)
            {
                Image<Rgb24> targetImage = new Image<Rgb24>(image.Width, image.Height);

                using (Image<Rgb24> sourceImage = image.CloneAs<Rgb24>())
                {
                    int height = sourceImage.Height;

                    sourceImage.ProcessPixelRows(targetImage, (sourceAccessor, targetAccessor) =>
                    {
                        for (int y = 0; y < sourceAccessor.Height; y++)
                        {
                            Span<Rgb24> sourceRow = sourceAccessor.GetRowSpan(y);
                            Span<Rgb24> targetRow = targetAccessor.GetRowSpan(y);

                            for (int x = 0; x < sourceRow.Length; x++)
                            {
                                ref Rgb24 pixelSource = ref sourceRow[x];
                                ref Rgb24 pixelTarget = ref targetRow[x];

                                if (pixelSource.R == maskColor && pixelSource.G == maskColor && pixelSource.B == maskColor)
                                    pixelTarget.R = pixelTarget.G = pixelTarget.B = (byte)_palette.Black; // replace mask color with black
                                else
                                    pixelTarget.R = pixelTarget.G = pixelTarget.B = (byte)_palette.White;
                            }
                        }
                    });
                }

                return targetImage;
            }
        }

        public class TextConverter
        {
            private Notebook _notebook;
            private ColorPalette _palette;

            public TextConverter(Notebook notebook, ColorPalette palette)
            {
                _notebook = notebook;
                _palette = palette ?? DefaultColorPalette.Grayscale;
            }

            public string Convert(int pageNumber)
            {
                if (_notebook.IsRealtimeRecognition == false)
                    return String.Empty;

                Page page = _notebook.Page(pageNumber);
                if (page.RecognStatus != Constants.RECOGNSTATUS_DONE)
                    throw new ConverterException($"note recognition status = {page.RecognStatus}, expected = {Constants.RECOGNSTATUS_DONE}");

                byte[] recognBytes = page.RecognText;
                Decoder.TxtDecoder decoder = new Decoder.TxtDecoder();
                List<string> recognText = decoder.Decode(recognBytes, _palette);

                return String.Join(" ", recognText);
            }
        }
    }
}