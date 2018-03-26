﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;
using PacketWriterDictionary = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketWriter>;
using PacketWriterPairList = System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<byte[], Mikodev.Network.PacketWriter>>;

namespace Mikodev.Network
{
    public sealed partial class PacketWriter : IDynamicMetaObjectProvider
    {
        internal readonly ConverterDictionary _cvt;
        private object _itm;
        private int _lenone;
        private int _lentwo;

        internal PacketWriter(object obj, ConverterDictionary cvt)
        {
            _cvt = cvt;
            _itm = obj;
        }

        internal PacketWriter(object obj, ConverterDictionary cvt, int lenone)
        {
            _cvt = cvt;
            _itm = obj;
            _lenone = lenone;
        }

        internal PacketWriter(object obj, ConverterDictionary cvt, int lenone, int lentwo)
        {
            _cvt = cvt;
            _itm = obj;
            _lenone = lenone;
            _lentwo = lentwo;
        }

        internal PacketWriter(PacketWriter wtr, ConverterDictionary cvt)
        {
            _cvt = cvt;
            if (wtr == null)
                return;
            _itm = wtr._itm;
            _lenone = wtr._lenone;
            _lentwo = wtr._lentwo;
        }

        public PacketWriter(ConverterDictionary converters = null) => _cvt = converters;

        internal object GetObject() => _itm;

        internal PacketWriterDictionary GetDictionary()
        {
            if (_itm is PacketWriterDictionary dic)
                return dic;
            var val = new PacketWriterDictionary();
            _itm = val;
            return val;
        }

