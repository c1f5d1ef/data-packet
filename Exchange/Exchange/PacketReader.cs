﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;
using PacketReaderDictionary = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketReader>;

namespace Mikodev.Network
{
    public sealed class PacketReader : IDynamicMetaObjectProvider
    {
        internal readonly ConverterDictionary _converters;
        internal PacketReaderDictionary _items = null;
        internal _Element _element;

        public PacketReader(byte[] buffer, ConverterDictionary converters = null)
        {
            _element = new _Element(buffer);
            _converters = converters;
        }

        public PacketReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            _element = new _Element(buffer, offset, length);
            _converters = converters;
        }

        internal PacketReaderDictionary _GetItems()
        {
            var obj = _items;
            if (obj != null)
                return obj;
            if (_element._index < 0)
                return null;
            _element._index = -1;

            var dic = new PacketReaderDictionary();
            var buf = _element._buffer;
            var max = _element._offset + _element._length;
            var idx = _element._offset;
            var len = 0;

            while (idx < max)
            {
                if (buf._HasNext(max, ref idx, out len) == false)
                    return null;
                var key = Encoding.UTF8.GetString(buf, idx, len);
                if (dic.ContainsKey(key))
                    return null;
                idx += len;
                if (buf._HasNext(max, ref idx, out len) == false)
                    return null;
                dic.Add(key, new PacketReader(buf, idx, len, _converters));
                idx += len;
            }

            _items = dic;
            return dic;
        }

        /// <summary>
        /// <paramref name="key"/> not null
        /// </summary>
        internal PacketReader _GetItem(string key, bool nothrow)
        {
            var dic = _GetItems();
            if (dic != null && dic.TryGetValue(key, out var val))
                return val;
            if (nothrow)
                return null;
            throw new PacketException(dic == null ? PacketError.Overflow : PacketError.PathError);
        }

        /// <summary>
        /// <paramref name="keys"/> not null
        /// </summary>
        internal PacketReader _GetItem(IEnumerable<string> keys, bool nothrow)
        {
            var rdr = this;
            foreach (var i in keys)
                if ((rdr = rdr._GetItem(i ?? throw new ArgumentNullException(), nothrow)) == null)
                    return null;
            return rdr;
        }

        public int Count => _GetItems()?.Count ?? 0;

        public IEnumerable<string> Keys => _GetItems()?.Keys ?? Enumerable.Empty<string>();

        public PacketReader this[string path, bool nothrow = false]
        {
            get
            {
                if (path == null)
                    throw new ArgumentNullException(nameof(path));
                var key = path.Split(_Extension.s_separators);
                var val = _GetItem(key, nothrow);
                return val;
            }
        }

        public override string ToString()
        {
            var stb = new StringBuilder(nameof(PacketReader));
            stb.Append(" with ");
            var dic = _GetItems();
            if (dic != null)
                stb.AppendFormat("{0} node(s), ", dic.Count);
            stb.AppendFormat("{0} byte(s)", _element._length);
            return stb.ToString();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicReader(parameter, this);

        internal bool _Convert(Type type, out object value)
        {
            var val = default(object);
            var con = default(IPacketConverter);
            var inf = default(_Inf);
            var tag = 0;

            if (type == typeof(PacketReader))
                val = this;
            else if (type == typeof(PacketRawReader))
                val = new PacketRawReader(this);
            else if ((con = _Caches.GetConverter(_converters, type, true)) != null)
                val = con._GetValueWrapError(_element, true);
            else if (((tag = (inf = _Caches.GetInfo(type)).Flags) & _Inf.Array) != 0)
                val = _Caches.GetArray(this, inf.ElementType);
            else if ((tag & _Inf.Enumerable) != 0)
                val = _Caches.GetEnumerable(this, inf.ElementType);
            else if ((tag & _Inf.List) != 0)
                val = _Caches.GetList(this, inf.ElementType);
            else if ((tag & _Inf.Collection) != 0)
                val = inf.CollectionFunction.Invoke(this);
            else if ((tag & _Inf.Dictionary) != 0)
                val = inf.DictionaryFunction.Invoke(this);
            else goto fail;

            value = val;
            return true;

            fail:
            value = null;
            return false;
        }

        public T Deserialize<T>(T anonymous) => (T)_Deserialize(this, typeof(T));

        public T Deserialize<T>() => (T)_Deserialize(this, typeof(T));

        public object Deserialize(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return _Deserialize(this, type);
        }

        internal static object _Deserialize(PacketReader reader, Type type)
        {
            if (type == typeof(object))
                return reader;
            if (reader._Convert(type, out var ret))
                return ret;

            var res = _Caches.GetSetterInfo(type);
            var arr = res.Arguments;
            var fun = res.Function;
            if (arr == null || fun == null)
                throw new PacketException(PacketError.InvalidType);
            var obj = new object[arr.Length];

            for (int i = 0; i < arr.Length; i++)
                obj[i] = _Deserialize(reader._GetItem(arr[i].Name, false), arr[i].Type);
            var val = fun.Invoke(obj);
            return val;
        }
    }
}
