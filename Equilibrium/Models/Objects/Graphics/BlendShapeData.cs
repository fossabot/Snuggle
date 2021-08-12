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
    public record BlendShapeData(
        List<MeshBlendShape> Shapes,
        List<MeshBlendShapeChannel> Channels) {
        private long VerticesOffset { get; init; } = -1;
        private long WeightsOffset { get; init; } = -1;

        [JsonIgnore]
        public List<BlendShapeVertex>? Vertices { get; set; }

        [JsonIgnore]
        public Memory<float>? Weights { get; set; }

        public static BlendShapeData Default { get; } = new(new List<MeshBlendShape>(), new List<MeshBlendShapeChannel>());

        [JsonIgnore]
        public bool ShouldDeserialize => Vertices == null || Weights == null;

        public static BlendShapeData FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var verticesOffset = reader.BaseStream.Position;
            var verticesCount = reader.ReadInt32();
            reader.BaseStream.Seek(verticesCount * 10 * 4, SeekOrigin.Current); // Vector3 + Vector3 + Vector3 + uint

            var shapeCount = reader.ReadInt32();
            var shapes = new List<MeshBlendShape>();
            shapes.EnsureCapacity(shapeCount);
            for (var i = 0; i < shapeCount; ++i) {
                shapes.Add(MeshBlendShape.FromReader(reader, file));
            }

            var channelCount = reader.ReadInt32();
            var channels = new List<MeshBlendShapeChannel>();
            channels.EnsureCapacity(channelCount);
            for (var i = 0; i < channelCount; ++i) {
                channels.Add(MeshBlendShapeChannel.FromReader(reader, file));
            }

            var weightsOffset = reader.BaseStream.Position;
            var weightsCount = reader.ReadInt32();
            reader.BaseStream.Seek(weightsCount * 4, SeekOrigin.Current);

            return new BlendShapeData(shapes, channels) { VerticesOffset = verticesOffset, WeightsOffset = weightsOffset };
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            if (ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            writer.Write(Vertices!.Count);
            foreach (var vertex in Vertices) {
                vertex.ToWriter(writer, serializedFile, targetVersion);
            }

            writer.Write(Shapes.Count);
            foreach (var shape in Shapes) {
                shape.ToWriter(writer, serializedFile, targetVersion);
            }

            writer.Write(Channels.Count);
            foreach (var channel in Channels) {
                channel.ToWriter(writer, serializedFile, targetVersion);
            }

            writer.Write(Weights!.Value.Length);
            writer.WriteMemory(Weights);
        }

        public void Deserialize(BiEndianBinaryReader reader, SerializedFile serializedFile, ObjectDeserializationOptions options) {
            reader.BaseStream.Seek(VerticesOffset, SeekOrigin.Begin);
            var verticesCount = reader.ReadInt32();
            Vertices = new List<BlendShapeVertex>();
            Vertices.EnsureCapacity(verticesCount);
            for (var i = 0; i < verticesCount; ++i) {
                Vertices.Add(BlendShapeVertex.FromReader(reader, serializedFile));
            }

            reader.BaseStream.Seek(WeightsOffset, SeekOrigin.Begin);
            var weightsCount = reader.ReadInt32();
            Weights = reader.ReadMemory<float>(weightsCount);
        }
    }
}
