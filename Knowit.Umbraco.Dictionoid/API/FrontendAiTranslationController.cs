using Microsoft.AspNetCore.Mvc;
using Knowit.Umbraco.Dictionoid.Services;
using Umbraco.Cms.Web.Common.Controllers;

namespace Knowit.Umbraco.Dictionoid.API
{
	public class FrontendAiTranslationController : UmbracoApiController
	{
		private readonly IDictionoidService _dictionoidService;

		public FrontendAiTranslationController(IDictionoidService dictionoidService)
		{
			_dictionoidService = dictionoidService;
		}

		[HttpGet("umbraco/api/dictionoid/item")]
		public async Task<IActionResult> Item(string key, string fallBack)
		{
			var groupedResults = await _dictionoidService.GetItemGroupedResults(key, fallBack);

			return Ok(groupedResults);
		}

		[HttpGet("umbraco/api/dictionoid/items")]
		public IActionResult Items(string keyStartsWith)
		{
			var groupedResults = _dictionoidService.GetItemsGroupedResult(keyStartsWith);

			return Ok(groupedResults);
		}
	}
}