        internal void GetBytesExtra(Stream str, int lev)
        {
            if (lev > _Caches.Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var itm = _itm;
            if (itm is PacketWriterDictionary dic)
            {
                foreach (var i in dic)
                {
                    str.WriteKey(i.Key);
                    i.Value.GetBytes(str, lev);
                }
            }
            else if (itm is List<PacketWriter> lst)
            {
                for (int i = 0; i < lst.Count; i++)
                {
                    lst[i].GetBytes(str, lev);
                }
            }
            else if (itm is byte[][] byt)
            {
                var len = _lenone;
                if (len > 0)
                    for (int i = 0; i < byt.Length; i++)
                        str.Write(byt[i]);
                else
                    for (int i = 0; i < byt.Length; i++)
                        str.WriteExt(byt[i]);
            }
            else if (itm is List<KeyValuePair<byte[], byte[]>> itr)
            {
                var keylen = _lenone;
                var vallen = _lentwo;
                foreach (var i in itr)
                {
                    var key = i.Key;
                    var val = i.Value;
                    if (keylen > 0)
                        str.Write(key, 0, key.Length);
                    else
                        str.WriteExt(key);
                    if (vallen > 0)
                        str.Write(val, 0, val.Length);
                    else
                        str.WriteExt(val);
                }
            }
            else if (itm is PacketWriterPairList kvp)
            {
                var len = _lenone;
                for (int i = 0; i < kvp.Count; i++)
                {
                    var cur = kvp[i];
                    if (len > 0)
                        str.Write(cur.Key, 0, len);
                    else
                        str.WriteExt(cur.Key);
                    cur.Value.GetBytes(str, lev);
                }
            }
            else throw new ApplicationException();
        }

        internal void GetBytes(Stream str, int lev)
        {
            if (lev > _Caches.Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var itm = _itm;
            if (itm == null)
            {
                str.Write(_Extension.s_zero_bytes, 0, sizeof(int));
            }
            else if (itm is byte[] buf)
            {
                str.WriteExt(buf);
            }
            else if (itm is MemoryStream mst)
            {
                str.WriteExt(mst);
            }
            else
            {
                str.BeginInternal(out var src);
                GetBytesExtra(str, lev);
                str.FinshInternal(src);
            }
        }

        internal static PacketWriter GetWriter(ConverterDictionary cvt, object itm, int lev)
        {
            if (lev > _Caches.Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var con = default(IPacketConverter);
            if (itm == null)
                return new PacketWriter(cvt);
            if (itm is PacketWriter oth)
                return new PacketWriter(oth, cvt);
            if (itm is PacketRawWriter raw)
                return new PacketWriter(raw._str, cvt);

            var type = itm.GetType();
            if ((con = _Caches.GetConverter(cvt, type, true)) != null)
                return new PacketWriter(con.GetBytesWrap(itm), cvt);

            var inf = _Caches.GetInfo(type);
            var tag = inf.From;
            switch (tag)
            {
                case _Inf.Enumerable:
                    {
                        if (inf.ElementType == typeof(byte) && itm is ICollection<byte> bytes)
                            return new PacketWriter(bytes.ToBytes(), cvt);
                        if (inf.ElementType == typeof(sbyte) && itm is ICollection<sbyte> sbytes)
                            return new PacketWriter(sbytes.ToBytes(), cvt);

                        if ((con = _Caches.GetConverter(cvt, inf.ElementType, true)) != null)
                            return new PacketWriter(inf.FromEnumerable(con, itm), cvt, con.Length);

                        var lst = new List<PacketWriter>();
                        foreach (var i in (itm as IEnumerable))
                            lst.Add(GetWriter(cvt, i, lev));
                        return new PacketWriter(lst, cvt);
                    }
                case _Inf.Mapping:
                    {
                        var dic = (IDictionary<string, object>)itm;
                        var wtr = new PacketWriter(cvt);
                        var lst = wtr.GetDictionary();
                        foreach (var i in dic)
                            lst[i.Key] = GetWriter(cvt, i.Value, lev);
                        return wtr;
                    }
                case _Inf.Dictionary:
                    {
                        var key = _Caches.GetConverter(cvt, inf.IndexerType, true);
                        if (key == null)
                            throw PacketException.InvalidKeyType(inf.IndexerType);
                        if ((con = _Caches.GetConverter(cvt, inf.ElementType, true)) != null)
                        {
                            var val = inf.FromDictionary(key, con, itm);
                            var res = new PacketWriter(val, cvt, key.Length, con.Length);
                            return res;
                        }
                        else
                        {
                            var val = new PacketWriterPairList();
                            var kvp = inf.FromDictionaryAdapter(key, itm);
                            foreach (var i in kvp)
                            {
                                var sub = GetWriter(cvt, i.Value, lev);
                                var tmp = new KeyValuePair<byte[], PacketWriter>(i.Key, sub);
                                val.Add(tmp);
                            }
                            var res = new PacketWriter(val, cvt, key.Length);
                            return res;
                        }
                    }
                default:
                    {
                        var res = new PacketWriter(cvt);
                        var lst = res.GetDictionary();
                        var get = _Caches.GetGetterInfo(type);
                        var val = get.GetValues(itm);
                        var arg = get.Arguments;
                        for (int i = 0; i < arg.Length; i++)
                            lst[arg[i].Name] = GetWriter(cvt, val[i], lev);
                        return res;
                    }
            }
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicWriter(parameter, this);

        public byte[] GetBytes()
        {
            var obj = _itm;
            if (obj == null)
                return _Extension.s_empty_bytes;
            else if (obj is byte[] buf)
                return buf;
            else if (obj is MemoryStream raw)
                return raw.ToArray();
            var mst = new MemoryStream(_Caches.Length);
            GetBytesExtra(mst, 0);
            var res = mst.ToArray();
            return res;
        }

        public override string ToString()
        {
            var obj = _itm;
            var stb = new StringBuilder(nameof(PacketWriter));
            stb.Append(" with ");
            if (obj == null)
                stb.Append("none");
            else if (obj is byte[] buf)
                stb.AppendFormat("{0} byte(s)", buf.Length);
            else if (obj is MemoryStream mst)
                stb.AppendFormat("{0} byte(s)", mst.Length);
            else if (obj is ICollection col)
                stb.AppendFormat("{0} node(s)", col.Count);
            else
                throw new ApplicationException();
            return stb.ToString();
        }

        public static PacketWriter Serialize(object value, ConverterDictionary converters = null) => GetWriter(converters, value, 0);

        public static PacketWriter Serialize(IDictionary<string, object> dictionary, ConverterDictionary converters = null) => Serialize((object)dictionary, converters);
    }
}
