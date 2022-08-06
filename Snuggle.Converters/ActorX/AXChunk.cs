using System.Runtime.InteropServices;

namespace Snuggle.Converters.ActorX;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AXChunk([MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)] string ChunkName, int TypeFlag, int DataSize, int DataCount) {
    public const string Mesh = "ACTRHEAD";
    public const string Animation = "ANIXHEAD";
    public const string World = "WRLDHEAD";
    public const string MeshExtension = ".pskx";
    public const string AnimationExtension = ".psax";
    public const string WorldExtension = ".psw";
}
