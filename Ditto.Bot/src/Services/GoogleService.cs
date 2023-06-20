using System;
using Google;
using Google.Apis.Services;
using System.Threading.Tasks;
using Google.Apis.Customsearch.v1;
using Google.Apis.Urlshortener.v1;

namespace Ditto.Bot.Services
{
    public class GoogleService : IDittoService
    {
        public BaseClientService.Initializer BaseClientService { get; private set; }
        public UrlshortenerService UrlShortenerService { get; private set; }
        public CustomsearchService CustomSearchService { get; private set; }

        public Task Connected()
        {
            try
            {
                BaseClientService = new BaseClientService.Initializer()
                {
                    ApplicationName = "Ditto",
                    ApiKey = Ditto.Settings.Credentials.GoogleApiKey
                };

                UrlShortenerService = new UrlshortenerService(BaseClientService);
                CustomSearchService = new CustomsearchService(BaseClientService);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return Task.CompletedTask;
        }

        public Task Exit() => Task.CompletedTask;

        public Task Initialised() => Task.CompletedTask;
    }
}
