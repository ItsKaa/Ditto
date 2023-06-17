using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Services
{
    public class ButtonHandler : IDisposable
    {
        private ConcurrentDictionary<ulong, List<(string, Func<SocketMessageComponent, Task>)>> ButtonCallbackDictionary { get; } = new ConcurrentDictionary<ulong, List<(string, Func<SocketMessageComponent, Task>)>>();
        public ButtonHandler()
        {
            Ditto.Client.ButtonExecuted += Client_ButtonExecuted;
        }

        public void Dispose()
        {
            Ditto.Client.ButtonExecuted -= Client_ButtonExecuted;
            GC.SuppressFinalize(this);
        }

        public void Add(string buttonId, ulong interactionId, Func<SocketMessageComponent, Task> callbackFunc, bool disableHandleOnAnyButtonPress)
        {
            var addCallbackFunc = callbackFunc;
            if (disableHandleOnAnyButtonPress)
            {
                addCallbackFunc = new Func<SocketMessageComponent, Task>(async (msg) => {
                    ButtonCallbackDictionary.TryRemove(interactionId, out var _);
                    await callbackFunc(msg);
                });
            }

            ButtonCallbackDictionary.AddOrUpdate(interactionId, new[] { (buttonId, addCallbackFunc) }.ToList(), (_, collection) => {
                collection.Add((buttonId, addCallbackFunc));
                return collection;
            });
        }

        public void AddMulti(string buttonId, ulong interactionId, Func<SocketMessageComponent, Task> callbackFunc)
            => Add(buttonId, interactionId, callbackFunc, false);

        public void AddSingle(string buttonId, ulong interactionId, Func<SocketMessageComponent, Task> callbackFunc)
            => Add(buttonId, interactionId, callbackFunc, true);

        private async Task Client_ButtonExecuted(Discord.WebSocket.SocketMessageComponent messageComponent)
        {
            var interactionId = messageComponent?.Message?.Interaction?.Id;
            var buttonId = messageComponent.Data?.CustomId;
            if (interactionId == null || string.IsNullOrEmpty(buttonId))
            {
                return;
            }

            if (ButtonCallbackDictionary.TryGetValue(interactionId.Value, out var callbackFunctions))
            {
                foreach(var data in callbackFunctions.Where(x => x.Item1 == buttonId))
                {
                    await data.Item2(messageComponent);
                }
            }
        }
    }
}
