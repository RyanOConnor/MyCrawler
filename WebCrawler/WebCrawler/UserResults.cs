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
        [DataMember][BsonId]
        public ObjectId JobId { get; set; }
        [DataMember]
        public List<ObjectId> UserIDs { get; set; }
        [DataMember]
        public Uri Domain { get; set; }
        [DataMember]
        public List<string> HtmlTags { get; set; }
        [DataMember]
        public bool ChangeInContent { get; set; }
        [BsonIgnore]
        protected List<ChildPage> childPages = new List<ChildPage>();

        public void AddChildPage(ChildPage page)
        {
            childPages.Add(page);
        }
    }

    public class LinkFeed : HtmlResults, Serializable
    {
        public List<string> Keywords { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, int> RankedResults { get; set; }

        public List<string> FilterByTags(string html)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                string query = HtmlTags[0];
                for (int i = 1; i < HtmlTags.Count - 1; i++)
                {
                    if (HtmlTags[i].ElementAt(0) == '.' || HtmlTags[i].ElementAt(0) == '#')
                    {
                        query += HtmlTags[i];
                    }
                    else
                    {
                        query += ' ' + HtmlTags[i];
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
            urls.Remove(Domain.AbsoluteUri);

            HashSet<string> fixedUrlSet = new HashSet<string>();
            foreach (string url in urls)
            {
                Uri fixedUri;
                if (Uri.TryCreate(Domain, url, out fixedUri))
                {
                    fixedUrlSet.Add(fixedUri.AbsoluteUri);
                }
                else
                {
                    // do regular expression checking and string concatenation....
                    Match regx = Regex.Match(url, @"\.[a-z]{2,3}(\.[a-z]{2,3})?");
                    if (!regx.Success)
                    {
                        fixedUrlSet.Add(Domain.Scheme + "://" + Domain.Host + url);
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
                if (page.HtmlDoc != null)
                {
                    string innerText = page.HtmlDoc.DocumentNode.InnerText.ToLower();
                    foreach (string keyword in Keywords)
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
                results.Add(new KeyValuePair<string, int>(page.Domain.AbsoluteUri, pageScore));
            }

            sortedList = from entry in results orderby entry.Value descending select entry;
            RankedResults = sortedList.ToDictionary(pair => pair.Key, pair => pair.Value);

        }
    }

    class TextUpdate : HtmlResults, Serializable
    {
        public string PreviousText { get; set; }
        public string CurrentText { get; set; }

        public void FilterByTags(string html)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                string query = HtmlTags[0];
                for (int i = 1; i < HtmlTags.Count - 1; i++)
                {
                    if (HtmlTags[i].ElementAt(0) == '.' || HtmlTags[i].ElementAt(0) == '#')
                    {
                        query += HtmlTags[i];
                    }
                    else
                    {
                        query += ' ' + HtmlTags[i];
                    }
                }

                IEnumerable<string> results = htmlDoc.DocumentNode.QuerySelectorAll(query).Select(x => x.InnerText);
                
                if(results.Count() == 0)
                {
                    ChangeInContent = true;
                }
                else if(results.Count() == 1)
                {
                    CurrentText = results.Single(x => x != null);
                    if (CurrentText != PreviousText)
                        ChangeInContent = true;
                    else
                        ChangeInContent = false;
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
