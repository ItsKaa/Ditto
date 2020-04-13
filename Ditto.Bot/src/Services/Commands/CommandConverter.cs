using Discord.Commands;
using Ditto.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TB.ComponentModel;

namespace Ditto.Bot.Services.Commands
{
    public class CommandConverter
    {
        private readonly ConcurrentDictionary<Type, TypeReader> _defaultTypeReaders;
        private readonly ImmutableList<Tuple<Type, Type>> _entityTypeReaders;

        public CommandConverter()
        {
            _defaultTypeReaders = new ConcurrentDictionary<Type, TypeReader>(DiscordHelper.GetDefaultTypeReaders());
            _entityTypeReaders = DiscordHelper.GetDefaultEntityTypeReaders().ToImmutableList();
        }

        public async Task<object> ConvertObjectAsync(ICommandContext context, Type type, string input)
        {
            var typeReader = GetDefaultTypeReader(type);
            var typeResult = await typeReader.ReadAsync(context, input, null).ConfigureAwait(false);
            if (typeResult.IsSuccess)
            {
                if (typeResult.Values.Count > 1)
                {
                    return typeResult.Values.OrderByDescending(x => x.Score).Select(x => x.Value).ToArray();
                }
                return typeResult.Values.OrderByDescending(a => a.Score).FirstOrDefault().Value;
            }
            // try default
            return UniversalTypeConverter.Convert(input, type, CultureInfo.CurrentCulture, ConversionOptions.Default);
        }

        public async Task<object[]> ConvertObjectAsync(ICommandContext context, System.Reflection.ParameterInfo parameterInfo, string input)
        {
            return new[] { await ConvertObjectAsync(context, parameterInfo.ParameterType, input).ConfigureAwait(false) };
        }

        public TypeReader GetDefaultTypeReader(Type type)
        {
            if (_defaultTypeReaders.TryGetValue(type, out var reader))
                return reader;
            var typeInfo = type.GetTypeInfo();

            //Is this an enum?
            if (typeInfo.IsEnum)
            {
                reader = DiscordHelper.GetTypeReader(type);
                _defaultTypeReaders[type] = reader;
                return reader;
            }

            //Is this an entity?
            for (int i = 0; i < _entityTypeReaders.Count; i++)
            {
                if (type == _entityTypeReaders[i].Item1 || typeInfo.ImplementedInterfaces.Contains(_entityTypeReaders[i].Item1))
                {
                    reader = Activator.CreateInstance(_entityTypeReaders[i].Item2.MakeGenericType(type)) as TypeReader;
                    _defaultTypeReaders[type] = reader;
                    return reader;
                }
            }
            return null;
        }
    }
}
