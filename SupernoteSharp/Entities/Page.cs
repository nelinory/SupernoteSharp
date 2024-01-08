using SupernoteSharp.Common;
using System;
using System.Collections.Generic;

namespace SupernoteSharp.Entities
{
    public class Page
    {
        public Dictionary<string, object> Metadata { get; private set; }
        public byte[] Content { get; set; }
        public bool IsLayerSupported { get; private set; }
        public List<Layer> Layers { get; private set; }
        public string Protocol { get; private set; }
        public string Style { get; private set; }
        public string StyleHash { get; private set; }
        public string LayerInfo { get; private set; }
        public List<string> LayerOrder { get; private set; }
        public byte[] TotalPath { get; set; }
        public string PageId { get; private set; }
        public int RecognStatus { get; private set; }
        public byte[] RecognFile { get; set; }
        public byte[] RecognText { get; set; }
        public byte[] ExternalLinkInfo { get; set; }

        public Page(Dictionary<string, object> metadata)
        {
            Metadata = metadata;
            Content = null;
            IsLayerSupported = metadata.ContainsKey(Constants.KEY_LAYERS);

            // layers
            Layers = new List<Layer>();
            if (IsLayerSupported == true)
            {
                foreach (Dictionary<string, object> layer in (List<Dictionary<string, object>>)metadata[Constants.KEY_LAYERS])
                {
                    // always add layer info even if it is null
                    Layers.Add(layer.Count == 0 ? null : new Layer(layer));
                }
            }

            Protocol = (IsLayerSupported == true) ? Layer(0).Protocol : (string)metadata["PROTOCOL"];
            Style = (string)metadata["PAGESTYLE"];
            StyleHash = (string)metadata["PAGESTYLEMD5"] == "0" ? String.Empty : (string)metadata["PAGESTYLEMD5"];

            // layer info
            string layerInfo = (string)metadata["LAYERINFO"];
            if (String.IsNullOrEmpty(layerInfo) == true || layerInfo == "none")
                LayerInfo = String.Empty;
            else
                LayerInfo = layerInfo.Replace("#", ":");

            // layer order
            string layerSequence = (string)metadata["LAYERSEQ"];
            if (String.IsNullOrEmpty(layerSequence) == true)
                LayerOrder = new List<string>();
            else
                LayerOrder = new List<string>(layerSequence.Split(','));

            PageId = (string)metadata["PAGEID"];
            RecognStatus = Int32.Parse((string)metadata["RECOGNSTATUS"]);
        }

        public Layer Layer(int layerNumber)
        {
            if (layerNumber < 0 || layerNumber >= Layers.Count)
                throw new ArgumentException($"Layer number out of range: {layerNumber}");

            return Layers[layerNumber];
        }
    }
}