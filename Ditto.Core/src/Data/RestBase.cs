using Microsoft.Extensions.Options;
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
        public class Parameter
        {
            public string Name { get; set; }
            public object Value { get; set; }

            public Parameter(string name, object value)
            {
                Name = name;
                Value = value;
            }
        }

        protected RestClient Client { get; private set; }
        protected RestClientOptions Options { get; } = new RestClientOptions()
        {
            Encoding = Encoding.UTF8,
            UserAgent = "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36",
        };

        protected IAuthenticator Authenticator
        {
            get => Options.Authenticator;
            set
            {
                Options.Authenticator = value;
                Client = new RestClient(Options);
            }
        }

        protected Uri BaseUrl
        {
            get => Options.BaseUrl;
            set
            {
                Options.BaseUrl = value;
                Client = new RestClient(Options);
            }
        }

        protected RestBase()
        {
            Client = new RestClient(Options);
        }

        protected T Call<T>(string resource, IEnumerable<Parameter> parameters = null, Method method = Method.Get)
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
        protected T Call<T>(string resource, Parameter[] parameters = null, Method method = Method.Get)
            where T : new()
            => Call<T>(resource, parameters?.ToList(), method);
    }

    public abstract class RestBase<T> : RestBase
        where T : new()
    {
        public RestBase() : base()
        {
        }

        protected T Call(string resource, IEnumerable<Parameter> parameters = null, Method method = Method.Get)
            => Call<T>(resource, parameters, method);

        protected T Call(string resource, Parameter[] parameters = null, Method method = Method.Get)
            => Call(resource, parameters?.ToList(), method);
    }
}
