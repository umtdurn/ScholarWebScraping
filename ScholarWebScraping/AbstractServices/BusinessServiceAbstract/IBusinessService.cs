using System;
using HtmlAgilityPack;

namespace ScholarWebScraping.AbstractServices.BusinessServiceAbstract
{
	public interface IBusinessService
	{
		public HtmlDocument RequestForScholar(string url, int pageNumber, string input);

	}
}

