using System;
using System.Collections.Generic;

namespace SupernoteSharp.Entities
{
    public class Title
    {
        public Dictionary<string, object> Metadata { get; private set; }
        public byte[] Content { get; set; }
        public int PageNumber { get; set; }
        public int Position { get; private set; }

        public Title(Dictionary<string, object> metadata)
        {
            Metadata = metadata;
            Content = null;
            PageNumber = 0;
            Position = Int32.Parse(((string)metadata["TITLERECTORI"]).Split(',')[1]); // get top value from "left,top,width,height"
        }
    }
}