using System;
using Google;
using Google.Apis.Services;
using System.Threading.Tasks;
using Google.Apis.Customsearch.v1;
using Google.Apis.Urlshortener.v1;

namespace Ditto.Bot.Services
{
    public partial class GoogleService
    {
        private BaseClientService.Initializer _baseClientService;
        private UrlshortenerService _urlShortenerService { get; set; }
        private CustomsearchService _customSearchService { get; set; }
        public YoutubeService Youtube { get; private set; }
        
        public GoogleService()
        {
            Youtube = new YoutubeService();
        }
        
        // TODO: Move apiKey to database
        public virtual async Task SetupAsync(string apiKey)
        {
            try
            {
                _baseClientService = new BaseClientService.Initializer()
                {
                    ApplicationName = "Ditto",
                    ApiKey = apiKey
                };

                _urlShortenerService = new UrlshortenerService(_baseClientService);
                _customSearchService = new CustomsearchService(_baseClientService);
                await Youtube.SetupAsync(_baseClientService);
            
                // Test method
                var test = await Youtube.GetPlaylistNameAsync("0");
            }
            catch (Exception ex)
            {
                if(ex is GoogleApiException)
                {
                    throw ex;
                }
            }
        }
    }
}
