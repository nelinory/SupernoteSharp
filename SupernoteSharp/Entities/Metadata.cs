using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using SupernoteSharp.Common;

namespace SupernoteSharp.Entities
{
    public class Metadata
    {
        [JsonPropertyName(Constants.KEY_SIGNATURE)]
        public string Signature { get; set; }

        [JsonPropertyName(Constants.KEY_HEADER)]
        public Dictionary<string, object> Header { get; set; }

        [JsonPropertyName(Constants.KEY_FOOTER)]
        public Dictionary<string, object> Footer { get; set; }

        [JsonPropertyName(Constants.KEY_PAGES)]
        public List<Dictionary<string, object>> Pages { get; set; }

        [JsonIgnore]
        public int TotalPages { get { return Pages.Count; } }

        public Metadata()
        {
            Signature = String.Empty;
            Header = new Dictionary<string, object>();
            Footer = new Dictionary<string, object>();
            Pages = new List<Dictionary<string, object>>();
        }

        public bool IsLayerSupported(int pageNumber)
        {
            if (pageNumber < 0 || pageNumber >= TotalPages)
                throw new ArgumentException($"Page number out of range: {pageNumber}");

            return Pages[pageNumber].ContainsKey(Constants.KEY_LAYERS);
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}