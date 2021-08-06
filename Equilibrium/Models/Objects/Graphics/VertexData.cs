﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Equilibrium.Exceptions;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record VertexData(
        uint CurrentChannels,
        uint VertexCount,
        List<ChannelInfo> Channels) {
        private long DataStart { get; init; } = -1;

        [JsonIgnore]
        public Memory<byte>? Data { get; set; }

        public static VertexData Default { get; } = new(0, 0, new List<ChannelInfo>());

        [JsonIgnore]
        public bool ShouldDeserialize => Data == null;

        public static VertexData FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var currentChannels = 0U;
            if (file.Version < UnityVersionRegister.Unity2018) {
                currentChannels = reader.ReadUInt32();
            }

            var vertexCount = reader.ReadUInt32();

            var channelCount = reader.ReadInt32();
            var channels = new List<ChannelInfo>();
            channels.EnsureCapacity(channelCount);

            for (var i = 0; i < channelCount; ++i) {
                channels.Add(ChannelInfo.FromReader(reader, file));
            }

            var dataStart = reader.BaseStream.Position;
            var dataCount = reader.ReadInt32();
            reader.BaseStream.Seek(dataCount, SeekOrigin.Current);
            reader.Align();

            return new VertexData(currentChannels, vertexCount, channels) { DataStart = dataStart };
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            if (ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            if (targetVersion < UnityVersionRegister.Unity2018) {
                writer.Write(CurrentChannels);
            }

            writer.Write(VertexCount);
            writer.Write(Channels.Count);

            foreach (var channel in Channels) {
                channel.ToWriter(writer, serializedFile, targetVersion);
            }

            writer.Write(Data!.Value.Length);
            writer.WriteMemory(Data);
        }

        public void Deserialize(BiEndianBinaryReader reader, SerializedFile serializedFile, ObjectDeserializationOptions options) {
            reader.BaseStream.Seek(DataStart, SeekOrigin.Begin);
            var dataCount = reader.ReadInt32();
            Data = reader.ReadMemory(dataCount);
        }
    }
}
