﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;

namespace Dietphone.BinarySerializers
{
    public interface BinarySerializer<T>
    {
        void WriteItem(BinaryWriter writer, T item);
        void ReadItem(BinaryReader reader, T item);
    }

    public static class BinarySerializerExtensions
    {
        public static void WriteList<T>(this BinaryWriter writer, ICollection<T> list, BinarySerializer<T> serializer)
        {
            writer.Write(list.Count);
            foreach (T item in list)
            {
                serializer.WriteItem(writer, item);
            }
        }

        public static void WriteList(this BinaryWriter writer, List<string> list)
        {
            writer.Write(list.Count);
            foreach (string item in list)
            {
                writer.Write(item);
            }
        }

        public static void Write(this BinaryWriter writer, IEnumerable<Guid> list)
        {
            writer.Write(list.Count());
            foreach (Guid item in list)
            {
                writer.Write(item);
            }
        }

        public static void Write<T>(this BinaryWriter writer, T value, BinarySerializer<T> serializer)
        {
            if (value != null)
            {
                writer.Write(true);
                serializer.WriteItem(writer, value);
            }
            else
            {
                writer.Write(false);
            }
        }

        public static void Write(this BinaryWriter writer, DateTime value)
        {
            writer.Write(value.Ticks);
        }

        public static void WriteObfuscated(this BinaryWriter writer, string value)
        {
            WriteString(writer, value);
        }

        public static void WriteString(this BinaryWriter writer, string value)
        {
            writer.Write(value ?? string.Empty);
        }

        public static void Write(this BinaryWriter writer, Guid value)
        {
            writer.Write(value.ToByteArray());
        }

        public static T ReadGeneric<T>(this BinaryReader reader, BinarySerializer<T> serializer) where T : new()
        {
            if (reader.ReadBoolean())
            {
                T result = new T();
                serializer.ReadItem(reader, result);
                return result;
            }
            return default(T);
        }

        public static List<string> ReadList(this BinaryReader reader)
        {
            List<string> list = new List<string>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadString());
            }
            return list;
        }

        public static List<T> ReadList<T>(this BinaryReader reader, BinarySerializer<T> serializer) where T : new()
        {
            List<T> list = new List<T>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                T item = new T();
                serializer.ReadItem(reader, item);
                list.Add(item);
            }
            return list;
        }

        public static List<Guid> ReadGuids(this BinaryReader reader)
        {
            List<Guid> list = new List<Guid>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadGuid());
            }
            return list;
        }

        public static DateTime ReadDateTime(this BinaryReader reader)
        {
            var int64 = reader.ReadInt64();
            return new DateTime(int64, DateTimeKind.Utc);
        }

        public static String ReadObfuscated(this BinaryReader reader)
        {
            return reader.ReadString();
        }

        public static Guid ReadGuid(this BinaryReader reader)
        {
            Byte[] guid = reader.ReadBytes(16);
            return new Guid(guid);
        }

    }
}
