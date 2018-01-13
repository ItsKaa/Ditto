using Discord;

namespace Ditto.Extensions
{
    public static class RequestOptionsExtensions
    {
        public static RequestOptions SetRetryMode(this RequestOptions @this, RetryMode? retryMode)
        {
            @this.RetryMode = retryMode;
            return @this;
        }
    }
}
