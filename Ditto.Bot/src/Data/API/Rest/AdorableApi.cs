using Discord;
using Ditto.Data;
using System;
using System.Collections.Generic;

namespace Ditto.Bot.Data.API.Rest
{
    public class AdorableApi : RestBase
    {
        public class ListResult
        {
            public class FaceResult
            {
                public IEnumerable<string> Eyes { get; set; }
                public IEnumerable<string> Nose { get; set; }
                public IEnumerable<string> Mouth { get; set; }
            }
            public FaceResult Face { get; set; }
        }

        public AdorableApi()
        {
            BaseUrl = new Uri("https://api.adorable.io");
        }

        public ListResult AvatarList()
        {
            return Call<ListResult>("avatars/list", null);
        }
        public Uri Avatar(string eyes, string nose, string mouth)
        {
            return Avatar(eyes, nose, mouth,
                new Color(
                    Randomizer.Static.New((byte)0, (byte)255),
                    Randomizer.Static.New((byte)0, (byte)255),
                    Randomizer.Static.New((byte)0, (byte)255)
                )
            );
        }
        public Uri Avatar(string eyes, string nose, string mouth, Color color)
        {
            return new Uri($"{BaseUrl}avatars/face/{eyes}/{nose}/{mouth}/{color.ToString().Replace("#", "")}");
        }
        public Uri RandomAvatar()
        {
            var list = AvatarList();
            return Avatar(
                Randomizer.Static.RandomEnumerableElement(list.Face.Eyes),
                Randomizer.Static.RandomEnumerableElement(list.Face.Nose),
                Randomizer.Static.RandomEnumerableElement(list.Face.Mouth)
            );
        }
    }
}
