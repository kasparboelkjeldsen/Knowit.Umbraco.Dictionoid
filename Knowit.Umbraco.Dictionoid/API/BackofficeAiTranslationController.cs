using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Knowit.Umbraco.Dictionoid.Models;
using Knowit.Umbraco.Dictionoid.Services;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Controllers;


namespace Knowit.Umbraco.Dictionoid.API
{
    public class BackofficeAiTranslationController : UmbracoApiController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IDictionoidService _dictionoidService;

        public BackofficeAiTranslationController(IWebHostEnvironment webHostEnvironment, IDictionoidService dictionoidService)
        {
            _webHostEnvironment = webHostEnvironment;
            _dictionoidService = dictionoidService;
        }

        [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        [HttpPost("umbraco/backoffice/dictionoid/translate")]
        public async Task<IActionResult> Translate([FromBody] TranslationRequest request)
        {
            return Ok(await _dictionoidService.Translate(request));
        }

        [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        [HttpGet("umbraco/backoffice/dictionoid/clearcache")]
        public async Task<IActionResult> ClearCache()
        {
            _dictionoidService.ClearCache();
            return Ok();
        }
		[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
		[HttpGet("umbraco/backoffice/dictionoid/isaidisabled")]
		public async Task<IActionResult> IsAiDisabled()
		{
			return Ok(_dictionoidService.IsAiDisabled());
		}

		[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        [HttpGet("umbraco/backoffice/dictionoid/gettext")]
        public async Task<IActionResult> GetText(string key)
        {
            return Ok(_dictionoidService.GetText(key));
        }

        [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        [HttpGet("umbraco/backoffice/dictionoid/getall")]
        public async Task<IActionResult> GetAllTexts(string key)
        {
            return Ok(_dictionoidService.CacheEntireDictionary());
        }

        [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        [HttpGet("umbraco/backoffice/dictionoid/shouldcleanup")]
        public async Task<IActionResult> ShouldCleanup()
        {
            return Ok(_dictionoidService.ShouldCleanup());
        }

        [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        [HttpGet("umbraco/backoffice/dictionoid/cleanupinspect")]
        public async Task<IActionResult> CleanupInspect()
        {
            var changes = await _dictionoidService.CleanupInspect(_webHostEnvironment.ContentRootPath);
            return changes is null ? Ok(false) : Ok(changes);
        }

        [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        [HttpGet("umbraco/backoffice/dictionoid/history")]
        public async Task<IActionResult> GetDictionoidHistory(string key)
        {
            return Ok(_dictionoidService.GetDictionoidHistory(key));
        }

        [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        [HttpGet("umbraco/backoffice/dictionoid/clearhistory")]
        public async Task<IActionResult> ClearHistory()
        {
            return Ok(_dictionoidService.ClearHistory());
        }

    }
}