using SupernoteSharp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SupernoteSharp.Business
{
    internal class SupernoteXParser : SupernoteParser
    {
        internal override string SN_SIGNATURE_PATTERN
        {
            get
            {
                return @"(note|mark)SN_FILE_VER_\d{8}";
            }
        }

        internal override List<String> SN_SIGNATURES
        {
            get
            {
                return new List<String>
                {
                    "noteSN_FILE_VER_20200001", // Firmware version C.053
                    "noteSN_FILE_VER_20200005", // Firmware version C.077
                    "noteSN_FILE_VER_20200006", // Firmware version C.130
                    "noteSN_FILE_VER_20200007", // Firmware version C.159
                    "noteSN_FILE_VER_20200008", // Firmware version C.237
                    "noteSN_FILE_VER_20210009", // Firmware version C.291
                    "markSN_FILE_VER_20220011", // Firmware version Chauvet 2.1.6
                    "noteSN_FILE_VER_20210010", // Firmware version Chauvet 2.1.6
                    "markSN_FILE_VER_20220011", // Firmware version Chauvet 2.5.17
                    "noteSN_FILE_VER_20220011", // Firmware version Chauvet 2.5.17
                    "markSN_FILE_VER_20220013", // Firmware version Chauvet 2.9.24
                    "noteSN_FILE_VER_20220013"  // Firmware version Chauvet 2.9.24
                  //  "noteSN_FILE_VER_20220014", // Firmware version Chauvet 2.11.26
                  //  "markSN_FILE_VER_20220014"  // Firmware version Chauvet 2.11.26
                };
            }
        }

        internal List<String> LAYER_KEYS
        {
            get
            {
                return new List<String>
                {
                    "MAINLAYER",
                    "LAYER1",
                    "LAYER2",
                    "LAYER3",
                    "BGLAYER"
                };
            }
        }

        internal override Dictionary<string, object> ParseFooterBlock(FileStream fileStream, int footerAddress)
        {
            Dictionary<string, object> footer = base.ParseFooterBlock(fileStream, footerAddress);

            // parse keywords
            List<int> keywordAddresses = GetItemAddresses(footer, "KEYWORD_");
            List<Dictionary<string, object>> keywords = new List<Dictionary<string, object>>();
            foreach (int keywordAddress in keywordAddresses)
            {
                keywords.Add(ParseMetadataBlock(fileStream, keywordAddress));
            }
            if (keywords.Count > 0)
                footer[Constants.KEY_KEYWORDS] = keywords;

            // parse titles
            List<int> titleAddresses = GetItemAddresses(footer, "TITLE_");
            List<Dictionary<string, object>> titles = new List<Dictionary<string, object>>();
            foreach (int titleAddress in titleAddresses)
            {
                titles.Add(ParseMetadataBlock(fileStream, titleAddress));
            }
            if (titles.Count > 0)
                footer[Constants.KEY_TITLES] = titles;

            // parse links
            List<int> linkAddresses = GetItemAddresses(footer, "LINK");
            List<Dictionary<string, object>> links = new List<Dictionary<string, object>>();
            foreach (int linkAddress in linkAddresses)
            {
                links.Add(ParseMetadataBlock(fileStream, linkAddress));
            }
            if (links.Count > 0)
                footer[Constants.KEY_LINKS] = links;

            return footer;
        }

        internal override Dictionary<string, object> ParsePageBlock(FileStream fileStream, int pageAddress)
        {
            Dictionary<string, object> page = ParseMetadataBlock(fileStream, pageAddress);

            // parse layers
            List<int> layerAddresses = new List<int>();
            foreach (string layerKey in LAYER_KEYS)
            {
                List<int> items = GetItemAddresses(page, layerKey);
                layerAddresses.Add(items.Count > 0 ? items[0] : 0);
            }
            List<Dictionary<string, object>> layers = new List<Dictionary<string, object>>();
            foreach (int layerAddress in layerAddresses)
            {
                layers.Add(ParseMetadataBlock(fileStream, layerAddress));
            }
            page[Constants.KEY_LAYERS] = layers;

            return page;
        }

        internal override List<int> GetPageAddresses(Dictionary<string, object> footer)
        {
            IEnumerable<string> pageKeys = footer.Keys.Where(p => p.StartsWith("PAGE"));

            return pageKeys.Select(p => Int32.Parse((string)footer[p])).ToList();
        }

        private static List<int> GetItemAddresses(Dictionary<string, object> footer, string itemName)
        {
            IEnumerable<string> itemKeys = footer.Keys.Where(p => p.StartsWith(itemName));
            List<int> itemAddresses = new List<int>();

            foreach (string itemKey in itemKeys)
            {
                if (footer[itemKey] is List<string> itemList)
                    itemAddresses.AddRange(itemList.Select(p => Convert.ToInt32(p)));
                else
                    itemAddresses.Add(Convert.ToInt32(footer[itemKey]));
            }

            return itemAddresses;
        }
    }
}