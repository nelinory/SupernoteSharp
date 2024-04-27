using SupernoteSharp.Common;
using SupernoteSharp.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SupernoteSharp.Business
{
    internal class SupernoteParser
    {
        internal virtual string SN_SIGNATURE_PATTERN
        {
            get
            {
                return @"SN_FILE_ASA_\d{8}";
            }
        }

        internal virtual List<String> SN_SIGNATURES
        {
            get
            {
                return new List<string>
                {
                    "SN_FILE_ASA_20190529"
                };
            }
        }

        internal Metadata ParseStream(FileStream fileStream, Policy policy)
        {
            // check file signature
            (string matchedSignature, string foundSigniture) = FindMatchingSignature(fileStream);
            if (String.IsNullOrEmpty(matchedSignature) == true)
            {
                bool isCompatibleSignature = CheckSignatureCompatible(fileStream);
                if (policy == Policy.Strict || isCompatibleSignature == false)
                    throw new UnsupportedFileFormatException($"Signature found: {foundSigniture}");
                else
                    matchedSignature = SN_SIGNATURES[SN_SIGNATURES.Count - 1]; // treat as latest supported signature
            }

            // parse footer block
            fileStream.Seek(-Constants.ADDRESS_SIZE, SeekOrigin.End); // footer address is located at last 4-bytes
            byte[] data = new byte[Constants.ADDRESS_SIZE];
            fileStream.Read(data, 0, Constants.ADDRESS_SIZE);
            int footerAddress = BitConverter.ToInt32(data, 0);
            Dictionary<string, object> footer = ParseFooterBlock(fileStream, footerAddress);

            // parse header block
            int headerAddress = Int32.Parse((string)footer["FILE_FEATURE"]);
            Dictionary<string, object> header = ParseMetadataBlock(fileStream, headerAddress);

            // parse page block
            List<int> pageAddresses = GetPageAddresses(footer);
            List<Dictionary<string, object>> pages = new List<Dictionary<string, object>>();
            foreach (int pageAddress in pageAddresses)
            {
                pages.Add(ParsePageBlock(fileStream, pageAddress));
            }

            Metadata result = new Metadata();
            result.Signature = matchedSignature;
            result.Header = header;
            result.Footer = footer;
            result.Pages = pages;

            return result;
        }

        internal virtual Dictionary<string, object> ParseFooterBlock(FileStream fileStream, int footerAddress)
        {
            return ParseMetadataBlock(fileStream, footerAddress);
        }

        internal virtual Dictionary<string, object> ParsePageBlock(FileStream fileStream, int pageAddress)
        {
            return ParseMetadataBlock(fileStream, pageAddress);
        }

        internal virtual List<int> GetPageAddresses(Dictionary<string, object> footer)
        {
            List<int> pageAddresses = new List<int>();

            if (footer.ContainsKey("PAGE") == true)
            {
                if (footer["PAGE"] is List<object>)
                    pageAddresses = ((List<object>)footer["PAGE"]).Select(p => Convert.ToInt32(p)).ToList();
                else
                    pageAddresses = new List<int> { Convert.ToInt32(footer["PAGE"]) };
            }

            return pageAddresses;
        }

        internal Dictionary<string, object> ParseMetadataBlock(FileStream fileStream, int address)
        {
            if (address == 0)
                return new Dictionary<string, object>();

            fileStream.Seek(address, SeekOrigin.Begin);
            byte[] blockSizeBytes = new byte[Constants.LENGTH_FIELD_SIZE];
            fileStream.Read(blockSizeBytes, 0, Constants.LENGTH_FIELD_SIZE);
            int blockSize = BitConverter.ToInt32(blockSizeBytes, 0);
            byte[] blockBytes = new byte[blockSize];
            fileStream.Read(blockBytes, 0, blockSize);

            return ExtractParameters(Encoding.UTF8.GetString(blockBytes));
        }

        private (string matchedSignature, string foundSigniture) FindMatchingSignature(FileStream fileStream)
        {
            string matchedSignature = String.Empty;
            string foundSigniture = String.Empty;

            foreach (string signature in SN_SIGNATURES)
            {
                byte[] data = new byte[signature.Length];
                try
                {
                    fileStream.Seek(0, SeekOrigin.Begin);
                    fileStream.Read(data, 0, signature.Length);

                    foundSigniture = Encoding.UTF8.GetString(data);

                    if (signature.Equals(foundSigniture))
                        return (signature, foundSigniture);
                }
                catch
                {
                    // try next signature
                }
            }

            return (matchedSignature, foundSigniture);
        }

        private bool CheckSignatureCompatible(FileStream fileStream)
        {
            bool result = false;

            byte[] data = new byte[SN_SIGNATURES[SN_SIGNATURES.Count - 1].Length];
            try
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.Read(data, 0, SN_SIGNATURES[0].Length);
                string signature = Encoding.UTF8.GetString(data);

                if (Regex.IsMatch(signature, SN_SIGNATURE_PATTERN))
                    return true;
                else
                    return false;
            }
            catch
            {
                result = false;
            }
            finally
            {
                fileStream.Seek(0, SeekOrigin.Begin);
            }

            return result;
        }

        private Dictionary<string, object> ExtractParameters(string metadata)
        {
            string pattern = @"<([^:<>]+):([^:<>]*)>";
            MatchCollection result = Regex.Matches(metadata, pattern);
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            foreach (Match m in result)
            {
                string key = m.Groups[1].Value;
                string value = m.Groups[2].Value;

                if (parameters.ContainsKey(key) == true)
                {
                    // the key is duplicate
                    if (parameters[key].GetType() != typeof(List<string>))
                    {
                        // To store duplicate parameters, we transform data structure
                        // from {key: value} to {key: [value1, value2, ...]}
                        object firstValue = parameters[key];
                        parameters.Remove(key);
                        parameters.Add(key, new List<string> { firstValue.ToString(), value });
                    }
                    else
                    {
                        // Data structure have already been transformed
                        // We simply append new value to the list
                        ((List<string>)parameters[key]).Add(value);
                    }
                }
                else
                    parameters.Add(key, value);
            }

            return parameters;
        }
    }
}