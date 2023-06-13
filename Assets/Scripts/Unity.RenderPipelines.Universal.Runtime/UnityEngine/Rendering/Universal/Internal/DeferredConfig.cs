namespace UnityEngine.Rendering.Universal.Internal
{
	internal static class DeferredConfig
	{
		public const int kPreferredCBufferSize = 65536;

		public const int kPreferredStructuredBufferSize = 131072;

		public const int kTilePixelWidth = 16;

		public const int kTilePixelHeight = 16;

		public const int kTilerDepth = 3;

		public const int kTilerSubdivisions = 4;

		public const int kAvgLightPerTile = 32;

		public const int kTileDepthInfoIntermediateLevel = -1;

		public const bool kHasNativeQuadSupport = false;

		internal static bool IsOpenGL { get; set; }

		internal static bool UseCBufferForDepthRange => IsOpenGL;

		internal static bool UseCBufferForTileList => IsOpenGL;

		internal static bool UseCBufferForLightData => true;

		internal static bool UseCBufferForLightList => IsOpenGL;
	}
}
