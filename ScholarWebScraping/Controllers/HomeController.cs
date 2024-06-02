using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ScholarWebScraping.AbstractServices.MongoDbAbstract;
using ScholarWebScraping.ConcreteServices.BusinessServiceConcrete;
using ScholarWebScraping.ConcreteServices.ElasticsearchConcrete;
using ScholarWebScraping.Models;

namespace ScholarWebScraping.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IMongoDbService _mongoDbService;
    private readonly IHttpClientFactory _clientFactory;
    private BusinessService _business = new BusinessService();
    private ElasticsearchService _elasticsearchService = new ElasticsearchService();

    public HomeController(IMongoDbService mongoDbService,ILogger<HomeController> logger, IHttpClientFactory clientFactory)
    {
        _logger = logger;
        _mongoDbService = mongoDbService;
        _clientFactory = clientFactory;
    }

    // [HttpPost("get-recommendations")]
    // public async Task<IActionResult> GetRecommendations([FromBody] Interest interest)
    // {
    //     var client = _clientFactory.CreateClient();
    //     var json = JsonSerializer.Serialize(interest);
    //     var data = new StringContent(json, Encoding.UTF8, "application/json");

    //     var response = await client.PostAsync("http://localhost:8000/recommendations/", data);

    //     if (response.IsSuccessStatusCode)
    //     {
    //         var result = await response.Content.ReadAsStringAsync();
    //         return Ok(result);
    //     }

    //     return BadRequest();
    // }

    public async Task<IActionResult> Index()
    {
        //List<Articles> allArticlesMongo = await _mongoDbService.GetAllDocumentsAsync<Articles>(nameof(Articles));
        List<Articles> allArticles = await _elasticsearchService.GetAllDocumentsAsync();

        return View(allArticles);
    }

    #region Eski Index Metodu
    /*
    // Sadece Google Scholar
    [HttpPost]
    public async Task<IActionResult> Index(string input)
    {
        //var url = $"https://scholar.google.com/scholar?hl=en&as_sdt=0%2C5&q={input}&btnG=&oq=deep+";
        int pageStart = 0;
        input = input.Replace(" ", "+");

        var filteredDivs = new List<HtmlNode>();

        // Schoolar arama motorunda sağ tarafta PDF olan ve sol tarafta da book, html olmayan kayıtları alıyor.
        // Ayrıca sol tarafta bir şey yoksa sağ tarafta PDF varsa alır.
        do
        {
            var url2 = $"https://scholar.google.com/scholar?start={pageStart}&q={input}&hl=en&as_sdt=0,5";
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url2);

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            if (htmlDocument == null)
            {
                TempData["error"] = "Veriler çekilirken bir hata oluştu.";
                return View();
            }

            var mainArticleDiv = htmlDocument.DocumentNode.SelectSingleNode("//div[@id='gs_res_ccl_mid']");

            var articles = mainArticleDiv.SelectNodes(".//div[@class='gs_r gs_or gs_scl']");
            foreach (var item in articles)
            {
                var isTherePdfDiv = item.SelectSingleNode(".//div[@class='gs_ggs gs_fl']");
                if (isTherePdfDiv == null)
                {
                    continue;
                }

                var isThereLeftHeaderSpan = item.SelectSingleNode(".//div[@class='gs_ri']")
                                            .SelectSingleNode(".//h3[@class='gs_rt']")
                                            .SelectSingleNode(".//span[@class='gs_ctc']");
                if (isThereLeftHeaderSpan != null)
                {
                    string leftHeaderSpanText = isThereLeftHeaderSpan.InnerText;
                    if (!leftHeaderSpanText.Contains("PDF", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                }


                var isItPdf = isTherePdfDiv.SelectSingleNode(".//a").SelectSingleNode(".//span[@class='gs_ctg2']");
                if (isItPdf != null && isItPdf.InnerText.Contains("PDF"))
                {
                    filteredDivs.Add(item);
                }

                if (filteredDivs.Count() == 10) { break; }

            }

            if (filteredDivs.Count() == 10) { break; }
            pageStart += 10;

        } while (filteredDivs.Count() < 11);


        Articles insertArticle = new Articles();

        foreach (var item in filteredDivs)
        {
            insertArticle.Title = item.SelectSingleNode(".//h3[@class='gs_rt']")
                                        .SelectSingleNode(".//a")
                                        .SelectSingleNode(".//b").InnerText;
        }

        return View();
    }
    */
    #endregion

    [HttpPost]
    public async Task<IActionResult> Index(string input)
    {
        string keywordInput = input;
        input = input.Replace(" ", "+");
        var url = $"https://scholar.google.com/scholar?hl=en&as_sdt=0%2C5&q={input}&btnG=&oq=deep+";
        

        var httpClient = new HttpClient();
        var html = await httpClient.GetStringAsync(url);

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        if (htmlDocument == null)
        {
            TempData["error"] = "Veriler çekilirken bir hata oluştu.";
            return RedirectToAction("Index", "Home");
        }

        var didYouMean = htmlDocument.DocumentNode.SelectSingleNode(".//div[@id='gs_res_ccl_top']");
        if (didYouMean != null && didYouMean.HasChildNodes)
        {
            var header2 = didYouMean.SelectSingleNode(".//h2[@class='gs_rt']");
            if(header2 is not null)
            {
                var aTagInnerText = header2.LastChild.InnerText;
                //var bTagInnerText = header2.LastChild.LastChild.LastChild.InnerText;

                if (aTagInnerText != null)
                {
                    TempData["DidYouMean"] = aTagInnerText;
                    return View("Index", new List<Articles>());
                }
            }
        }


        var insertCheck = await _mongoDbService
                            .GetDocumentBySearchKeywordAsync<Articles>(nameof(Articles), keywordInput);

        if(insertCheck is not null)
        {
            if(insertCheck.Count > 0)
            {
                return View(insertCheck);
            }
        }

        var tenArticle = await _business.TakeTenArticleAsync(input);
        if(tenArticle == null)
        {
            TempData["error"] = "Bir hata ile karşılaşıldı";
            return View("Index","Home");
        }

        List<HtmlNode> firstFive = new List<HtmlNode>();
        List<HtmlNode> secondFive = new List<HtmlNode>();

        for (int i = 0; i < 5;  i++)
        {
            firstFive.Add(tenArticle.ElementAt(i));
        }

        for (int i = 5; i < 10; i++)
        {
            secondFive.Add(tenArticle.ElementAt(i));
        }

        List<Articles> articleList = new List<Articles>();

        // Iterate first five element.
        foreach (var item in tenArticle)
        //while(articleList.Count <= 10 )
        {
            string urlForGoInto = item.SelectSingleNode(".//h5[@class='card-title']")
                                        .SelectSingleNode(".//a")
                                        .GetAttributeValue("href", string.Empty);

            var articleResponse = await httpClient.GetAsync(new Uri(urlForGoInto.Trim(), UriKind.Absolute));
            var html2 = await articleResponse.Content.ReadAsStringAsync();

            if (articleResponse.StatusCode == HttpStatusCode.Found) // 302 Found
            {
                // HTTP 302 hatası aldığınızda, ilgili değerleri null olarak ayarlayın
                continue;
            }

            /* Her bir makalenin içine gir. -- BAŞLANGIÇ -- */

            //var html2 = await httpClient.GetStringAsync(urlForGoInto.Trim());

            var htmlDocument2 = new HtmlDocument();
            htmlDocument2.LoadHtml(html2);

            /* Her bir makalenin içine gir. -- BİTİŞ -- */

            if (htmlDocument2 == null) { continue; }

            string titleValue = _business.GetTitle(htmlDocument2).Trim();

            List<string> authors = _business.GetAuthors(htmlDocument2).ToList();

            string publicationType = _business.GetPublicationType(htmlDocument2);

            DateTime publicationDate = _business.GetPublicationDate(htmlDocument2);

            string publisherName = _business.GetPublisherName(htmlDocument2);

            string keywordSearchEngine = keywordInput;

            List<string> articleKeywords = _business.GetArticleKeywords(htmlDocument2).ToList();

            string abstractText = _business.GetAbstract(htmlDocument2);

            List<string> references = _business.GetReferences(htmlDocument2).ToList();

            //int citationCount = 

            string doi = _business.GetDoiNumber(htmlDocument2);

            string pdfLink = _business.GetPdfLink(htmlDocument2);

            string urlAddress = urlForGoInto;

            int citationNumber = _business.GetCitationCount(htmlDocument2);

            Articles insertArticle = new Articles()
            {
                Title = titleValue,
                AuthorNames = authors,
                PublicationType = publicationType,
                PublicationDate = publicationDate,
                PublisherName = publisherName,
                SearchKeywords = keywordSearchEngine,
                ArticleKeywords = articleKeywords,
                Abstract = abstractText,
                References = references,
                PdfUrl = pdfLink,
                CitationCount = citationNumber,
                Doi = doi,
                Url = urlAddress
            };

            articleList.Add(insertArticle);            
            if(articleList.Count == 10)
            {
                break;
            }

        }

        // Bütün article'lara mongodb'ye kaydetme ve elasticserach'e kaydetme
        // işlemleri yapılmalı.

        foreach (var item in articleList)
        {
            await _mongoDbService.AddDocumentAsync(nameof(Articles), item);
            await _elasticsearchService.AddDocumentAsync(item);
        }


        return View(articleList);

        #region SecondFiveLoop
        /*
        // Iterate second five element.
        foreach (var item in secondFive)
        {
            string urlForGoInto = item.SelectSingleNode(".//h5[@class='card-title']")
                                        .SelectSingleNode(".//a")
                                        .GetAttributeValue("href", string.Empty);

            /* Her bir makalenin içine gir. -- BAŞLANGIÇ -- *

            var html2 = await httpClient.GetStringAsync(urlForGoInto.Trim());

            var htmlDocument2 = new HtmlDocument();
            htmlDocument2.LoadHtml(html2);

            /* Her bir makalenin içine gir. -- BİTİŞ -- 

            if (htmlDocument2 == null) { continue; }

            string titleValue = _business.GetTitle(htmlDocument2).Trim();

            List<string> authors = _business.GetAuthors(htmlDocument2).ToList();

            string publicationType = _business.GetPublicationType(htmlDocument2);

            DateTime publicationDate = _business.GetPublicationDate(htmlDocument2);

            string publisherName = _business.GetPublisherName(htmlDocument2);

            string keywordSearchEngine = keywordInput;

            List<string> articleKeywords = _business.GetArticleKeywords(htmlDocument2).ToList();

            string abstractText = _business.GetAbstract(htmlDocument2);

        }

    */
        #endregion

    }
    
    public async Task<IActionResult> DownloadPdf(string url, string title)
    {
        url = "https://dergipark.org.tr" + url;
        // URL'den PDF dosyasını indir
        var httpClient = new HttpClient();
        var pdfBytes = await httpClient.GetByteArrayAsync(url);

        // PDF dosyasının adını belirle (opsiyonel)
        var fileName = $"{title}.pdf";

        // İndirme işlemini gerçekleştir
        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> DergiPark(string input)
    {

        return View(); 
    }

    [NonAction]
    public async Task<IActionResult> GetFromDergiPark(string input)
    {
        var url = $"https://dergipark.org.tr/tr/search?q={input}&section=articles";

        var httpClient = new HttpClient();
        var html = await httpClient.GetStringAsync(url);

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        if (htmlDocument == null)
        {
            TempData["error"] = "Veriler çekilirken bir hata oluştu.";
            return View();
        }

        var mainArticleDiv = htmlDocument.DocumentNode.SelectSingleNode("//div[@id='gs_res_ccl_mid']");


        return View();
    }

    public async Task<IActionResult> ArticleDetails(string id)
    {
        var document = await _elasticsearchService.GetDocumentByIdAsync(id);
        return View(document);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

