﻿using System.Dynamic;
using System.Linq.Expressions;

namespace Mikodev.Network
{
    internal class DynamicPacketWriter : DynamicMetaObject
    {
        internal DynamicPacketWriter(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        /// <summary>
        /// 动态创建节点
        /// </summary>
        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var wtr = (PacketWriter)Value;
            var val = wtr._Push(binder.Name);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        /// <summary>
        /// 动态设置元素
        /// </summary>
        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            var wtr = (PacketWriter)Value;
            var key = binder.Name;
            var val = value.Value;

            if (val is byte[] buf)
            {
                wtr._Push(key, buf);
            }
            else if (val is PacketWriter pkt)
            {
                wtr._Push(key, pkt);
            }
            else
            {
                var fun = wtr._Func(val.GetType());
                var bts = fun.Invoke(val);
                wtr._Push(key, bts);
            }

            var nod = wtr._Push(key);
            var exp = Expression.Constant(val, typeof(object));
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }
    }
}
