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

                var hrefs = htmlDoc.DocumentNode.QuerySelectorAll(htmlTags)
                                                .ToDictionary(k => FixUrl(k.Attributes["href"]
                                                                           .Value, domain));
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
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Parallel.ForEach(childPages, page =>
            {
                if (page.htmlDoc != null)
                {
                    string innerText = page.htmlDoc.DocumentNode.InnerText.ToLower();
                    MatchCollection matches = Regex.Matches(innerText, @"\b[\w']*\b");
                    var words = from m in matches.Cast<Match>()
                                where !string.IsNullOrEmpty(m.Value)
                                where m.Length > 2
                                where m.Value.IndexOfAny("0123456789<>!/".ToCharArray()) == -1
                                select m.Value;

                    Dictionary<string, int> wordCount = new Dictionary<string, int>();
                    Parallel.ForEach(words, word =>
                    {
                        if (innerText.Contains(word))
                        {
                            int numOccurrances = Regex.Matches(innerText, word).Count;
                            lock (wordCount)
                            {
                                wordCount[word] = numOccurrances;
                            }
                        }
                    });

                    links[page.domain.OriginalString].AddWordCount(wordCount);
                }
            });
            sw.Stop();
            System.Diagnostics.Debug.Print("[ProcessKeywordScores]: " + sw.Elapsed.ToString());
        }

        public int GetSubstringOccurrences(string fullString, string subString)
        {
            int count = 0;
            int i = 0;
            while ((i = fullString.IndexOf(subString, i)) != -1)
            {
                i += subString.Length;
                count++;
            }
            return count;
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
