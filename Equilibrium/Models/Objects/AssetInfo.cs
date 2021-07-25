﻿using Equilibrium.Implementations;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects {
    [PublicAPI]
    public record AssetInfo(
        int PreloadIndex,
        int PreloadSize,
        PPtr<SerializedObject> Asset) {
        public static AssetInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) => new(reader.ReadInt32(), reader.ReadInt32(), PPtr<SerializedObject>.FromReader(reader, file));
    }
}
