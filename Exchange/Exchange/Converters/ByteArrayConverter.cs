﻿using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(byte[]))]
    internal sealed class ByteArrayConverter : PacketConverter<byte[]>
    {
        public static byte[] ToBytes(byte[] buffer) => buffer ?? Extension.s_empty_bytes;

        public static byte[] ToValue(byte[] buffer, int offset, int length)
        {
            if (length == 0)
                return Extension.s_empty_bytes;
            if (buffer == null || (uint)length > (uint)buffer.Length)
                throw PacketException.Overflow();
            var dst = new byte[length];
            Buffer.BlockCopy(buffer, offset, dst, 0, length);
            return dst;
        }

        public override int Length => 0;

        public override byte[] GetBytes(byte[] value) => ToBytes(value);

        public override byte[] GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((byte[])value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
