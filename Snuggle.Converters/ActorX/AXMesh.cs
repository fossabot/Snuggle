using System.Runtime.InteropServices;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Converters.ActorX;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshPoints(Vector3 Point) {
    public const string TypeName = "PNTS0000";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshWedge(int VertexIndex, Vector2 UV, byte MaterialId = 0) {
    public const string TypeName = "VTXW3200";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshFace(ushort A, ushort B, ushort C, byte MaterialId = 0, byte AuxiliaryMaterialId = 0, int Group = 0) {
    public const string TypeName = "FACE0000";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshFace32(uint A, uint B, uint C, byte MaterialId = 0, byte AuxiliaryMaterialId = 0, int Group = 0){
    public const string TypeName = "FACE3200";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshNormals(Vector3 Normals) {
    public const string TypeName = "VTXNORMS";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshTangents(Vector4 Normals){
    public const string TypeName = "VTXTANGS";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshMaterial([MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)] string Name, int TextureId, int PolyFlags = 0, int AuxiliaryTextureId = 0, int LODBias = 0, int LODStyle = 0) {
    public const string TypeName = "MATT0000";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshBone([MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)] string Name, int Flags, int NumChildren, int ParentId, Quaternion Rotation, Vector3 Position, float Length, Vector3 Scale){
    public const string TypeName = "BONENAMES";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshWeight(float Weight, uint VertexId, uint BoneId) {
    public const string TypeName = "RAWWEIGHTS";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshColor(byte R, byte G, byte B, byte A) {
    public const string TypeName = "VERTEXCOLOR";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshUV(Vector2 UV) {
    public const string TypeName = "EXTRAUVS";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshMorph(uint VertexId, Vector3 Position) {
    public const string TypeName = "MORPHTARGET";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshMorphName([MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)] string Name){
    public const string TypeName = "MORPHNAMES";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXMeshShape([MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)] string Name, int Type, Vector3 Center, Quaternion Rotation, Vector3 Scale){
    public const string TypeName = "SHAPEELEMS";
}
