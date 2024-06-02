using System;
using System.Net.Http;
using HtmlAgilityPack;
using ScholarWebScraping.AbstractServices.BusinessServiceAbstract;

namespace ScholarWebScraping.ConcreteServices.BusinessServiceConcrete
{
	public class BusinessService
	{
        private readonly HttpClient _httpClient;

		public BusinessService()
		{
            _httpClient = new HttpClient();
		}

        public async Task<IEnumerable<HtmlNode>?>TakeTenArticleAsync(string input)
		{

            var url2 = $"https://dergipark.org.tr/tr/search?q={input}&section=articles";

            var httpClient = new HttpClient();
            var html2 = await httpClient.GetStringAsync(url2);

            var htmlDocument2 = new HtmlDocument();
            htmlDocument2.LoadHtml(html2);

            if (htmlDocument2 == null)
            {
                return null;
            }

            var mainArticleDiv = htmlDocument2.DocumentNode.SelectSingleNode(".//div[@class='article-cards']");

            if (mainArticleDiv == null || !mainArticleDiv.HasChildNodes)
            {
                return null;
            }

            //var articles = mainArticleDiv.SelectNodes(".//div[@class='card article-card dp-card-outline']").Take(10);
            var articles = mainArticleDiv.SelectNodes(".//div[@class='card article-card dp-card-outline']");
            return articles;
        }

        public async Task<HtmlDocument?> GoIntoNode(string nodeLink)
        {
            var html = await _httpClient.GetStringAsync(nodeLink);
            var htmlDocument = new HtmlDocument();
            htmlDocument.Load(html);

            if(htmlDocument == null) { return null; }

            return htmlDocument;
        }

        public string GetTitle(HtmlDocument document)
        {
            HtmlNode mainCard = document.DocumentNode.SelectSingleNode(".//div[contains(@class,'tab-pane active')]");
            var tmp = mainCard.SelectSingleNode(".//div[@class='h3 d-flex align-items-baseline']");
            if(tmp == null || !tmp.HasChildNodes) { return string.Empty; }

            var tmp1 = tmp.SelectSingleNode(".//h3[@class='article-title']");
            if (tmp1 == null || !tmp1.HasChildNodes) { return string.Empty; }

            string title = tmp1.InnerText;
            return title;

        }

        //public IEnumerable<string> GetAuthors(HtmlDocument document)
        //{
        //    HtmlNode mainCard = document.DocumentNode.SelectSingleNode(".//div[contains(@class,'tab-pane active')]");
        //    var authorNodes = mainCard.SelectNodes(".//a[@class='is-user']");
        //    if(authorNodes is not null)
        //    {
        //        List<string> authors = new List<string>();

        //        foreach (var item in authorNodes)
        //        {
        //            authors.Add(item.InnerText.Trim());
        //        }

        //        return authors;
        //    }
        //    else
        //    {
        //        List<string> authors = new List<string>();
        //        var articleAuthors = mainCard.SelectSingleNode(".//p[@class='article-authors']");
        //        var x = articleAuthors.InnerText;
        //        return x.ToList();
        //    }

        //}

        public IEnumerable<string> GetAuthors(HtmlDocument document)
        {
            HtmlNode mainCard = document.DocumentNode.SelectSingleNode(".//div[contains(@class,'tab-pane active')]");
            var pAuthorTag = mainCard.SelectSingleNode(".//p[@class='article-authors']");

            List<string> authors = new List<string>();

            if (pAuthorTag is not null)
            {
                var authorNodes = mainCard.SelectNodes(".//a[@class='is-user']");
                if (authorNodes is not null)
                {
                    foreach (var item in authorNodes)
                    {
                        authors.Add(item.InnerText.Trim());
                    }
                }

                var authorNodes2 = mainCard.SelectNodes(".//a[data-toggle='tooltip']");
                if(authorNodes2 is not null)
                {
                    foreach (var item in authorNodes2)
                    {
                        authors.Add(item.InnerText.Trim());
                    }
                }
            }

            return authors;
            
            

        }

        public string GetPublicationType(HtmlDocument document)
        {
            HtmlNode headerDiv = document.DocumentNode.SelectSingleNode(".//div[@class='kt-portlet__head-label']");
            string publicationType = headerDiv.SelectSingleNode(".//div[@class='kt-portlet__head-title']")
                                                .InnerText
                                                .Trim();

            return publicationType;
        }

