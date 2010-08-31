﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Hammock.Serialization
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public class HammockDataContractJsonSerializer : Utf8Serializer, ISerializer, IDeserializer
    {
        private readonly Dictionary<RuntimeTypeHandle, DataContractJsonSerializer> _serializers =
            new Dictionary<RuntimeTypeHandle, DataContractJsonSerializer>();

        #region ISerializer Members

        public virtual string Serialize(object instance, Type type)
        {
            string result;
            using (var stream = new MemoryStream())
            {
                var serializer = CacheOrGetSerializerFor(type);
                serializer.WriteObject(stream, instance);

                var data = stream.ToArray();
                result = ContentEncoding.GetString(data, 0, data.Length);
            }
            return result;
        }

        public virtual string ContentType
        {
            get { return "application/json"; }
        }

        #endregion

        #region IDeserializer Members

        public virtual object Deserialize(string content, Type type)
        {
            object instance;
            using (var stream = new MemoryStream(ContentEncoding.GetBytes(content)))
            {
                var serializer = CacheOrGetSerializerFor(type);
                instance = serializer.ReadObject(stream);
            }
            return instance;
        }

        public virtual T Deserialize<T>(string content)
        {
            var type = typeof (T);
            T instance;
            using (var stream = new MemoryStream(ContentEncoding.GetBytes(content)))
            {
                var serializer = CacheOrGetSerializerFor(type);
                instance = (T) serializer.ReadObject(stream);
            }
            return instance;
        }

        #endregion

        private DataContractJsonSerializer CacheOrGetSerializerFor(Type type)
        {
            var handle = type.TypeHandle;
            if (_serializers.ContainsKey(handle))
            {
                return _serializers[handle];
            }

            var serializer = new DataContractJsonSerializer(type);
            _serializers.Add(handle, serializer);

            return serializer;
        }
    }
}