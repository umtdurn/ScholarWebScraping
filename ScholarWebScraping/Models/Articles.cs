using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScholarWebScraping.Models
{
	public class Articles
	{
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string? Title { get; set; }

        public List<string>? AuthorNames { get; set; }

        public string? PublicationType { get; set; } // Araştırma makalesi, derleme, konferans, kitap vb.

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? PublicationDate { get; set; }

        public string? PublisherName { get; set; }

        public string? SearchKeywords { get; set; } // Arama motorunda aratılan anahtar kelimeler

        public List<string>? ArticleKeywords { get; set; } // Makaleye ait anahtar kelimeler

        public string? Abstract { get; set; }

        public List<string>? References { get; set; }

        public int? CitationCount { get; set; }

        public string? PdfUrl { get; set; }

        public string? Doi { get; set; } // Eğer varsa

        public string? Url { get; set; }

    }
}