        public DateTime GetPublicationDate(HtmlDocument document)
        {
            bool check = false;

            HtmlNode mainDiv = document.DocumentNode.SelectSingleNode(".//div[contains(@class,'tab-pane active')]");
            string spanAsString = mainDiv.SelectSingleNode(".//span[@class='article-subtitle']")
                                            .InnerText
                                            .ToString()
                                            .Trim();
            if(spanAsString.Length >= 11)
            {
                string dateAsString = spanAsString.Substring((spanAsString.Length - 11));
                DateTime _publicationDate;
                check = DateTime.TryParse(dateAsString, out _publicationDate);
                if (check)
                {
                    return _publicationDate;
                }
            }

            return DateTime.Now;

        }

        public string GetPublisherName(HtmlDocument document)
        {
            HtmlNode mainDiv = document.DocumentNode.SelectSingleNode(".//div[contains(@class,'kt-heading kt-align-center')]");
            var publisherHeader = mainDiv.SelectSingleNode(".//h1[@id='journal-title']")
                                            .InnerText
                                            .Trim();

            return publisherHeader;
        }

        public IEnumerable<string> GetArticleKeywords(HtmlDocument document)
        {
            List<string> articleKeywords = new List<string>();
            HtmlNode mainCard = document.DocumentNode.SelectSingleNode(".//div[contains(@class,'tab-pane active')]");
            HtmlNode keywordDiv = mainCard.SelectSingleNode(".//div[@class='article-keywords data-section']");
            if(keywordDiv is not null)
            {
                var keywords = keywordDiv.SelectSingleNode(".//p").SelectNodes(".//a");

                foreach (var item in keywords)
                {
                    articleKeywords.Add(item.InnerText.Trim());
                }

                return articleKeywords;
            }
            else
            {
                var x = string.Empty;
                articleKeywords.Add(x);
                return articleKeywords;
            }

        }

        public string GetAbstract(HtmlDocument document)
        {
            HtmlNode mainCard = document.DocumentNode.SelectSingleNode(".//div[contains(@class,'tab-pane active')]");
            HtmlNode abstractDiv = mainCard.SelectSingleNode(".//div[@class='article-abstract data-section']");
            string abstractText = abstractDiv.SelectSingleNode(".//p").InnerText.Trim();

            return abstractText;
        }

        public IEnumerable<string> GetReferences(HtmlDocument document)
        {
            List<string> references = new List<string>();
            HtmlNode mainCard = document.DocumentNode.SelectSingleNode(".//div[contains(@class,'tab-pane active')]");
            HtmlNode? referencesDiv = mainCard.SelectSingleNode(".//div[@class='article-citations data-section']");

            if(referencesDiv == null)
            {
                return new List<string>()
                {
                    "İlgili makalede kaynakça bölümü yoktur."
                };
            }

            HtmlNode listElement = referencesDiv.SelectSingleNode(".//ul[@class='fa-ul']");
            HtmlNodeCollection liElements = listElement.SelectNodes(".//li");
            foreach (var item in liElements)
            {
                references.Add(item.InnerText.Trim());
            }

            return references;
        }

        public int GetCitationCount()
        {


            return -1;
        }

        public string GetDoiNumber(HtmlDocument document)
        {
            HtmlNode mainCard = document.DocumentNode.SelectSingleNode(".//div[contains(@class,'tab-pane active')]");
            HtmlNode doiDiv = mainCard.SelectSingleNode(".//div[@class='article-doi data-section']");
            if(doiDiv == null)
            {
                return "YOK";
            }

            string doiText = doiDiv.SelectSingleNode(".//a[@class='doi-link']").InnerText.Trim();

            return doiText;
        }

        public string GetPdfLink(HtmlDocument document)
        {
            HtmlNode mainCard = document.DocumentNode.SelectSingleNode(".//div[contains(@class,'kt-portlet__body')]");
            HtmlNode pdfDiv = mainCard.SelectSingleNode(".//div[@id='article-toolbar']");
            if(pdfDiv is not null)
            {
                var pdfLink = pdfDiv.SelectSingleNode(".//a").GetAttributeValue("href", string.Empty);
                return pdfLink;
            }

            return string.Empty;
        }

        public int GetCitationCount(HtmlDocument document)
        {
            HtmlNode mainCard = document.DocumentNode.SelectSingleNode(".//div[contains(@class,'tab-pane active')]");
            HtmlNode doiDiv = mainCard.SelectSingleNode(".//div[@class='article-doi data-section']");

            if(doiDiv == null)
            {
                return 0;
            }

            HtmlNode citationDiv = doiDiv.SelectSingleNode(".//div[@class='mt-3']");
            if(citationDiv is null)
            {
                return 0;
            }

            HtmlNode citationText = citationDiv.SelectSingleNode(".//a");
            if(citationText is null) { return 0; }

            string[] citationArray = citationText.InnerText.Trim().Split(":");
            int citationNumber = 0;
            int.TryParse(citationArray.Last(), out citationNumber);

            return citationNumber;
        }
    }
}

