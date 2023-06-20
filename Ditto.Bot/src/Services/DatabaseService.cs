using Ditto.Bot.Database;
using Ditto.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Ditto.Bot.Services
{
    public enum DatabaseType
    {
        Sqlite = 0,
        Mysql
    }
    public class DatabaseService : BaseClass, IDittoService
    {
        private DbContextOptions _options;
        public UnitOfWork UnitOfWork => new(GetContext());

        public Task Initialised() => Task.CompletedTask;
        public Task Connected() => Task.CompletedTask;
        public Task Exit() => Task.CompletedTask;

        public void Setup(DatabaseType databaseType, string connectionString = null)
        {
            var builder = new DbContextOptionsBuilder();

            switch (databaseType)
            {
                case DatabaseType.Mysql:
                    builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;

                //case DatabaseType.Sqlite:
                //    var filePath = GetProperPathAndCreate($"{Globals.AppDirectory}/data/Kaabot.db");
                //    builder.UseSqlite($"Data Source=\"{filePath}\"");
                //    return;

                default:
                    Log.Fatal($"Unsupported database type \"{databaseType}\"");
                    throw new Exception($"Unsupported database type \"{databaseType}\"");
            }
            _options = builder.Options;

            // Attempt a connection
            Read((_) => { }, true, false);
        }

        public ApplicationDbContext GetContext()
        {
            var context = new ApplicationDbContext(_options);
            context.Database.Migrate();
            context.Database.SetCommandTimeout(180);
            return context;
        }

        private void HandleException(Exception exception, bool silent, bool throwOnError)
        {
            if (!silent)
            {
                Log.Error(exception);
            }
            if (throwOnError)
            {
                throw exception;
            }
        }

        public void Do(Action<UnitOfWork> action, bool complete = true, bool throwOnError = false, bool silent = false)
        {
            Do((uow) =>
            {
                action(uow);
                return true;
            }, complete, throwOnError, silent);
        }

        public TResult Do<TResult>(Func<UnitOfWork, TResult> func, bool complete = true, bool throwOnError = false, bool silent = false)
        {
            var result = default(TResult);
            try
            {
                using var uow = UnitOfWork;
                result = func(uow);
                if (complete)
                {
                    uow.Complete();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, silent, throwOnError);
            }
            return result;
        }

        public Task DoAsync(Action<UnitOfWork> action, bool complete = true, bool throwOnError = false, bool silent = false)
        {
            return DoAsync(uow =>
            {
                action(uow);
                return Task.FromResult(true);
            }, complete, throwOnError, silent);
        }

        public Task DoAsync(Func<UnitOfWork, Task> func, bool complete = true, bool throwOnError = false, bool silent = false)
        {
            return DoAsync(async uow =>
            {
                await func(uow);
                return true;
            }, complete, throwOnError, silent);
        }

        public Task<TResult> DoAsync<TResult>(Func<UnitOfWork, TResult> func, bool complete = true, bool throwOnError = false, bool silent = false)
        {
            return DoAsync(uow =>
            {
                return Task.FromResult(func(uow));
            }, complete, throwOnError, silent);
        }

        public async Task<TResult> DoAsync<TResult>(Func<UnitOfWork, Task<TResult>> func, bool complete = true, bool throwOnError = false, bool silent = false)
        {
            var result = default(TResult);
            try
            {
                using var uow = UnitOfWork;
                result = await func(uow);
                if (complete)
                {
                    await uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, silent, throwOnError);
            }
            return result;
        }


        //***********************************************************************************
        // Read
        //***********************************************************************************
        public void Read(Action<UnitOfWork> action, bool throwOnError = false, bool silent = false)
            => Do(action, false, throwOnError, silent);

        public void Read<TResult>(Func<UnitOfWork, TResult> func, bool throwOnError = false, bool silent = false)
            => Do(func, false, throwOnError, silent);

        public Task ReadAsync(Action<UnitOfWork> action, bool throwOnError = false, bool silent = false)
            => DoAsync(action, false, throwOnError, silent);

        public Task ReadAsync(Func<UnitOfWork, Task> func, bool throwOnError = false, bool silent = false)
            => DoAsync(func, false, throwOnError, silent);

        public Task<TResult> ReadAsync<TResult>(Func<UnitOfWork, TResult> func, bool throwOnError = false, bool silent = false)
            => DoAsync(func, false, throwOnError, silent);

        public Task<TResult> ReadAsync<TResult>(Func<UnitOfWork, Task<TResult>> func, bool throwOnError = false, bool silent = false)
            => DoAsync(func, false, throwOnError, silent);


        //***********************************************************************************
        // Write
        //***********************************************************************************
        public void Write(Action<UnitOfWork> action, bool throwOnError = false, bool silent = false)
            => Do(action, true, throwOnError, silent);

        public void Write<TResult>(Func<UnitOfWork, TResult> func, bool throwOnError = false, bool silent = false)
            => Do(func, true, throwOnError, silent);

        public Task WriteAsync(Action<UnitOfWork> action, bool throwOnError = false, bool silent = false)
            => DoAsync(action, true, throwOnError, silent);

        public Task WriteAsync(Func<UnitOfWork, Task> func, bool throwOnError = false, bool silent = false)
            => DoAsync(func, true, throwOnError, silent);

        public Task<TResult> WriteAsync<TResult>(Func<UnitOfWork, TResult> func, bool throwOnError = false, bool silent = false)
            => DoAsync(func, true, throwOnError, silent);

        public Task<TResult> WriteAsync<TResult>(Func<UnitOfWork, Task<TResult>> func, bool throwOnError = false, bool silent = false)
            => DoAsync(func, true, throwOnError, silent);

    }


}
