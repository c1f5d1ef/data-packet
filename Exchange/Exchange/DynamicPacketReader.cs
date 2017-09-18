﻿using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Network
{
    internal sealed class DynamicPacketReader : DynamicMetaObject
    {
        internal static readonly MethodInfo s_Method = typeof(PacketReader).GetTypeInfo().GetMethod(nameof(PacketReader._ListGen), BindingFlags.NonPublic | BindingFlags.Instance);

        internal DynamicPacketReader(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        /// <summary>
        /// Get node by key, throw if not found
        /// </summary>
        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var rea = (PacketReader)Value;
            var val = rea._Item(binder.Name, false);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        /// <summary>
        /// Cast node to target type, throw if type invalid
        /// </summary>
        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            var rea = (PacketReader)Value;
            var typ = binder.Type;
            var val = default(object);

            if (rea._con.TryGetValue(typ, out var con) || PacketCaches.TryGetValue(typ, out con))
                val = con.ToObject(rea._buf, rea._off, rea._len);
            else if (typ._IsGenericEnumerable(out var inn))
                val = s_Method.MakeGenericMethod(inn).Invoke(rea, null);
            else
                throw new PacketException(PacketError.TypeInvalid);

            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }
    }
}
