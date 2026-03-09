using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OpenRCT2.API.Abstractions;

namespace OpenRCT2.API.Controllers
{
    public class LocalisationController : Controller
    {
        private static ConcurrentDictionary<int, string> CachedProgressImages = new ConcurrentDictionary<int, string>();
        private readonly HttpClient _httpClient;

        public LocalisationController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [Route("localisation/status/badges/{languageId}")]
        public async Task<IActionResult> GetBadgeStatusAsync(
            [FromServices] ILocalisationService localisationService,
            [FromRoute] string languageId)
        {
            int progress = await localisationService.GetLanguageProgressAsync(languageId);
            string progressSvg = await GetProgressImageAsync(progress);
            if (progressSvg == null)
            {
                return new StatusCodeResult(404);
            }

            return Content(progressSvg, MimeTypes.ImageSvgXml);
        }

        private async Task<string> GetProgressImageAsync(int progress)
        {
            string svg;
            if (CachedProgressImages.TryGetValue(progress, out svg))
            {
                return svg;
            }

            var response = await _httpClient.GetAsync($"http://progressed.io/bar/{progress}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            svg = await response.Content.ReadAsStringAsync();
            CachedProgressImages.TryAdd(progress, svg);
            return svg;
        }
    }
}
