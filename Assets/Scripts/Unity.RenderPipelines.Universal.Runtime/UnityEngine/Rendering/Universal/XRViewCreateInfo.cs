namespace UnityEngine.Rendering.Universal
{
	internal struct XRViewCreateInfo
	{
		public Matrix4x4 projMatrix;

		public Matrix4x4 viewMatrix;

		public Rect viewport;

		public int textureArraySlice;
	}
}
