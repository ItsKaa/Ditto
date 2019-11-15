using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Modules.Music.Data;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions.Discord;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Music
{
    [Alias("audio", "sound", "sing", "vocal", "youtube", "m")]
    public class Music : DiscordModule
    {
        private static ConcurrentDictionary<ulong, MusicPlayer> _musicPlayers { get; set; } = new ConcurrentDictionary<ulong, MusicPlayer>();

        static Music()
        {
            Ditto.Connected += () =>
            {
                return Task.CompletedTask;
            };
            Ditto.Exit += async () =>
            {
                var reconnecting = Ditto.Reconnecting;
                foreach (var musicPlayer in _musicPlayers)
                {
                    if (musicPlayer.Value != null)
                    {
                        if (!reconnecting)
                        {
                            musicPlayer.Value.Dispose();
                        }
                        else
                        {
                            await musicPlayer.Value.ReconnectAsync().ConfigureAwait(false);
                        }
                    }
                }
            };
        }

        private bool GetMusicPlayer(out MusicPlayer musicPlayer)
        {
            return _musicPlayers.TryGetValue(Context?.Guild?.Id ?? 0, out musicPlayer);
        }
        private bool DoMusicPlayer(Action<MusicPlayer> action)
        {
            if (GetMusicPlayer(out MusicPlayer musicPlayer))
            {
                action(musicPlayer);
                return true;
            }
            return false;
        }
        private async Task<bool> DoMusicPlayerAsync(Func<MusicPlayer, Task> func)
        {
            if(GetMusicPlayer(out MusicPlayer musicPlayer))
            {
                await func(musicPlayer).ConfigureAwait(false);
                return true;
            }
            return false;
        }
        private Task<bool> DoMusicPlayerAsync(Action<MusicPlayer> action)
            => DoMusicPlayerAsync(m => { action(m); return Task.CompletedTask; });



        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Global, deleteUserMessage: true)]
        public async Task Play([Multiword] string query = "", IVoiceChannel voiceChannel = null)
        {
            var musicPlayer = _musicPlayers.GetOrAdd(Context.Guild.Id, new MusicPlayer(Context));
            if (musicPlayer == null)
            {
                _musicPlayers.TryRemove(Context.Guild.Id, out MusicPlayer mp);
                if (!_musicPlayers.TryAdd(Context.Guild.Id, (musicPlayer = new MusicPlayer(Context))))
                {
                    await Context.EmbedAsync(
                        "An unexpected error occured while initialising a new music player.",
                        ContextMessageOption.ReplyWithError
                    ).DeleteAfterAsync(10).ConfigureAwait(false);
                }
            }
            
            var userVoiceChannel = Context.GuildUser.VoiceChannel;
            if(voiceChannel == null)
            {
                voiceChannel = userVoiceChannel;
            }

            if (voiceChannel == null)
            {
                await Context.EmbedAsync(
                    "Please join a voice channel or include it in the arguments of the command Play.",
                    ContextMessageOption.ReplyWithError
                ).DeleteAfterAsync(10).ConfigureAwait(false);
            }
            else
            {
                if (!await Ditto.Client.DoAsync(c => c.CanJoinChannel(voiceChannel)))
                {
                    await Context.EmbedAsync(
                        "I'm unable to join the selected voice channel.",
                        ContextMessageOption.ReplyWithError
                    ).DeleteAfterAsync(10).ConfigureAwait(false);
                }
                else
                {
                    await (musicPlayer?.ConnectAndPlayAsync(
                        voiceChannel,
                        Context.GuildUser,
                        query
                    )).ConfigureAwait(false);

                }
            }
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Global, deleteUserMessage: true)]
        public Task Stop()
        {
            return DoMusicPlayerAsync(musicPlayer =>
            {
                musicPlayer.Dispose();
                _musicPlayers.TryRemove(Context.Guild.Id, out MusicPlayer _);
            });
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Global, deleteUserMessage: true)]
        [Alias("vol"), Priority(0)]
        public Task Volume(double volume)
        {
            return DoMusicPlayerAsync(m => m.Volume = volume);
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Global, deleteUserMessage: true)]
        [Alias("vol"), Priority(1)]
        public Task Volume(string volume)
        {
            if (!string.IsNullOrEmpty(volume))
            {
                if (double.TryParse(volume, out double value))
                {
                    return Volume(value);
                }
                if (volume.Contains("%"))
                {
                    if (double.TryParse(volume.Replace("%", ""), out value))
                    {
                        return Volume(value);
                    }
                }

                if (volume.ToLower() == "save")
                {
                    // TODO: Save default volume
                }
            }
            return Task.CompletedTask;
        }


        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Global, deleteUserMessage: true)]
        public Task Skip(int amount = 1)
        {
            return DoMusicPlayerAsync(m => m.NavigateSongAsync(amount));
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Global, deleteUserMessage: true)]
        [Alias("rand")]
        public Task Random()
        {
            return DoMusicPlayerAsync(m => m.RandomSong = true);
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Global, deleteUserMessage: true)]
        [Alias("index", "goto", "go to")]
        public Task Scroll(int number)
        {
            return DoMusicPlayerAsync(m => m.ScrollToIndex(number));
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Global, deleteUserMessage: true)]
        public Task Shuffle()
        { 
            return DoMusicPlayerAsync(m => m.ShufflePlaylist());
        }
    }
}
