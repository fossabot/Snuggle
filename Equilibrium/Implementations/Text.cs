﻿using System;
using System.IO;
using Equilibrium.Exceptions;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.TextAsset)]
    public class Text : NamedObject {
        public Text(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            StringStart = reader.BaseStream.Position;
            var size = reader.ReadInt32();
            if (size == 0) {
                String = string.Empty;
            } else {
                reader.BaseStream.Seek(size, SeekOrigin.Current);
            }

            reader.Align();
        }

        public Text(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

        private long StringStart { get; set; }
        public string? String { get; set; }
        public override bool ShouldDeserialize => base.ShouldDeserialize || String == null;

        public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
            base.Deserialize(reader, options);
            reader.BaseStream.Seek(StringStart, SeekOrigin.Begin);
            String = reader.ReadString32();
        }

        public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            if (ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            base.Serialize(writer, options);
            writer.WriteString32(String!);
        }

        public override void Free() {
            base.Free();
            String = null;
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), String);
    }
}
