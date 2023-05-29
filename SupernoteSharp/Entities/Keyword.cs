using System;
using System.Collections.Generic;

namespace SupernoteSharp.Entities
{
    public class Keyword
    {
        public Dictionary<string, object> Metadata { get; private set; }
        public byte[] Content { get; set; }
        public int PageNumber { get; private set; }
        public int Position { get; private set; }

        public Keyword(Dictionary<string, object> metadata)
        {
            Metadata = metadata;
            Content = null;

            PageNumber = Int32.Parse((string)metadata["KEYWORDPAGE"]);
            Position = Int32.Parse(((string)metadata["KEYWORDRECT"]).Split(',')[1]); // get top value from "left,top,width,height"
        }
    }
}