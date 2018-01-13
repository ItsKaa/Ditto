using System.Threading.Tasks;

namespace Ditto.Data.Chatting
{
    public class CleverbotSession
    {
        private string ApiKey { get; }
        private string ConversationId { get; set; } = "";

        public bool Valid => !string.IsNullOrWhiteSpace(ApiKey);

        /// <summary>
        /// Creates a Cleverbot instance.
        /// </summary>
        /// <param name="apikey">Your api key obtained from https://cleverbot.com/api/</param>
        /// <param name="sendTestMessage">Send a test message to be sure you're connected</param>
        public CleverbotSession(string apikey, bool sendTestMessage = true)
        {
            if (string.IsNullOrWhiteSpace(apikey))
            {
                //throw new Exception("You can't connect without a API key.");
                ApiKey = null;
                return;
            }

            ApiKey = apikey;

            /*
             * If you're going to work async, it might throw random errors e.g. Invocation error. 
             * Use this test message to confirm it works.
             * jeuxjeux20's note : I would suggest making test units
             */
            if (sendTestMessage)
            {
                var test = GetResponseAsync("test").GetAwaiter().GetResult();
                ConversationId = test?.ConversationId ?? ConversationId;
            }
        }

        /// <summary>
        /// Send a message to cleverbot and get a response to it. Consider using <see cref="GetResponseAsync(string)"/>
        /// </summary>
        /// <param name="message">your message sent to cleverbot</param>
        /// <returns>response from the cleverbot.com api</returns>
        //[Obsolete("Use GetResponseAsync instead.")]
        //public CleverbotResponse GetResponse(string message)
        //{
        //    if(!Valid)
        //    {
        //        return null;
        //    }

        //    var result = CleverbotResponse.CreateAsync(message, "", ApiKey).GetAwaiter().GetResult();
        //    ConversationId = result?.ConversationId ?? ConversationId;
        //    return result;
        //}

        /// <summary>
        /// Send a message to cleverbot asynchronously and get a response.
        /// </summary>
        /// <param name="message">your message sent to cleverbot</param>
        /// <returns>response from the cleverbot.com api</returns>
        public async Task<CleverbotResponse> GetResponseAsync(string message)
        {
            if (!Valid)
            {
                return null;
            }

            var result = await CleverbotResponse.CreateAsync(message, ConversationId, ApiKey).ConfigureAwait(false);
            ConversationId = result?.ConversationId ?? ConversationId;
            return result;
        }
    }
}
