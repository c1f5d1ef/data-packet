﻿using System;
using System.Collections.Generic;
using System.Net;
using static System.BitConverter;

namespace Mikodev.Network
{
    partial class _Extension
    {
        internal const int _GuidLength = 4 * sizeof(int);

        internal static byte[] _OfIPAddress(IPAddress value) => value.GetAddressBytes();

        internal static IPAddress _ToIPAddress(byte[] buffer, int offset, int length) => new IPAddress(_Borrow(buffer, offset, length));

        internal static byte[] _OfEndPoint(IPEndPoint value)
        {
            var add = value.Address.GetAddressBytes();
            var pot = GetBytes((ushort)value.Port);
            var res = new byte[add.Length + pot.Length];
            Buffer.BlockCopy(add, 0, res, 0, add.Length);
            Buffer.BlockCopy(pot, 0, res, add.Length, pot.Length);
            return res;
        }

        internal static IPEndPoint _ToEndPoint(byte[] buffer, int offset, int length)
        {
            var add = new IPAddress(_Borrow(buffer, offset, length - sizeof(ushort)));
            var pot = ToUInt16(buffer, offset + length - sizeof(ushort));
            return new IPEndPoint(add, pot);
        }

        internal static byte[] _OfDecimal(decimal value)
        {
            var arr = decimal.GetBits(value);
            var buf = new byte[sizeof(decimal)];
            for (int i = 0; i < arr.Length; i++)
                Buffer.BlockCopy(GetBytes(arr[i]), 0, buf, i * sizeof(int), sizeof(int));
            return buf;
        }

        internal static decimal _ToDecimal(byte[] buffer, int offset)
        {
            var arr = new int[sizeof(decimal) / sizeof(int)];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = ToInt32(buffer, offset + i * sizeof(int));
            var val = new decimal(arr);
            return val;
        }

        internal static byte[] _OfSByte(sbyte value) => new[] { (byte)value };

        internal static byte[] _OfByte(byte value) => new[] { value };

        internal static sbyte _ToSByte(byte[] buffer, int offset) => (sbyte)buffer[offset];

        internal static byte _ToByte(byte[] buffer, int offset) => buffer[offset];

        internal static byte[] _OfByteArray(this byte[] buffer) => buffer;

        internal static byte[] _OfSByteArray(this sbyte[] buffer)
        {
            var len = buffer?.Length ?? 0;
            var buf = new byte[len];
            if (len > 0)
                Buffer.BlockCopy(buffer, 0, buf, 0, len);
            return buf;
        }

        internal static byte[] _ToByteArray(this byte[] buffer, int offset, int length)
        {
            var len = buffer?.Length ?? 0;
            if (length < 0 || length > len)
                throw new PacketException(PacketError.Overflow);
            var buf = new byte[length];
            if (length > 0)
                Buffer.BlockCopy(buffer, offset, buf, 0, length);
            return buf;
        }

        internal static sbyte[] _ToSByteArray(this byte[] buffer, int offset, int length)
        {
            var len = buffer?.Length ?? 0;
            if (length < 0 || length > len)
                throw new PacketException(PacketError.Overflow);
            var buf = new sbyte[length];
            if (length > 0)
                Buffer.BlockCopy(buffer, offset, buf, 0, length);
            return buf;
        }

        internal static byte[] _OfByteCollection(this ICollection<byte> buffer)
        {
            var len = buffer?.Count ?? 0;
            var buf = new byte[len];
            if (len > 0)
                buffer.CopyTo(buf, 0);
            return buf;
        }

        internal static byte[] _OfSByteCollection(this ICollection<sbyte> buffer)
        {
            var len = buffer?.Count ?? 0;
            var buf = new byte[len];
            if (len > 0)
            {
                var tmp = new sbyte[len];
                buffer.CopyTo(tmp, 0);
                Buffer.BlockCopy(tmp, 0, buf, 0, len);
            }
            return buf;
        }

        internal static byte[] _OfDateTime(DateTime value) => GetBytes(value.ToBinary());

        internal static DateTime _ToDateTime(byte[] buffer, int offset) => DateTime.FromBinary(ToInt64(buffer, offset));

        internal static byte[] _OfTimeSpan(TimeSpan value) => GetBytes(value.Ticks);

        internal static TimeSpan _ToTimeSpan(byte[] buffer, int offset) => new TimeSpan(ToInt64(buffer, offset));

        internal static byte[] _OfGuid(Guid value) => value.ToByteArray();

        internal static Guid _ToGuid(byte[] buffer, int offset) => new Guid(_Borrow(buffer, offset, _GuidLength));
    }
}
