﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Network
{
    partial class Cache
    {
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly MethodInfo FromArrayMethodInfo = typeof(Cache).GetMethod(nameof(GetBytesFromArray), Flags);
        private static readonly MethodInfo FromListMethodInfo = typeof(Cache).GetMethod(nameof(GetBytesFromList), Flags);
        private static readonly MethodInfo FromEnumerableMethodInfo = typeof(Cache).GetMethod(nameof(GetBytesFromEnumerable), Flags);
        private static readonly MethodInfo FromDictionaryMethodInfo = typeof(Cache).GetMethod(nameof(GetBytesFromDictionary), Flags);

        private static readonly MethodInfo CastDictionaryMethodInfo = typeof(Convert).GetMethod(nameof(Convert.ToDictionaryCast), Flags);

        private static readonly MethodInfo ToArrayMethodInfo = typeof(Convert).GetMethod(nameof(Convert.ToArray), Flags);
        private static readonly MethodInfo ToListMethodInfo = typeof(Convert).GetMethod(nameof(Convert.ToList), Flags);
        private static readonly MethodInfo ToEnumerableMethodInfo = typeof(Convert).GetMethod(nameof(Convert.ToEnumerable), Flags);
        private static readonly MethodInfo ToDictionaryMethodInfo = typeof(Convert).GetMethod(nameof(Convert.ToDictionary), Flags);

        private static readonly MethodInfo CopyArrayMethodInfo = typeof(Array).GetMethod(nameof(Array.Copy), new[] { typeof(Array), typeof(Array), typeof(int) });

        private static Func<PacketReader, PacketConverter, object> GetToFunction(MethodInfo info, Type element)
        {
            var con = Expression.Parameter(typeof(PacketConverter), "converter");
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var met = info.MakeGenericMethod(element);
            var cal = Expression.Call(met, rea, con);
            var exp = Expression.Lambda<Func<PacketReader, PacketConverter, object>>(cal, rea, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, PacketConverter, object> GetToCollectionFunction(Type type, Type element, out ConstructorInfo info)
        {
            var itr = typeof(IEnumerable<>).MakeGenericType(element);
            var cto = type.GetConstructor(new[] { itr });
            info = cto;
            if (cto == null)
                return null;
            var con = Expression.Parameter(typeof(PacketConverter), "converter");
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var met = ToArrayMethodInfo.MakeGenericMethod(element);
            var cal = Expression.Call(met, rea, con);
            var cst = Expression.Convert(cal, itr);
            var inv = Expression.New(cto, cst);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<PacketReader, PacketConverter, object>>(box, rea, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, PacketConverter, object> GetToCollectionFunction(Type type, Type elementType, out ConstructorInfo constructor, out MethodInfo add)
        {
            constructor = type.GetConstructor(Type.EmptyTypes);
            add = type.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new[] { elementType }, null);
            if (constructor == null || add == null)
                return null;
            var converter = Expression.Parameter(typeof(PacketConverter), "converter");
            var reader = Expression.Parameter(typeof(PacketReader), "reader");
            var method = ToArrayMethodInfo.MakeGenericMethod(elementType);
            var result = AddCollectionFromArray(elementType, Expression.Call(method, reader, converter), constructor, add);
            var expression = Expression.Lambda<Func<PacketReader, PacketConverter, object>>(result, reader, converter);
            var functor = expression.Compile();
            return functor;
        }

        private static Expression AddCollectionFromArray(Type elementType, Expression value, ConstructorInfo constructor, MethodInfo add)
        {
            var instance = Expression.Variable(constructor.DeclaringType, "collection");
            var array = Expression.Variable(value.Type, "array");
            var index = Expression.Variable(typeof(int), "index");
            var label = Expression.Label(typeof(object), "result");
            var block = Expression.Block(
                new[] { instance, array, index },
                Expression.Assign(instance, Expression.New(constructor)),
                Expression.Assign(array, value),
                Expression.Assign(index, Expression.Constant(0)),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(index, Expression.ArrayLength(array)),
                        Expression.Call(instance, add, Expression.ArrayAccess(array, Expression.PostIncrementAssign(index))),
                        Expression.Break(label, Expression.Convert(instance, typeof(object)))),
                    label));
            return block;
        }

        private static Func<PacketReader, PacketConverter, PacketConverter, object> GetToDictionaryFunction(params Type[] types)
        {
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var key = Expression.Parameter(typeof(PacketConverter), "key");
            var val = Expression.Parameter(typeof(PacketConverter), "value");
            var met = ToDictionaryMethodInfo.MakeGenericMethod(types);
            var cal = Expression.Call(met, rea, key, val);
            var exp = Expression.Lambda<Func<PacketReader, PacketConverter, PacketConverter, object>>(cal, rea, key, val);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, int, Info, object> GetToEnumerableAdapter(Type element)
        {
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var lev = Expression.Parameter(typeof(int), "level");
            var inf = Expression.Parameter(typeof(Info), "info");
            var cto = typeof(EnumerableAdapter<>).MakeGenericType(element).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var inv = Expression.New(cto, rea, lev, inf);
            var exp = Expression.Lambda<Func<PacketReader, int, Info, object>>(inv, rea, lev, inf);
            var fun = exp.Compile();
            return fun;
        }

        private static Expression GetCastArrayExpression(Type elementType, out ParameterExpression parameter)
        {
            parameter = Expression.Parameter(typeof(object[]), "objects");
            if (elementType == typeof(object))
                return parameter;
            var len = Expression.ArrayLength(parameter);
            var dst = Expression.NewArrayBounds(elementType, len);
            var loc = Expression.Variable(elementType.MakeArrayType(), "destination");
            var ass = Expression.Assign(loc, dst);
            var cpy = Expression.Call(CopyArrayMethodInfo, parameter, loc, len);
            var blk = Expression.Block(new[] { loc }, new Expression[] { ass, cpy, loc });
            return blk;
        }

        private static Func<object[], object> GetCastArrayFunction(Type elementType)
        {
            var blk = GetCastArrayExpression(elementType, out var arr);
            var exp = Expression.Lambda<Func<object[], object>>(blk, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<object[], object> GetCastListFunction(Type elementType)
        {
            var blk = GetCastArrayExpression(elementType, out var arr);
            var con = typeof(List<>).MakeGenericType(elementType).GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(elementType) });
            var inv = Expression.New(con, blk);
            var exp = Expression.Lambda<Func<object[], object>>(inv, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<object[], object> GetCastCollectionFunction(Type elementType, ConstructorInfo constructor)
        {
            var blk = GetCastArrayExpression(elementType, out var arr);
            var inv = Expression.New(constructor, blk);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<object[], object>>(box, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<object[], object> GetCastCollectionFunction(Type elementType, ConstructorInfo constructor, MethodInfo add)
        {
            var blk = GetCastArrayExpression(elementType, out var arr);
            var box = AddCollectionFromArray(elementType, blk, constructor, add);
            var exp = Expression.Lambda<Func<object[], object>>(box, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<List<object>, object> GetCastDictionaryFunction(params Type[] types)
        {
            var arr = Expression.Parameter(typeof(List<object>), "list");
            var met = CastDictionaryMethodInfo.MakeGenericMethod(types);
            var cal = Expression.Call(met, arr);
            var exp = Expression.Lambda<Func<List<object>, object>>(cal, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketConverter, object, byte[][]> GetFromEnumerableFunction(MethodInfo method, Type enumerable)
        {
            var con = Expression.Parameter(typeof(PacketConverter), "converter");
            var obj = Expression.Parameter(typeof(object), "object");
            var cvt = Expression.Convert(obj, enumerable);
            var cal = Expression.Call(method, con, cvt);
            var exp = Expression.Lambda<Func<PacketConverter, object, byte[][]>>(cal, con, obj);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketConverter, PacketConverter, object, List<KeyValuePair<byte[], byte[]>>> GetFromDictionaryFunction(params Type[] types)
        {
            var key = Expression.Parameter(typeof(PacketConverter), "index");
            var val = Expression.Parameter(typeof(PacketConverter), "element");
            var obj = Expression.Parameter(typeof(object), "object");
            var cvt = Expression.Convert(obj, typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(types)));
            var met = FromDictionaryMethodInfo.MakeGenericMethod(types);
            var cal = Expression.Call(met, key, val, cvt);
            var exp = Expression.Lambda<Func<PacketConverter, PacketConverter, object, List<KeyValuePair<byte[], byte[]>>>>(cal, key, val, obj);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>> GetFromAdapterFunction(params Type[] types)
        {
            var itr = typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(types));
            var cto = typeof(DictionaryAdapter<,>).MakeGenericType(types).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var key = Expression.Parameter(typeof(PacketConverter), "index");
            var obj = Expression.Parameter(typeof(object), "object");
            var cvt = Expression.Convert(obj, itr);
            var inv = Expression.New(cto, key, cvt);
            var exp = Expression.Lambda<Func<PacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>>>(inv, key, obj);
            var fun = exp.Compile();
            return fun;
        }
    }
}
