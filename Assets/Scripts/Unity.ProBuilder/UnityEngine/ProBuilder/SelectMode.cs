using System;

namespace UnityEngine.ProBuilder
{
	[Flags]
	public enum SelectMode
	{
		None = 0,
		Object = 1,
		Vertex = 2,
		Edge = 4,
		Face = 8,
		TextureFace = 0x10,
		TextureEdge = 0x20,
		TextureVertex = 0x40,
		InputTool = 0x80,
		Any = 0xFFFF
	}
}
