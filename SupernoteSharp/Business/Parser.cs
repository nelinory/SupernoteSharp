using SupernoteSharp.Common;
using SupernoteSharp.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SupernoteSharp.Business
{
    public class Parser
    {
        public Metadata ParseMetadata(FileStream fileStream, Policy policy)
        {
            Metadata result;
            Exception exception;

            // support for A5/A6 Agile
            try
            {
                SupernoteParser parser = new SupernoteParser();
                result = parser.ParseStream(fileStream, policy);

                return result;
            }
            catch (UnsupportedFileFormatException)
            {
                // ignore this exception and try next parser
            }

            // support for A5X/A6X
            try
            {
                SupernoteXParser parser = new SupernoteXParser();
                result = parser.ParseStream(fileStream, policy);

                return result;
            }
            catch (UnsupportedFileFormatException ex)
            {
                // ignore this exception and try next parser
                exception = ex;
            }

            throw new UnsupportedFileFormatException($"Unsupported file format. {exception.Message}");
        }

        public Notebook LoadNotebook(FileStream fileStream, Policy policy)
        {
            Metadata metadata = ParseMetadata(fileStream, policy);
            Notebook notebook = new Notebook(metadata);

            // attach cover
            int coverAddress = GetCoverAddress(metadata);
            if (coverAddress > 0)
                notebook.Cover.Content = GetContentAtAddress(fileStream, coverAddress);

            // attach keyword data
            foreach (Keyword keyword in notebook.Keywords)
            {
                int keywordAddress = Int32.Parse((string)keyword.Metadata["KEYWORDSITE"]);
                keyword.Content = GetContentAtAddress(fileStream, keywordAddress);
            }

            // attach title data
            List<int> titlePageNumbers = GetPageNumberFromFooterProperty(notebook.Metadata.Footer, "TITLE_");
            for (int i = 0; i < notebook.Titles.Count; i++)
            {
                int titleAddress = Int32.Parse((string)notebook.Titles[i].Metadata["TITLEBITMAP"]);
                notebook.Titles[i].Content = GetContentAtAddress(fileStream, titleAddress);
                notebook.Titles[i].PageNumber = titlePageNumbers[i] - 1; // title indexes are not 0 based
            }

            // attach link data
            List<int> linkPageNumbers = GetPageNumberFromFooterProperty(notebook.Metadata.Footer, "LINK"); // covers LINKO_ & LINKI_
            for (int i = 0; i < notebook.Links.Count; i++)
            {
                int linkAddress = Int32.Parse((string)notebook.Links[i].Metadata["LINKBITMAP"]);
                notebook.Links[i].Content = GetContentAtAddress(fileStream, linkAddress);
                notebook.Links[i].PageNumber = linkPageNumbers[i] - 1;  // link indexes are not 0 based
            }

            // attach page data
            for (int i = 0; i < metadata.TotalPages; i++)
            {
                List<int> bitmapAddresses = GetBitmapAddress(metadata, i);
                if (bitmapAddresses.Count == 1) // the page has no layers - not a Supernote X model
                    notebook.Pages[i].Content = GetContentAtAddress(fileStream, bitmapAddresses[0]);
                else
                {
                    for (int c = 0; c < bitmapAddresses.Count; c++)
                    {
                        if (notebook.Pages[i].Layer(c) != null)
                            notebook.Pages[i].Layer(c).Content = GetContentAtAddress(fileStream, bitmapAddresses[c]);
                    }
                }

                // attach data path
                int totalPathAddress = metadata.Pages[i].ContainsKey("TOTALPATH") == true ? Int32.Parse((string)metadata.Pages[i]["TOTALPATH"]) : 0;
                if (totalPathAddress > 0)
                    notebook.Pages[i].TotalPath = GetContentAtAddress(fileStream, totalPathAddress);

                // attach recogn file data
                int recognFileAddress = metadata.Pages[i].ContainsKey("RECOGNFILE") == true ? Int32.Parse((string)metadata.Pages[i]["RECOGNFILE"]) : 0;
                if (recognFileAddress > 0)
                    notebook.Pages[i].RecognFile = GetContentAtAddress(fileStream, recognFileAddress);

                // attach recogn text data
                int recognTextAddress = metadata.Pages[i].ContainsKey("RECOGNTEXT") == true ? Int32.Parse((string)metadata.Pages[i]["RECOGNTEXT"]) : 0;
                if (recognTextAddress > 0)
                    notebook.Pages[i].RecognText = GetContentAtAddress(fileStream, recognTextAddress);

                // attach external link info
                int externalLinkInfoAddress = metadata.Pages[i].ContainsKey("EXTERNALLINKINFO") == true ? Int32.Parse((string)metadata.Pages[i]["EXTERNALLINKINFO"]) : 0;
                if (externalLinkInfoAddress > 0)
                    notebook.Pages[i].ExternalLinkInfo = GetContentAtAddress(fileStream, externalLinkInfoAddress);
            }

            // attach template link data
            List<Page> pagesWithTemplateLinks = notebook.Pages.Where(x => x.ExternalLinkInfo != null).ToList();
            foreach (Page page in pagesWithTemplateLinks)
            {
                // ExternalLinkInfo contains multiple links separated by a '|' character
                string[] links = Encoding.UTF8.GetString(page.ExternalLinkInfo).Split("|", StringSplitOptions.RemoveEmptyEntries);
                foreach (string link in links)
                {
                    // each link properties are separated by a ',' character
                    string[] linkProperties = link.Split(",", StringSplitOptions.RemoveEmptyEntries);
                    notebook.TemplateLinks.AddRange(GetTemplateLink(linkProperties, notebook.FileId));
                }
            }

            return notebook;
        }

        private int GetCoverAddress(Metadata metadata)
        {
            int coverAddress = 0;
            if (metadata.Footer.ContainsKey("COVER_1") == true)
                coverAddress = Int32.Parse((string)metadata.Footer["COVER_1"]);

            return coverAddress;
        }

        private byte[] GetContentAtAddress(FileStream fileStream, int address)
        {
            byte[] content = null;

            if (address > 0)
            {
                fileStream.Seek(address, SeekOrigin.Begin);
                byte[] contentSizeBytes = new byte[Constants.LENGTH_FIELD_SIZE];
                fileStream.Read(contentSizeBytes, 0, Constants.LENGTH_FIELD_SIZE);
                int contentSize = BitConverter.ToInt32(contentSizeBytes, 0);
                content = new byte[contentSize];
                fileStream.Read(content, 0, contentSize);
            }

            return content;
        }

        private List<int> GetPageNumberFromFooterProperty(Dictionary<string, object> footer, string propertyPrefix)
        {
            List<int> pageNumbers = new List<int>();

            IEnumerable<string> propertyKeys = footer.Keys.Where(p => p.StartsWith(propertyPrefix));
            foreach (string property in propertyKeys)
            {
                // get '0123' from 'TITLE_01234567' or 'LINKI_01234567' or 'LINKO_01234567'
                if (footer[property] is List<string> itemList)
                    pageNumbers.AddRange(itemList.Select(p => Convert.ToInt32(property.Substring(6, 4))));
                else
                    pageNumbers.Add(Int32.Parse(property.Substring(6, 4)));
            }

            return pageNumbers;
        }

        private List<int> GetBitmapAddress(Metadata metadata, int pageNumber)
        {
            List<int> bitmapAddresses = new List<int>();

            if (metadata.IsLayerSupported(pageNumber) == true)
            {
                for (int i = 0; i < Constants.MAX_LAYERS; i++)
                {
                    List<Dictionary<string, object>> addresses = (List<Dictionary<string, object>>)metadata.Pages[pageNumber][Constants.KEY_LAYERS];
                    bitmapAddresses.Add(addresses[i].Count > 0 ? Int32.Parse((string)addresses[i]["LAYERBITMAP"]) : 0);
                }
            }
            else
            {
                // TODO: Need Supernote A5 test note
                //bitmapAddresses.Add(Int32.Parse((string)metadata.Pages[pageNumber]["DATA"]));
                throw new NotImplementedException();
            }

            return bitmapAddresses;
        }

        private List<Link> GetTemplateLink(string[] templateLink, string fileId)
        {
            List<Link> templateLinks = new List<Link>();

            Dictionary<string, object> metadata = new Dictionary<string, object>
            {
                ["LINKRECT"] = $"{Encoding.UTF8.GetString(Convert.FromBase64String(templateLink[3]))}," +
                                $"{Encoding.UTF8.GetString(Convert.FromBase64String(templateLink[4]))}," +
                                $"{Encoding.UTF8.GetString(Convert.FromBase64String(templateLink[5]))}," +
                                $"{Encoding.UTF8.GetString(Convert.FromBase64String(templateLink[6]))}",
                ["LINKTYPE"] = Int32.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(templateLink[7]))) == 0 ? ((Int32)LinkType.Page).ToString() : ((Int32)LinkType.Web).ToString(),
                ["LINKINOUT"] = ((Int32)LinkDirection.Out).ToString(),
                ["LINKBITMAP"] = DateTime.Now.Ticks.ToString(),
                ["LINKTIMESTAMP"] = DateTime.Now.Ticks.ToString(),
                ["LINKFILE"] = templateLink[8],
                ["LINKFILEID"] = fileId,
                ["PAGEID"] = Encoding.UTF8.GetString(Convert.FromBase64String(templateLink[1]))
            };

            Link linkOut = new Link(metadata)
            {
                PageNumber = Int32.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(templateLink[0]))) - 1 // link indexes are not 0 based
            };

            templateLinks.Add(linkOut);

            // in case we are building template page links, we need to ensure we have both IN and OUT links
            // they are the same structure, with the exception of the LINKINOUT attribute
            if (linkOut.Type == (Int32)LinkType.Page)
            {
                metadata["LINKINOUT"] = ((Int32)LinkDirection.In).ToString();
                Link linkIn = new Link(metadata)
                {
                    PageNumber = Int32.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(templateLink[2]))) - 1 // link indexes are not 0 based
                };

                templateLinks.Add(linkIn);
            }

            return templateLinks;
        }
    }
}