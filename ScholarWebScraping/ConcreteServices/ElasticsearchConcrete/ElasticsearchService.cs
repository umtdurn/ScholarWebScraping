using System;
using Nest;
using ScholarWebScraping.Models;

namespace ScholarWebScraping.ConcreteServices.ElasticsearchConcrete
{
	public class ElasticsearchService
	{

        private readonly ElasticClient _client;

        public ElasticsearchService()
		{
            var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
                                                .DefaultIndex("articles");
            _client = new ElasticClient(settings);
        }

        public async Task AddDocumentAsync(Articles article)
        {
            var x =  await _client.IndexDocumentAsync(article);
        }

        // MongoDB'deki GetAllDocumentsAsync metodunun Elasticsearch versiyonu
        public async Task<List<Articles>> GetAllDocumentsAsync()
        {
            var searchResponse = await _client.SearchAsync<Articles>(s => s
                .MatchAll()
                .Size(10000)
            );

            return searchResponse.Documents.ToList();
        }

        // MongoDB'deki GetDocumentByIdAsync metodunun Elasticsearch versiyonu
        public async Task<Articles> GetDocumentByIdAsync(string id)
        {
            var response = await _client.GetAsync<Articles>(id);
            if (response.IsValid)
            {
                return response.Source; // Bu doğrudan Articles tipindedir.
            }
            else { return null; }
        }

        // MongoDB'deki UpdateDocumentAsync metodunun Elasticsearch versiyonu
        public async Task UpdateDocumentAsync(string id, Articles article)
        {
            await _client.UpdateAsync<DocumentPath<Articles>>(id, u => u
                .Index("articles")
                .Doc(article)
                .RetryOnConflict(3)
            );
        }

        // MongoDB'deki DeleteDocumentAsync metodunun Elasticsearch versiyonu
        public async Task DeleteDocumentAsync(string id)
        {
            await _client.DeleteAsync<DocumentPath<Articles>>(id, d => d.Index("articles"));
        }

        // MongoDB'deki GetDocumentBySearchKeywordAsync metodunun Elasticsearch versiyonu
        public async Task<List<Articles>> GetDocumentBySearchKeywordAsync(string keyword)
        {
            var searchResponse = await _client.SearchAsync<Articles>(s => s
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.SearchKeywords)
                        .Query(keyword)
                    )
                )
            );

            return searchResponse.Documents.ToList();
        }


    }
}

