using SupernoteSharp.Common;
using System;
using System.Collections.Generic;

namespace SupernoteSharp.Entities
{
    public class Notebook
    {
        public Metadata Metadata { get; private set; }
        public string Signature { get; private set; }
        public Cover Cover { get; private set; }
        public List<Keyword> Keywords { get; private set; }
        public List<Title> Titles { get; private set; }
        public List<Link> Links { get; private set; }
        public List<Page> Pages { get; private set; }
        public string FileId { get; private set; }
        public bool IsRealtimeRecognition { get; private set; }
        public int TotalPages { get { return Pages.Count; } }

        public Notebook(Metadata metadata)
        {
            Metadata = metadata;
            Signature = metadata.Signature;
            Cover = new Cover();

            Keywords = new List<Keyword>();
            bool hasKeywords = metadata.Footer.ContainsKey(Constants.KEY_KEYWORDS);
            if (hasKeywords == true)
            {
                foreach (Dictionary<string, object> keyword in (List<Dictionary<string, object>>)metadata.Footer[Constants.KEY_KEYWORDS])
                {
                    Keywords.Add(new Keyword(keyword));
                }
            }

            Titles = new List<Title>();
            bool hasTitles = metadata.Footer.ContainsKey(Constants.KEY_TITLES);
            if (hasTitles == true)
            {
                foreach (Dictionary<string, object> title in (List<Dictionary<string, object>>)metadata.Footer[Constants.KEY_TITLES])
                {
                    Titles.Add(new Title(title));
                }
            }

            Links = new List<Link>();
            bool hasLinks = metadata.Footer.ContainsKey(Constants.KEY_LINKS);
            if (hasLinks == true)
            {
                foreach (Dictionary<string, object> link in (List<Dictionary<string, object>>)metadata.Footer[Constants.KEY_LINKS])
                {
                    Links.Add(new Link(link));
                }
            }

            Pages = new List<Page>();
            for (int i = 0; i < metadata.TotalPages; i++)
            {
                Pages.Add(new Page(metadata.Pages[i]));
            }

            FileId = (string)metadata.Header["FILE_ID"];
            IsRealtimeRecognition = (string)Metadata.Header["FILE_RECOGN_TYPE"] == "1";
        }

        public Page Page(int pageNumber)
        {
            if (pageNumber < 0 || pageNumber >= TotalPages)
                throw new ArgumentException($"Page number out of range: {pageNumber}");

            return Pages[pageNumber];
        }
    }
}