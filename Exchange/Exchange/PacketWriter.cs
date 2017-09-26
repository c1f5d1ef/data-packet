﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ItemDictionary = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketWriter>;

namespace Mikodev.Network
{
    /// <summary>
    /// Binary packet writer
    /// </summary>
    public class PacketWriter : IDynamicMetaObjectProvider
    {
        internal const int _Level = 64;
        internal object _obj = null;
        internal readonly Dictionary<Type, IPacketConverter> _con = null;

        /// <summary>
        /// Create new writer
        /// </summary>
        /// <param name="converters">Packet converters, use default converters if null</param>
        public PacketWriter(Dictionary<Type, IPacketConverter> converters = null)
        {
            _con = converters;
        }

        internal ItemDictionary _ItemList()
        {
            if (_obj is ItemDictionary dic)
                return dic;
            var val = new ItemDictionary();
            _obj = val;
            return val;
        }

        /// <summary>
        /// Write key and another instance
        /// </summary>
        public PacketWriter Push(string key, PacketWriter val)
        {
            _ItemList()[key] = new PacketWriter(_con) { _obj = val?._obj };
            return this;
        }

        /// <summary>
        /// Write key and data
        /// </summary>
        /// <param name="key">Node tag</param>
        /// <param name="type">Source type</param>
        /// <param name="val">Value to be written</param>
        public PacketWriter Push(string key, Type type, object val)
        {
            var nod = new PacketWriter(_con);
            var con = _Caches.Converter(type, _con, false);
            nod._obj = con.GetBytes(val);
            _ItemList()[key] = nod;
            return this;
        }

        /// <summary>
        /// Write key and data
        /// </summary>
        /// <typeparam name="T">Source type</typeparam>
        /// <param name="key">Node tag</param>
        /// <param name="val">Value to be written</param>
        public PacketWriter Push<T>(string key, T val)
        {
            var nod = new PacketWriter(_con);
            var con = _Caches.Converter(typeof(T), _con, false);
            if (con is IPacketConverter<T> res)
                nod._obj = res.GetBytes(val);
            else
                nod._obj = con.GetBytes(val);
            _ItemList()[key] = nod;
            return this;
        }

        /// <summary>
        /// Set key and byte array
        /// </summary>
        public PacketWriter PushList(string key, byte[] buf)
        {
            var nod = new PacketWriter(_con) { _obj = buf };
            _ItemList()[key] = nod;
            return this;
        }

        internal void _ByteList(Type type, IEnumerable val)
        {
            var con = _Caches.Converter(type, _con, false);
            var hea = con.Length.HasValue == false;
            var mst = new MemoryStream();
            foreach (var i in val)
                mst._WriteExt(con.GetBytes(i), hea);
            mst.Dispose();
            _obj = mst.ToArray();
        }

        internal void _ByteList<T>(IEnumerable<T> val)
        {
            var con = _Caches.Converter(typeof(T), _con, false);
            var hea = con.Length.HasValue == false;
            var mst = new MemoryStream();
            if (con is IPacketConverter<T> res)
                foreach (var i in val)
                    mst._WriteExt(res.GetBytes(i), hea);
            else
                foreach (var i in val)
                    mst._WriteExt(con.GetBytes(i), hea);
            mst.Dispose();
            _obj = mst.ToArray();
        }

        /// <summary>
        /// Write key and collections
        /// </summary>
        /// <param name="key">Node tag</param>
        /// <param name="type">Source type</param>
        /// <param name="val">Value collection</param>
        public PacketWriter PushList(string key, Type type, IEnumerable val)
        {
            var nod = new PacketWriter(_con);
            if (val != null)
                nod._ByteList(type, val);
            _ItemList()[key] = nod;
            return this;
        }

        /// <summary>
        /// Write key and collections
        /// </summary>
        /// <typeparam name="T">Source type</typeparam>
        /// <param name="key">Node tag</param>
        /// <param name="val">Value collection</param>
        public PacketWriter PushList<T>(string key, IEnumerable<T> val)
        {
            var nod = new PacketWriter(_con);
            if (val != null)
                nod._ByteList(val);
            _ItemList()[key] = nod;
            return this;
        }

        internal void _Byte(MemoryStream str, ItemDictionary dic, int lvl)
        {
            if (lvl > _Level)
                throw new PacketException(PacketError.RecursiveError);
            foreach (var i in dic)
            {
                str._WriteExt(Encoding.UTF8.GetBytes(i.Key));
                var val = i.Value;
                if (val._obj is null)
                {
                    str._WriteLen(0);
                    continue;
                }
                if (val._obj is byte[] buf)
                {
                    str._WriteExt(buf);
                    continue;
                }

                var pos = str.Position;
                str._WriteLen(0);
                _Byte(str, (ItemDictionary)val._obj, lvl + 1);
                var end = str.Position;
                str.Seek(pos, SeekOrigin.Begin);
                str._WriteLen((int)(end - pos - sizeof(int)));
                str.Seek(end, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Get binary packet
        /// </summary>
        public byte[] GetBytes()
        {
            if (_obj is byte[] buf)
                return buf;
            var dic = _obj as ItemDictionary;
            if (dic == null)
                return new byte[0];
            var mst = new MemoryStream();
            _Byte(mst, dic, 0);
            return mst.ToArray();
        }

        /// <summary>
        /// Create dynamic writer
        /// </summary>
        public DynamicMetaObject GetMetaObject(Expression parameter) => new _DynamicWriter(parameter, this);

        /// <summary>
        /// Show byte count or node count
        /// </summary>
        public override string ToString()
        {
            var stb = new StringBuilder(nameof(PacketWriter));
            stb.Append(" with ");
            if (_obj is null)
                stb.Append("none");
            else if (_obj is byte[] buf)
                stb.AppendFormat("{0} byte(s)", buf.Length);
            else
                stb.AppendFormat("{0} node(s)", ((ItemDictionary)_obj).Count);
            return stb.ToString();
        }

        internal static bool _ItemNode(object val, Dictionary<Type, IPacketConverter> cons, out PacketWriter value)
        {
            var wtr = new PacketWriter(cons);
            var con = default(IPacketConverter);

            if (val == null)
                wtr._obj = null;
            else if (val is PacketWriter wri)
                wtr._obj = wri._obj;
            else if ((con = _Caches.Converter(val.GetType(), wtr._con, true)) != null)
                wtr._obj = con.GetBytes(val);
            else if (val.GetType()._IsEnumerable(out var inn))
                wtr._ByteList(inn, (IEnumerable)val);
            else
                wtr = null;
            value = wtr;
            return wtr != null;
        }

        internal static PacketWriter _Serialize(object val, Dictionary<Type, IPacketConverter> con, int lvl)
        {
            if (lvl > _Level)
                throw new PacketException(PacketError.RecursiveError);
            var wtr = new PacketWriter(con);

            void _push(string key, object obj)
            {
                var sub = _Serialize(obj, con, lvl + 1);
                wtr._ItemList()[key] = sub;
            }

            if (val is IDictionary<string, object> dic)
                foreach (var p in dic)
                    _push(p.Key, p.Value);
            else if (_ItemNode(val, con, out var nod))
                return nod;
            else
                foreach (var p in val.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    _push(p.Name, p.GetValue(val));
            return wtr;
        }

        /// <summary>
        /// Create new writer from object or dictionary (generic dictionary with string as key)
        /// </summary>
        public static PacketWriter Serialize(object obj, Dictionary<Type, IPacketConverter> converters = null)
        {
            return _Serialize(obj, converters, 0);
        }
    }
}
