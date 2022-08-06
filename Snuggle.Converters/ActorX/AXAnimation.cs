using System.Runtime.InteropServices;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Converters.ActorX; 

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXAnimationSequence([MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)] string Name, [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x100)] string Asset, int Parent, Vector3 Position, Quaternion Rotation, Vector3 Scale, int Flags) {
    public const string TypeName = "SEQUENCES";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXAnimationPosition(float Time, Vector3 Position) {
    public const string TypeName = "POSTRACK";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXAnimationRotation(float Time, Quaternion Rotation) {
    public const string TypeName = "ROTTRACK";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXAnimationScale(float Time, Vector3 Scale) {
    public const string TypeName = "SCLTRACK";
}
