using System;
using System.Collections.Generic;

namespace SupernoteSharp.Entities
{
    public class Link
    {
        public Dictionary<string, object> Metadata { get; private set; }
        public byte[] Content { get; set; }
        public int PageNumber { get; set; }
        public int Position { get; private set; }
        public int Type { get; private set; }
        public int InOut { get; private set; }
        public Tuple<int, int, int, int> Rect { get; private set; }
        public string Timestamp { get; private set; }
        public string FilePath { get; private set; }
        public string FileId { get; private set; }
        public string PageId { get; private set; }

        public Link(Dictionary<string, object> metadata)
        {
            Metadata = metadata;
            Content = null;
            PageNumber = 0;
            Position = Int32.Parse(((string)metadata["LINKRECT"]).Split(',')[1]); // get top value from "left,top,width,height"
            Type = Int32.Parse((string)metadata["LINKTYPE"]);
            InOut = Int32.Parse((string)metadata["LINKINOUT"]);

            // rectangle
            string[] rectangle = ((string)metadata["LINKRECT"]).Split(',');
            int left = int.Parse(rectangle[0]);
            int top = int.Parse(rectangle[1]);
            int width = int.Parse(rectangle[2]);
            int height = int.Parse(rectangle[3]);
            Rect = new Tuple<int, int, int, int>(left, top, left + width, top + height);

            Timestamp = (string)metadata["LINKTIMESTAMP"];
            FilePath = (string)metadata["LINKFILE"];
            FileId = (string)metadata["LINKFILEID"] == "none" ? String.Empty : (string)metadata["LINKFILEID"];
            PageId = (string)metadata["PAGEID"] == "none" ? String.Empty : (string)metadata["PAGEID"];
        }
    }
}