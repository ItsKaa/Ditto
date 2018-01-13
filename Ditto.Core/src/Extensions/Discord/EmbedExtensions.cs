using Discord;

namespace Ditto.Extensions
{
    public static class EmbedExtensions
    {
        public static EmbedBuilder ToBuilder(this Embed embed)
        {
            var builder = new EmbedBuilder();
            if(embed.Author.HasValue)
            {
                builder.WithAuthor(new EmbedAuthorBuilder()
                {
                    IconUrl = embed.Author.Value.IconUrl,
                    Name = embed.Author.Value.Name,
                    Url = embed.Author.Value.Url
                });
            }
            if(embed.Color.HasValue)
            {
                builder.WithColor(embed.Color.Value);
            }
            builder.WithDescription(embed.Description);
            foreach(var field in embed.Fields)
            {
                builder.AddField(new EmbedFieldBuilder()
                {
                    IsInline = field.Inline,
                    Name = field.Name,
                    Value = field.Value
                });
            }
            if(embed.Footer.HasValue)
            {
                builder.WithFooter(new EmbedFooterBuilder()
                {
                    IconUrl = embed.Footer.Value.IconUrl,
                    Text = embed.Footer.Value.Text
                });
            }
            if(embed.Image.HasValue)
            {
                builder.WithImageUrl(embed.Image.Value.Url);
            }
            if(embed.Thumbnail.HasValue)
            {
                builder.WithThumbnailUrl(embed.Thumbnail.Value.Url);
            }
            if(embed.Timestamp.HasValue)
            {
                builder.WithTimestamp(embed.Timestamp.Value);
            }
            builder.WithTitle(embed.Title);
            builder.WithUrl(embed.Url);
            //if(embed.Video.HasValue)
            //{
            //}
            return builder;
        }
        
        //public static Embed WithImage(this Embed @this, string url, int? width = null, int? height = null)
        //{
        //    @this.Image = new EmbedImage(url, null, height, width);
        //    return @this;
        //}
        //public static Embed WithVideo(this Embed @this, string url, int? width = null, int? height = null)
        //{
        //    @this.Video = new EmbedVideo(url, height, width);
        //    return @this;
        //}

    }
}
