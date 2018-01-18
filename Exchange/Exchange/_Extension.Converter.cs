﻿using Mikodev.Network.Converters;
using System;
using System.Threading;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    partial class _Extension
    {
        internal static readonly ConverterDictionary s_dic = null;

        static _Extension()
        {
            var dic = new ConverterDictionary();
            var ass = typeof(_Extension).Assembly;

            foreach (var t in ass.GetTypes())
            {
                var ats = t.GetCustomAttributes(typeof(_ConverterAttribute), false);
                if (ats.Length != 1)
                    continue;
                var att = (_ConverterAttribute)ats[0];
                var typ = att.Type;
                var ins = (IPacketConverter)Activator.CreateInstance(t);
                dic.Add(typ, ins);
            }
            s_dic = dic;
        }

        internal static bool _WrapError(Exception ex) => (ex is PacketException || ex is OutOfMemoryException || ex is StackOverflowException || ex is ThreadAbortException) == false;

        internal static PacketException _Overflow() => new PacketException(PacketError.Overflow);

        internal static PacketException _ConvertError(Exception ex) => new PacketException(PacketError.ConvertError, ex);

        internal static PacketException _Mismatch(int len) => new PacketException(PacketError.ConvertMismatch, $"Converter should return a byte array of length {len}");


        internal static object _GetValueWrapError(this IPacketConverter con, _Element ele, bool check)
        {
            return _GetValueWrapError(con, ele._buf, ele._off, ele._len, check);
        }

        internal static object _GetValueWrapError(this IPacketConverter con, byte[] buf, int off, int len, bool check)
        {
            try
            {
                if (check && con.Length > len)
                    throw _Overflow();
                return con.GetValue(buf, off, len);
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
        }

        internal static T _GetValueWrapErrorAuto<T>(this IPacketConverter con, _Element element, bool check)
        {
            return _GetValueWrapErrorAuto<T>(con, element._buf, element._off, element._len, check);
        }

        internal static T _GetValueWrapErrorAuto<T>(this IPacketConverter con, byte[] buf, int off, int len, bool check)
        {
            try
            {
                if (check && con.Length > len)
                    throw _Overflow();
                if (con is IPacketConverter<T> res)
                    return res.GetValue(buf, off, len);
                return (T)con.GetValue(buf, off, len);
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
        }

        internal static T _GetValueWrapErrorGeneric<T>(this IPacketConverter<T> con, byte[] buf, int off, int len, bool check)
        {
            try
            {
                if (check && con.Length > len)
                    throw _Overflow();
                return con.GetValue(buf, off, len);
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
        }

        internal static byte[] _GetBytesWrapError(this IPacketConverter con, object val)
        {
            try
            {
                var buf = con.GetBytes(val);
                if (buf == null)
                    buf = s_empty_bytes;
                var len = con.Length;
                if (len > 0 && len != buf.Length)
                    throw _Mismatch(len);
                return buf;
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
        }

        internal static byte[] _GetBytesWrapErrorGeneric<T>(this IPacketConverter<T> con, T val)
        {
            try
            {
                var buf = con.GetBytes(val);
                if (buf == null)
                    buf = s_empty_bytes;
                var len = con.Length;
                if (len > 0 && len != buf.Length)
                    throw _Mismatch(len);
                return buf;
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
        }
    }
}
