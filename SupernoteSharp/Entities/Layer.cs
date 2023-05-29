using System.Collections.Generic;

namespace SupernoteSharp.Entities
{
    public class Layer
    {
        public Dictionary<string, object> Metadata { get; private set; }
        public byte[] Content { get; set; }
        public string Name { get; private set; }
        public string Protocol { get; private set; }

        public Layer(Dictionary<string, object> metadata)
        {
            Metadata = metadata;
            Name = (string)metadata["LAYERNAME"];
            Protocol = (string)metadata["LAYERPROTOCOL"];
        }
    }
}