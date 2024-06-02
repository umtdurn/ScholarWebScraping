using System;
namespace ScholarWebScraping.AbstractServices.MongoDbAbstract
{
	public interface IMongoDbService
	{
        Task AddDocumentAsync<T>(string collectionName, T document);

        Task<List<T>> GetAllDocumentsAsync<T>(string collectionName);

        Task<T> GetDocumentByIdAsync<T>(string collectionName, string id);

        Task UpdateDocumentAsync<T>(string collectionName, string id,T document);

        Task DeleteDocumentAsync<T>(string collectionName, string id);

        Task<List<T>> GetDocumentBySearchKeywordAsync<T>(string collectionName, string keyword);

    }
}

