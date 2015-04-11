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
    [BsonKnownTypes(typeof(HtmlResults))]
    public abstract class UserResultsBase
    {
        public Uri domain { get; set; }
        public List<string> htmlTags { get; set; }
    }

    //[BsonIgnoreExtraElements]
    [BsonKnownTypes(typeof(LinkFeed), typeof(TextUpdate))]
    public class HtmlResults : UserResultsBase, Serializable
    {
        public ObjectId id { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        protected Dictionary<ObjectId, LinkOwner> linkOwners { get; set; }
        [BsonIgnore]
        private List<ChildPage> _childPages = new List<ChildPage>();
        [BsonIgnore]
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

    //[BsonKnownTypes(typeof(LinkFeedResults))]
    public class LinkFeed : HtmlResults, Serializable
    {
        public HashSet<string> keywords { get; set; }
        //[BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        //public Dictionary<string, int> keywordScores { get; set; }
        //[BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        //private Dictionary<ObjectId, FeedOwner> feedOwners { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, List<ObjectId>> keywordOwners { get; set; }

        public HashSet<string> FilterByTags(string html)
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

                HashSet<string> links = new HashSet<string>(queryResults);
                links = FixUrls(links);

                return links;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        private HashSet<string> FixUrls(HashSet<string> urls)
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

            return fixedUrlSet;
        }

        public void ProcessKeywordScores()
        {
            foreach(ChildPage page in childPages)
            {
                if(page.htmlDoc != null)
                {
                    string innerText = page.htmlDoc.DocumentNode.InnerText.ToLower();
                    foreach(KeyValuePair<string, List<ObjectId>> keyword in keywordOwners)
                    {
                        int keywordScore = 0;
                        if (innerText.Contains(keyword.Key.ToLower()))
                        {
                            keywordScore = Regex.Matches(innerText, keyword.Key).Count;
                            foreach (ObjectId userid in keyword.Value)
                            {
                                if(!(linkOwners[userid] as FeedOwner).userPageRank.ContainsKey(page.domain.AbsoluteUri))
                                {
                                    (linkOwners[userid] as FeedOwner).userPageRank.Add(page.domain.AbsoluteUri, keywordScore);
                                }
                                else
                                {
                                    (linkOwners[userid] as FeedOwner).userPageRank[page.domain.AbsoluteUri] += keywordScore;
                                }
                            }
                        }
                    }
                }
            }
        }

        /*public void RankByKeywords()
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
        }*/
    }

    /*public class LinkFeedResults : LinkFeed, Serializable
    {
        public List<ObjectId> users { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, int> rankedResults { get; set; }

        
    }*/

    //[BsonKnownTypes(typeof(TextUpdateResults))]
    public class TextUpdate : HtmlResults, Serializable
    {
        public string previousText { get; set; }
        public string currentText { get; set; }

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

                bool change = false;
                if (results.Count() == 0)
                {
                    change = true;
                }
                else if (results.Count() == 1)
                {
                    currentText = results.Single(x => x != null);
                    if (currentText != previousText)
                        change = true;
                    else
                        change = false;
                }
                else
                {
                    // Multiple elements returned, tags need to be adjusted to be more precise in finding content
                }

                foreach(TextOwner textOwner in linkOwners.Values)
                {
                    textOwner.changeInContent = change;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }
    }

    [BsonKnownTypes(typeof(FeedOwner), typeof(TextOwner))]
    public class LinkOwner : UserResultsBase
    {
        public ObjectId resultsid { get; set; }
        public ObjectId userid { get; set; }
        public bool changeInContent { get; set; }
    }

    public class FeedOwner : LinkOwner
    {
        public HashSet<string> keywords { get; set; }
        public Dictionary<string, int> userPageRank { get; set; }

    }

    public class TextOwner : LinkOwner
    {
        public string previousText { get; set; }
    }
}
