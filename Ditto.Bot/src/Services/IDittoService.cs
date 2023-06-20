using System;
using System.Threading.Tasks;

namespace Ditto.Bot.Services
{
    public interface IDittoService
    {
        public Task Initialised();

        public Task Connected();

        public Task Exit();
    }
}
