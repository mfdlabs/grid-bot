﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFDLabs.Hashicorp.VaultClient.V1.SystemBackend
{
    /// <summary>
    /// Converts the <see cref="AbstractAuditBackend" /> object from JSON.
    /// </summary>
    internal class AuditBackendJsonConverter : JsonConverter
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON.
        /// </summary>
        /// <value>
        /// <c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanWrite => false;

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <exception cref="NotImplementedException">Unnecessary because CanWrite is false. The type will skip the converter.</exception>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanWrite is false. The type will skip the converter.");
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken jtoken = JToken.Load(reader);
            object target = null;

            if (jtoken != null && jtoken.HasValues && jtoken["type"] != null)
            {
                var typeValue = jtoken["type"].Value<string>();
                var type = new AuditBackendType(typeValue);

                if (type == AuditBackendType.File)
                {
                    target = new FileAuditBackend();
                }
                else
                {
                    if (type == AuditBackendType.Syslog)
                    {
                        target = new SyslogAuditBackend();
                    }
                }

                if (target == null)
                {
                    target = new CustomAuditBackend(new AuditBackendType(typeValue));
                }

                serializer.Populate(jtoken.CreateReader(), target);
            }

            return target;
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (AbstractAuditBackend);
        }
    }
}