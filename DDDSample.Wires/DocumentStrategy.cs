using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using ProtoBuf;
using ServiceStack.Text;
using DDDSample.Contracts;


    static class NameCache<T>
    {
        // ReSharper disable StaticFieldInGenericType
        public static readonly string Name;
        public static readonly string Namespace;
        // ReSharper restore StaticFieldInGenericType
        static NameCache()
        {
            var type = typeof(T);

            Name = new string(Splice(type.Name).ToArray()).TrimStart('-');
            var dcs = type.GetCustomAttributes(false).OfType<DataContractAttribute>().ToArray();


            if (dcs.Length <= 0) return;
            var attribute = dcs.First();

            if (!string.IsNullOrEmpty(attribute.Name))
            {
                Name = attribute.Name;
            }

            if (!string.IsNullOrEmpty(attribute.Namespace))
            {
                Namespace = attribute.Namespace;
            }
        }

        static IEnumerable<char> Splice(string source)
        {
            foreach (var c in source)
            {
                if (char.IsUpper(c))
                {
                    yield return '-';
                }
                yield return char.ToLower(c);
            }
        }
    }

    public sealed class ViewStrategy : IDocumentStrategy
    {
        public string GetEntityBucket<TEntity>()
        {
            return Conventions.ViewsFolder + "/" + NameCache<TEntity>.Name;
        }

        public string GetEntityLocation<TEntity>(object key)
        {
            if (key is unit)
                return NameCache<TEntity>.Name + ".pb";

            var hashed = key as IIdentity;
            if (hashed != null)
            {
                var stableHashCode = hashed.GetStableHashCode();
                var b = (byte)((uint)stableHashCode % 251);
                return b + "/" + hashed.GetTag() + "-" + hashed.GetId() + ".pb";
            }
            if (key is Guid)
            {
                var b = (byte)((uint)((Guid)key).GetHashCode() % 251);
                return b + "/" + key.ToString().ToLowerInvariant() + ".pb";
            }
            if (key is string)
            {
                var corrected = ((string)key).ToLowerInvariant().Trim();
                var b = (byte)((uint)CalculateStringHash(corrected) % 251);
                return b + "/" + corrected + ".pb";
            }
            return key.ToString().ToLowerInvariant() + ".pb";
        }

        static int CalculateStringHash(string value)
        {
            if (value == null) return 42;
            unchecked
            {
                var hash = 23;
                foreach (var c in value)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }
        public void Serialize<TEntity>(TEntity entity, Stream stream)
        {
            // ProtoBuf must have non-zero files
            stream.WriteByte(42);
            Serializer.Serialize(stream, entity);
        }

        public TEntity Deserialize<TEntity>(Stream stream)
        {
            var signature = stream.ReadByte();

            if (signature != 42)
                throw new InvalidOperationException("Unknown view format");

            return Serializer.Deserialize<TEntity>(stream);
        }
    }

    public sealed class DocumentStrategy : IDocumentStrategy
    {
        public void Serialize<TEntity>(TEntity entity, Stream stream)
        {
            // ProtoBuf must have non-zero files
            stream.WriteByte(42);
            Serializer.Serialize(stream, entity);
        }

        public TEntity Deserialize<TEntity>(Stream stream)
        {
            var signature = stream.ReadByte();

            if (signature != 42)
                throw new InvalidOperationException("Unknown view format");

            return Serializer.Deserialize<TEntity>(stream);
        }

        public string GetEntityBucket<TEntity>()
        {
            return Conventions.DocsFolder + "/" + NameCache<TEntity>.Name;
        }

        public string GetEntityLocation<TEntity>(object key)
        {
            if (key is unit)
                return NameCache<TEntity>.Name + ".pb";

            return key.ToString().ToLowerInvariant() + ".pb";
        }
    }
