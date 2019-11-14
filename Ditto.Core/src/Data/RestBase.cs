using Ditto.Data;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ditto.Data
{
    public abstract class RestBase : BaseClass
    {
        protected RestClient Client { get; private set; }

        private IAuthenticator _authenticator = null;
        protected IAuthenticator Authenticator
        {
            get => _authenticator;
            set => Client.Authenticator = (_authenticator = value);
        }

        private Uri _baseUrl;
        protected Uri BaseUrl
        {
            get => _baseUrl;
            set
            {
                _baseUrl = value;
                Client.BaseUrl = _baseUrl;
            }
        }

        public RestBase()
        {
            Client = new RestClient
            {
                Encoding = Encoding.UTF8,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36 OPR/50.0.2762.67"
            };
        }

        protected T Call<T>(string resource, IEnumerable<Parameter> parameters = null, Method method = Method.GET)
            where T: new()
        {
            var request = new RestRequest(resource, method);
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    request.AddQueryParameter(p.Name, p.Value.ToString());
                }
            }
            var response = Client.Execute<T>(request);
            if (response.ErrorException != null)
            {
                const string errorMessage = "Error retrieving response.  Check inner details for more info.";
                throw new ApplicationException(errorMessage, response.ErrorException);
            }
            return response.Data;
        }
        protected T Call<T>(string resource, Parameter[] parameters = null, Method method = Method.GET)
            where T : new()
            => Call<T>(resource, parameters?.ToList(), method);
    }

    public abstract class RestBase<T> : RestBase
        where T : new()
    {
        public RestBase() : base()
        {
        }

        protected T Call(string resource, IEnumerable<Parameter> parameters = null, Method method = Method.GET)
            => Call<T>(resource, parameters, method);

        protected T Call(string resource, Parameter[] parameters = null, Method method = Method.GET)
            => Call(resource, parameters?.ToList(), method);
    }
}
