using SupernoteSharp.Common;
using SupernoteSharp.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SupernoteSharp.Business
{
    public class Parser
    {
        public Metadata ParseMetadata(FileStream fileStream, Policy policy)
        {
            Metadata result;

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

            try
            {
                SupernoteXParser parser = new SupernoteXParser();
                result = parser.ParseStream(fileStream, policy);

                return result;
            }
            catch (UnsupportedFileFormatException)
            {
                // ignore this exception and try next parser
            }

            throw new UnsupportedFileFormatException("Unsupported file format");
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
                notebook.Titles[i].PageNumber = titlePageNumbers[i];
            }

            // attach link data
            List<int> linkPageNumbers = GetPageNumberFromFooterProperty(notebook.Metadata.Footer, "LINK"); // covers LINKO_ & LINKI_
            for (int i = 0; i < notebook.Links.Count; i++)
            {
                int linkAddress = Int32.Parse((string)notebook.Links[i].Metadata["LINKBITMAP"]);
                notebook.Links[i].Content = GetContentAtAddress(fileStream, linkAddress);
                notebook.Links[i].PageNumber = linkPageNumbers[i];
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
                if (footer[property].GetType() != typeof(string))
                {
                    // TODO: Need Supernote A5 test note
                    /*
                    if type(footer[k]) == list:
                        for _ in range(len(footer[k])):
                            page_numbers.append(int(k[6:10]) - 1)
                    else:
                        page_numbers.append(int(k[6:10]) - 1) # e.g. get '0123' from 'TITLE_01234567'                   
                    */
                    throw new NotImplementedException();
                }
                else
                    pageNumbers.Add(Int32.Parse(property.Substring(6, 4))); // get '0123' from 'TITLE_01234567'
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
    }
}