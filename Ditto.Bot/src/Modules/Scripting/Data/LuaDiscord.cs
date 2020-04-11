using Discord;
using Discord.WebSocket;
using Ditto.Bot.Data.Discord;
using Ditto.Data.Commands;
using MoonSharp.Interpreter;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Scripting.Data
{
    public class LuaDiscord
    {
        public const int USER       = 1 << 0;
        public const int CHANNEL    = 1 << 1;
        public const int DM         = USER;

        public bool RoleAdded { get; set; }

        private Lazy<ICommandContextEx> Context { get; set; }
        public IGuild Guild { get; set; }
        public IRole Role { get; set; }
        public IUser User { get; set; }
        public IMessageChannel Channel { get; set; }
        public IUserMessage UserMessage { get; set; }
        public IGuildUser GuildUser => User as IGuildUser;
        
        public DateTime CurrentDate => DateTime.UtcNow;
        public TimeSpan CurrentTime => DateTime.UtcNow.TimeOfDay;

        public string ChannelId
        {
            get => Channel?.Id.ToString() ?? "0";
            private set
            {
                Channel = Ditto.Client.Do(client => client.GetChannel(Convert.ToUInt64(value))) as IMessageChannel;
            }
        }

        public TimeSpan Time(int hours, int minutes, int seconds)
        {
            return new TimeSpan(hours, minutes, seconds);
        }

        public DateTime Date(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        {
            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
        }

        public string GlobalUsername => (User?.Username ?? "") + "#" + (User?.Discriminator ?? "0000");
        public string NicknameAndGlobalUsername => $"{GuildUser?.Nickname ?? User?.Username} ({GlobalUsername})";

        static LuaDiscord()
        {
            UserData.RegisterAssembly(Assembly.GetEntryAssembly());
            var types = new[]
            {
                //typeof(String),
                typeof(IChannel),
                typeof(IMessage),
                typeof(IUserMessage),
                typeof(IGuild),
                typeof(IRole),
                typeof(IUser),
                typeof(IGuildUser),
                typeof(DateTime),
                typeof(TimeSpan),
                //typeof(LuaDiscord),
            };
            foreach (var type in types)
            {
                var desc = UserData.RegisterType(type);
            }
        }
        
        public LuaDiscord()
        {
            Context = new Lazy<ICommandContextEx>(() =>
            {
                return new CommandContextEx(Ditto.Client.Do(client => client), UserMessage)
                {
                    Channel = Channel,
                    Guild = Guild,
                    User = User
                };
            });
        }

        public void Wait(int seconds = 60)
        {
            Thread.Sleep(seconds * 60);
        }

        ~LuaDiscord()
        {
        }

        [MoonSharpHidden]
        private T GetObject<T>(string input) where T : class, ISnowflakeEntity
        {
            try
            {
                var result = Ditto.CommandHandler.ConvertObjectAsync(Context.Value, typeof(T), input).GetAwaiter().GetResult();
                return result as T;
            }
            catch { }
            return null;
        }

        [MoonSharpHidden]
        private string SetVariables(string text)
        {
            return text?

                // === USER ===
                .Replace("%user%", NicknameAndGlobalUsername ?? "<invalid user>", StringComparison.CurrentCultureIgnoreCase)
                .Replace("@user", User?.Mention ?? "<invalid user>", StringComparison.CurrentCultureIgnoreCase)

                // === CHANNEL ===
                .Replace("%channel%", Channel?.Name ?? "<invalid channel>", StringComparison.CurrentCultureIgnoreCase)
                .Replace("@channel", (Channel as ITextChannel)?.Mention ?? Channel?.Name ?? "<invalid channel>", StringComparison.CurrentCultureIgnoreCase)

                // === ROLE ===
                .Replace("%role%", Role?.Name ?? "<invalid role>", StringComparison.CurrentCultureIgnoreCase)
                .Replace("@role", Role?.Mention ?? "<invalid role>", StringComparison.CurrentCultureIgnoreCase)
                ;
        }

        public IMessage WaitForResponse(IMessage sourceMessage)
        {
            IMessage response = null;
            var waitAction = new Func<SocketMessage, Task>((SocketMessage socketMessage) =>
            {
                if(socketMessage.Channel == sourceMessage.Channel)
                {
                    response = socketMessage;
                }
                return Task.CompletedTask;
            });
            Ditto.Client.Do(client => client.MessageReceived += waitAction);
            
            while(response == null)
            {
                Thread.Sleep(100);
            }
            Ditto.Client.Do(client => client.MessageReceived -= waitAction);
            return response;
        }

        public IMessage Message(int type, string text)
        {
            IMessageChannel channel = null;
            if (type == USER)
            {
                channel = User.GetOrCreateDMChannelAsync().GetAwaiter().GetResult();
            }
            else if(type == CHANNEL)
            {
                channel = Channel as IMessageChannel;
            }
            return channel?.SendMessageAsync(SetVariables(text ?? string.Empty)).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Send a question to the channel/user and return the response.
        /// </summary>
        public string Question(int type, string question)
        {
            var message = Message(type, SetVariables(question ?? string.Empty));
            var answer = WaitForResponse(message);
            return answer.Content;
        }

        // ================================================================================================
        //   User
        // ================================================================================================
        public IUser GetUser(string input)          => GetObject<IUser>(input);
        public bool IsUser(string input)            => GetUser(input) == User;


        // ================================================================================================
        //   Channel
        // ================================================================================================
        public IChannel GetChannel(string input)    => GetObject<IChannel>(input);
        public bool IsChannel(string input)         => GetChannel(input) == Channel;
    }
}
