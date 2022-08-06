using System.Runtime.InteropServices;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Converters.ActorX; 

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXWorldActor([MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)] string Name, [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x100)] string Asset, int Parent, Vector3 Position, Quaternion Rotation, Vector3 Scale, int Flags) {
    public const string TypeName = "WORLDACTORS";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXWorldLandscape([MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x100)] string Name, int ActorId, int X, int Y, int Type, int Size, int Bias, Vector2 Offset, int DimX, int DimY) {
    public const string TypeName = "LANDSCAPE";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXWorldMaterial(int ActorId, int MaterialId, [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)] string Name) {
    public const string TypeName = "INSTMATERIAL";
}
