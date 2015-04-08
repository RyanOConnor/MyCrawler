using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using MongoDB.Bson;
using HtmlAgilityPack;
using System.Net;
using Fizzler.Systems.HtmlAgilityPack;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace WebCrawler
{
    [BsonKnownTypes(typeof(LinkFeed), typeof(TextUpdate))]
    public class HtmlResults : Serializable
    {
        [BsonId]
        public ObjectId jobId { get; set; }
        public Uri domain { get; set; }
        public List<string> htmlTags { get; set; }
        public bool changeInContent { get; set; }
        [BsonIgnore]
        private List<ChildPage> _childPages = new List<ChildPage>();
        protected List<ChildPage> childPages
        {
            get { lock (_childPages) { return _childPages; } }
        }

        public void AddChildPage(ChildPage page)
        {
            if(childPages.Any(val => val.url == page.url))
            {
                Console.WriteLine();
            }
            childPages.Add(page);
        }
    }

    [BsonKnownTypes(typeof(LinkFeedResults))]
    public class LinkFeed : HtmlResults, Serializable
    {
        public List<string> keywords { get; set; }
    }

    public class LinkFeedResults : LinkFeed, Serializable
    {
        public List<ObjectId> users { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, int> rankedResults { get; set; }

        public List<string> FilterByTags(string html)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                string query = htmlTags[0];
                for (int i = 1; i < htmlTags.Count - 1; i++)
                {
                    if (htmlTags[i].ElementAt(0) == '.' || htmlTags[i].ElementAt(0) == '#')
                    {
                        query += htmlTags[i];
                    }
                    else
                    {
                        query += ' ' + htmlTags[i];
                    }
                }

                IEnumerable<string> queryResults = htmlDoc.DocumentNode.QuerySelectorAll(query).Select(x => x.Attributes["href"].Value);

                List<string> links = new List<string>(queryResults);
                links = FixUrls(links);

                return links;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        private List<string> FixUrls(List<string> urls)
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
                    // do regular expression checking and string concatenation....
                    Match regx = Regex.Match(url, @"\.[a-z]{2,3}(\.[a-z]{2,3})?");
                    if (!regx.Success)
                    {
                        fixedUrlSet.Add(domain.Scheme + "://" + domain.Host + url);
                    }
                }
            }

            return fixedUrlSet.ToList();
        }

        public void RankByKeywords()
        {
            IOrderedEnumerable<KeyValuePair<string, int>> sortedList = null;

            List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>();

            foreach (ChildPage page in childPages)
            {
                int pageScore = 0;
                if (page.htmlDoc != null)
                {
                    string innerText = page.htmlDoc.DocumentNode.InnerText.ToLower();
                    foreach (string keyword in keywords)
                    {
                        if (innerText.Contains(keyword.ToLower()))
                        {
                            pageScore += Regex.Matches(innerText, keyword).Count;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                results.Add(new KeyValuePair<string, int>(page.domain.AbsoluteUri, pageScore));
            }

            sortedList = from entry in results orderby entry.Value descending select entry;
            rankedResults = sortedList.ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }

    [BsonKnownTypes(typeof(TextUpdateResults))]
    public class TextUpdate : HtmlResults, Serializable
    {
        public string previousText { get; set; }
        public string currentText { get; set; }
    }

    public class TextUpdateResults : TextUpdate, Serializable
    {
        public List<ObjectId> users { get; set; }

        public void FilterByTags(string html)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                string query = htmlTags[0];
                for (int i = 1; i < htmlTags.Count - 1; i++)
                {
                    if (htmlTags[i].ElementAt(0) == '.' || htmlTags[i].ElementAt(0) == '#')
                    {
                        query += htmlTags[i];
                    }
                    else
                    {
                        query += ' ' + htmlTags[i];
                    }
                }

                IEnumerable<string> results = htmlDoc.DocumentNode.QuerySelectorAll(query).Select(x => x.InnerText);

                if (results.Count() == 0)
                {
                    changeInContent = true;
                }
                else if (results.Count() == 1)
                {
                    currentText = results.Single(x => x != null);
                    if (currentText != previousText)
                        changeInContent = true;
                    else
                        changeInContent = false;
                }
                else
                {
                    // Multiple elements returned, tags need to be adjusted to be more precise in finding content
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }
    }
}
