using Discord;
using Discord.Commands;
using Ditto.Data.Commands.Readers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Ditto.Helpers
{
    public static class DiscordHelper
    {
        private static bool PrimitiveMethodString(string x, out string y)
        {
            y = x;
            return true;
        }

        //internal delegate bool TryParseDelegate<T>(string str, out T value);
        private static readonly string DiscordCommandsAssemblyName = "Discord.Net.Commands";
        public static IDictionary<Type, TypeReader> GetDefaultTypeReaders()
        {
            var defaultTypeReaders = new Dictionary<Type, TypeReader>();
            
            var primitiveParsersType = Type.GetType("Discord.Commands.PrimitiveParsers, " + DiscordCommandsAssemblyName);
            var primitiveTypeReaderType = Type.GetType("Discord.Commands.PrimitiveTypeReader, " + DiscordCommandsAssemblyName);
            var nullableTypeReaderType = Type.GetType("Discord.Commands.NullableTypeReader, " + DiscordCommandsAssemblyName);
            var supportedTypes = (IEnumerable<Type>)primitiveParsersType.GetField("SupportedTypes").GetValue(null);
            var primitiveTypeReaderCreate = primitiveTypeReaderType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
            var nullableTypeReaderCreate = nullableTypeReaderType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

            //foreach (var type in PrimitiveParsers.SupportedTypes)
            foreach (var type in supportedTypes)
            {
                //defaultTypeReaders[type] = PrimitiveTypeReader.Create(type);
                //defaultTypeReaders[typeof(Nullable<>).MakeGenericType(type)] = NullableTypeReader.Create(type, defaultTypeReaders[type]);
                defaultTypeReaders[type] = (TypeReader)primitiveTypeReaderCreate.Invoke(null, new object[] { type });
                defaultTypeReaders[typeof(Nullable<>).MakeGenericType(type)] = (TypeReader)nullableTypeReaderCreate.Invoke(null, new object[] { type, defaultTypeReaders[type] });
            }

            //defaultTypeReaders[typeof(string)] = new PrimitiveTypeReader<string>((string x, out string y) => { y = x; return true; }, 0);
            var primitiveTypeReaderGenericType = Type.GetType("Discord.Commands.PrimitiveTypeReader`1, " + DiscordCommandsAssemblyName);
            var tryParseDelegateGenericType = Type.GetType("Discord.Commands.TryParseDelegate`1, " + DiscordCommandsAssemblyName);
            defaultTypeReaders[typeof(string)] = (TypeReader)Activator.CreateInstance(primitiveTypeReaderGenericType.MakeGenericType(typeof(string)), new object[] {
                Delegate.CreateDelegate(
                    tryParseDelegateGenericType.MakeGenericType(typeof(string)),
                    typeof(DiscordHelper).GetMethod(nameof(PrimitiveMethodString),
                    BindingFlags.Static | BindingFlags.NonPublic)
                ),
                (float)0
            });
            return defaultTypeReaders;
        }

        public static IImmutableList<Tuple<Type, Type>> GetDefaultEntityTypeReaders()
        {
            var entityTypeReaders = ImmutableList.CreateBuilder<Tuple<Type, Type>>();
            //entityTypeReaders.Add(new Tuple<Type, Type>(typeof(IMessage), typeof(MessageTypeReader<>)));
            //entityTypeReaders.Add(new Tuple<Type, Type>(typeof(IChannel), typeof(ChannelTypeReaderEx<>))); //ChannelTypeReader<>
            //entityTypeReaders.Add(new Tuple<Type, Type>(typeof(IRole), typeof(RoleTypeReader<>)));
            //entityTypeReaders.Add(new Tuple<Type, Type>(typeof(IUser), typeof(UserTypeReader<>)));

            entityTypeReaders.Add(new Tuple<Type, Type>(typeof(IMessage), Type.GetType("Discord.Commands.MessageTypeReader`1, " + DiscordCommandsAssemblyName)));
            entityTypeReaders.Add(new Tuple<Type, Type>(typeof(IChannel), typeof(ChannelTypeReaderEx<>))); //ChannelTypeReader<>
            entityTypeReaders.Add(new Tuple<Type, Type>(typeof(IRole), Type.GetType("Discord.Commands.RoleTypeReader`1, " + DiscordCommandsAssemblyName)));
            entityTypeReaders.Add(new Tuple<Type, Type>(typeof(IUser), Type.GetType("Discord.Commands.UserTypeReader`1, " + DiscordCommandsAssemblyName)));

            return entityTypeReaders.ToImmutable();
        }

        public static TypeReader GetTypeReader(Type type)
        {
            var method = Type.GetType("Discord.Commands.EnumTypeReader, " + DiscordCommandsAssemblyName).GetMethod("GetReader", BindingFlags.Static | BindingFlags.NonPublic);
            var typeReader = (TypeReader)method.Invoke(null, new object[] { type });
            //return EnumTypeReader.GetReader(type);
            return typeReader;
        }

        public static RequestOptions GetRequestOptions(bool headerOnly = false)
        {
            var requestOptions = new RequestOptions();
            requestOptions.GetType().GetProperty(nameof(RequestOptions.HeaderOnly)).SetValue(requestOptions, true, null);
            //new RequestOptions() { HeaderOnly = headerOnly };
            return requestOptions;
        }
    }
}
