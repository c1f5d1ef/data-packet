﻿using System;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public static partial class PacketConvert
    {
        #region Throw If Argument Error
        internal static void ThrowIfArgumentError(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return;
        }

        internal static void ThrowIfArgumentError(PacketRawWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            return;
        }

        internal static void ThrowIfArgumentError(PacketRawReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            return;
        }

        internal static void ThrowIfArgumentError(PacketWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            return;
        }

        internal static void ThrowIfArgumentError(PacketReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            return;
        }

        internal static void ThrowIfArgumentError(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return;
        }

        internal static void ThrowIfArgumentError(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            return;
        }

        internal static void ThrowIfArgumentError(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            return;
        }
        #endregion

        public static object GetValue(byte[] buffer, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(buffer);
            return _Caches.Converter(null, type, false)._GetValueWrapError(buffer, 0, buffer.Length, true);
        }

        public static object GetValue(byte[] buffer, int offset, int length, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(buffer, offset, length);
            return _Caches.Converter(null, type, false)._GetValueWrapError(buffer, offset, length, true);
        }

        public static object GetValue<T>(byte[] buffer)
        {
            ThrowIfArgumentError(buffer);
            return _Caches.Converter<T>(null, false)._GetValueWrapErrorAuto<T>(buffer, 0, buffer.Length, true);
        }

        public static object GetValue<T>(byte[] buffer, int offset, int length)
        {
            ThrowIfArgumentError(buffer, offset, length);
            return _Caches.Converter<T>(null, false)._GetValueWrapError(buffer, offset, length, true);
        }

        public static byte[] GetBytes(object value, Type type)
        {
            ThrowIfArgumentError(type);
            return _Caches.GetBytes(type, null, value);
        }

        public static byte[] GetBytes<T>(T value)
        {
            return _Caches.GetBytesAuto(null, value);
        }
        
        public static object Deserialize(byte[] buffer, Type type, ConverterDictionary converters = null)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(buffer);

            var rea = new PacketReader(buffer, converters);
            var val = PacketReader._Deserialize(rea, type);
            return val;
        }

        public static object Deserialize(byte[] buffer, int offset, int length, Type type, ConverterDictionary converters = null)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(buffer, offset, length);

            var rea = new PacketReader(buffer, converters);
            var val = PacketReader._Deserialize(rea, type);
            return val;
        }

        public static T Deserialize<T>(byte[] buffer, ConverterDictionary converters = null)
        {
            return (T)Deserialize(buffer, typeof(T), converters);
        }

        public static T Deserialize<T>(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            return (T)Deserialize(buffer, offset, length, typeof(T), converters);
        }

        public static T Deserialize<T>(byte[] buffer, T anonymous, ConverterDictionary converters = null)
        {
            return (T)Deserialize(buffer, typeof(T), converters);
        }

        public static T Deserialize<T>(byte[] buffer, int offset, int length, T anonymous, ConverterDictionary converters = null)
        {
            return (T)Deserialize(buffer, offset, length, typeof(T), converters);
        }
    }
}