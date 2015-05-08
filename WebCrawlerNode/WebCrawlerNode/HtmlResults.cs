using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace WebCrawlerNode
{
    public class HtmlResults : Serializable
    {
        [BsonId]
        public ObjectId resultsid { get; set; }
        public string htmlTags { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.Dynamic)]
        public Dictionary<string, Link> links { get; set; }
        public HashSet<ObjectId> owners { get; set; }
        [BsonIgnore]
        private List<ChildPage> _childPages = new List<ChildPage>();
        [BsonIgnore]
        protected List<ChildPage> childPages
        {
            get { lock (_childPages) { return _childPages; } }
        }

        public void AddChildPage(ChildPage page)
        {
            childPages.Add(page);
        }

        public HashSet<string> FilterByTags(string html, Uri domain)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var hrefs = htmlDoc.DocumentNode.QuerySelectorAll(htmlTags).ToDictionary(k => FixUrl(k.Attributes["href"].Value, domain));
                HashSet<string> set = new HashSet<string>();
                
                foreach (string href in hrefs.Keys)
                {
                    links[href] = new Link(href, hrefs[href].InnerText);
                    set.Add(href);
                }

                return set;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                throw ex;
            }
        }

        private string FixUrl(string url, Uri domain)
        {
            Uri fixedUri = null;
            if (Uri.TryCreate(domain, url, out fixedUri))
            {
                return fixedUri.OriginalString;
            }
            else
            {
                Match regx = Regex.Match(url, @"\.[a-z]{2,3}(\.[a-z]{2,3})?");
                if (!regx.Success)
                {
                    return domain.Scheme + "://" + domain.Host + url;
                }
                else
                {
                    return url;
                }
            }
        }

        private HashSet<string> FixUrls(HashSet<string> urls, Uri domain)
        {
            urls.Remove(domain.AbsoluteUri);

            HashSet<string> fixedUrlSet = new HashSet<string>();
            foreach (string url in urls)
            {
                Uri fixedUri;
                if (Uri.TryCreate(domain, url, out fixedUri))
                {
                    fixedUrlSet.Add(fixedUri.OriginalString);
                }
                else
                {
                    Match regx = Regex.Match(url, @"\.[a-z]{2,3}(\.[a-z]{2,3})?");
                    if (!regx.Success)
                    {
                        fixedUrlSet.Add(domain.Scheme + "://" + domain.Host + url);
                    }
                }
            }

            return fixedUrlSet;
        }

        public void ProcessKeywordScores()
        {
            foreach (ChildPage page in childPages)
            {
                if (page.htmlDoc != null)
                {
                    string innerText = page.htmlDoc.DocumentNode.InnerText.ToLower();
                    MatchCollection matches = Regex.Matches(innerText, @"\b[\w']*\b");
                    var words = from m in matches.Cast<Match>()
                                where !string.IsNullOrEmpty(m.Value)
                                select m.Value;

                    Dictionary<string, int> wordCount = new Dictionary<string, int>();
                    foreach (string word in words)
                    {
                        if(innerText.Contains(word))
                        {
                            wordCount[word] = Regex.Matches(innerText, word).Count;
                        }
                    }

                    links[page.domain.OriginalString].AddWordCount(wordCount);
                }
            }
        }
    }

    public class Link
    {
        public string url { get; set; }
        public string innerText { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        private Dictionary<string, int> wordCount { get; set; }

        public Link(string url, string innerText)
        {
            this.url = url;
            this.innerText = innerText;
        }

        public void AddWordCount(Dictionary<string, int> wordCount)
        {
            this.wordCount = wordCount;
        }
    }
}
