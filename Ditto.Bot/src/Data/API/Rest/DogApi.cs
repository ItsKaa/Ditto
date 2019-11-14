using Ditto.Data;
using Ditto.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ditto.Bot.Data.API.Rest
{
    public class DogApi : RestBase<DogApi.ImageResult>
    {
        public class ListResult
        {
            public string Status { get; set; }
            public Dictionary<string, List<string>> Message { get; set; }
        }
        public class ImageResult
        {
            public string Status { get; set; }
            public Uri Message { get; set; }
        }

        public DogApi()
        {
            BaseUrl = new Uri("https://dog.ceo/api/");
        }

        public Dictionary<string, List<string>> GetBreeds()
        {
            return Call<ListResult>("breeds/list/all", null)?.Message;
        }

        public Uri Random(string breed = "", string subBreed = "")
        {
            // breeds/image/random
            // breed/{breed name}/images/random
            // breed/{breed name}/{sub-breed name}/images/random
            if (breed == string.Empty && subBreed == string.Empty)
            {
                return Call("breeds/image/random", null)?.Message;
            }
            else
            {
                var foundBreed = false;
                var breeds = GetBreeds();
                string masterBreed = "";

                if(breeds.TryGetValue(breed, out List<string> subBreeds))
                {
                    foundBreed = true;
                    masterBreed = breed;
                    if(subBreed.Length > 0 && !subBreeds.Contains(subBreed))
                    {
                        subBreed = "";
                        foundBreed = false;
                    }
                }
                else
                {
                    // Try the opposite
                    if(breeds.TryGetValue(subBreed, out List<string> subBreeds2))
                    {
                        foundBreed = true;
                        masterBreed = subBreed;
                        if (subBreed.Length > 0 && !subBreeds2.Contains(breed))
                        {
                            breed = "";
                            foundBreed = false;
                        }
                    }
                }
                if (foundBreed)
                {
                    return Call($"breed/{masterBreed}{(subBreed != "" ? $"/{subBreed}" : "")}/images/random", null)?.Message;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
