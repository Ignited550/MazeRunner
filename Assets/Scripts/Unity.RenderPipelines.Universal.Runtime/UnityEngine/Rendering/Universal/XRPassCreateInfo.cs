namespace UnityEngine.Rendering.Universal
{
	internal struct XRPassCreateInfo
	{
		public int multipassId;

		public int cullingPassId;

		public RenderTexture renderTarget;

		public RenderTextureDescriptor renderTargetDesc;

		public bool renderTargetIsRenderTexture;

		public ScriptableCullingParameters cullingParameters;

		public XRPass.CustomMirrorView customMirrorView;
	}
}
