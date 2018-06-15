﻿using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    partial class PacketWriter
    {
        #region new item
        internal static Item NewItem(byte[] data) => new Item(data, ItemFlags.Buffer);

        internal static Item NewItem(List<Item> data) => new Item(data, ItemFlags.ItemList);

        internal static Item NewItem(Dictionary<string, PacketWriter> data) => new Item(data, ItemFlags.Dictionary);

        internal static Item NewItem(byte[][] data, int length) => new Item(data, ItemFlags.BufferArray, length);

        internal static Item NewItem(List<KeyValuePair<byte[], Item>> data, int length) => new Item(data, ItemFlags.DictionaryBufferItem, length);

        internal static Item NewItem(List<KeyValuePair<byte[], byte[]>> data, int indexLength, int elementLength) => new Item(data, ItemFlags.DictionaryBuffer, indexLength, elementLength);
        #endregion

        internal sealed class Item
        {
            internal static readonly Item Empty = new Item(new object(), ItemFlags.None);

            internal readonly object data;
            internal readonly ItemFlags flag;
            private readonly int indexLength;
            private readonly int elementLength;

            internal Item(object data, ItemFlags flag)
            {
                if (data == null)
                    return;
                this.data = data;
                this.flag = flag;
            }

            internal Item(object data, ItemFlags flag, int elementLength)
            {
                if (data == null)
                    return;
                this.data = data;
                this.flag = flag;
                this.elementLength = elementLength;
            }

            internal Item(object data, ItemFlags flag, int indexLength, int elementLength)
            {
                if (data == null)
                    return;
                this.data = data;
                this.flag = flag;
                this.indexLength = indexLength;
                this.elementLength = elementLength;
            }

            internal void GetBytes(UnsafeStream stream, int level)
            {
                PacketException.VerifyRecursionError(ref level);
                var source = stream.BeginModify();
                GetBytesMatch(stream, level);
                stream.EndModify(source);
            }

            internal void GetBytesMatch(UnsafeStream stream, int level)
            {
                PacketException.VerifyRecursionError(ref level);
                switch (flag)
                {
                    case ItemFlags.None:
                        break;
                    case ItemFlags.Buffer:
                        stream.Write((byte[])data);
                        break;
                    case ItemFlags.BufferArray:
                        GetBytesMatchBufferArray(stream);
                        break;
                    case ItemFlags.ItemList:
                        GetBytesMatchItemList(stream, level);
                        break;
                    case ItemFlags.Dictionary:
                        GetBytesMatchDictionary(stream, level);
                        break;
                    case ItemFlags.DictionaryBuffer:
                        GetBytesMatchDictionaryBuffer(stream);
                        break;
                    case ItemFlags.DictionaryBufferItem:
                        GetBytesMatchDictionaryBufferItem(stream, level);
                        break;
                    default: throw new ApplicationException();
                }
            }

            private void GetBytesMatchBufferArray(UnsafeStream stream)
            {
                var array = (byte[][])data;
                if (elementLength > 0)
                    for (int i = 0; i < array.Length; i++)
                        stream.Write(array[i]);
                else
                    for (int i = 0; i < array.Length; i++)
                        stream.WriteExtend(array[i]);
            }

            private void GetBytesMatchItemList(UnsafeStream stream, int level)
            {
                var list = (List<Item>)data;
                for (int i = 0; i < list.Count; i++)
                    list[i].GetBytes(stream, level);
            }

            private void GetBytesMatchDictionary(UnsafeStream stream, int level)
            {
                var dictionary = (Dictionary<string, PacketWriter>)data;
                foreach (var i in dictionary)
                {
                    stream.WriteKey(i.Key);
                    i.Value.item.GetBytes(stream, level);
                }
            }

            private void GetBytesMatchDictionaryBuffer(UnsafeStream stream)
            {
                var list = (List<KeyValuePair<byte[], byte[]>>)data;
                for (int i = 0; i < list.Count; i++)
                {
                    var current = list[i];
                    if (indexLength > 0)
                        stream.Write(current.Key);
                    else
                        stream.WriteExtend(current.Key);
                    if (elementLength > 0)
                        stream.Write(current.Value);
                    else
                        stream.WriteExtend(current.Value);
                }
            }

            private void GetBytesMatchDictionaryBufferItem(UnsafeStream stream, int level)
            {
                var list = (List<KeyValuePair<byte[], Item>>)data;
                for (int i = 0; i < list.Count; i++)
                {
                    var current = list[i];
                    if (elementLength > 0)
                        stream.Write(current.Key);
                    else
                        stream.WriteExtend(current.Key);
                    current.Value.GetBytes(stream, level);
                }
            }

            public override string ToString() => $"{nameof(Item)} | flag: {flag}, index length: {indexLength}, element length: {elementLength}";
        }
    }
}
