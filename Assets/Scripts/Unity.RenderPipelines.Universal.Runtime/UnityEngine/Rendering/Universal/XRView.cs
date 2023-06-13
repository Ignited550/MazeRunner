using UnityEngine.XR;

namespace UnityEngine.Rendering.Universal
{
	internal struct XRView
	{
		internal readonly Matrix4x4 projMatrix;

		internal readonly Matrix4x4 viewMatrix;

		internal readonly Rect viewport;

		internal readonly Mesh occlusionMesh;

		internal readonly int textureArraySlice;

		internal XRView(Matrix4x4 proj, Matrix4x4 view, Rect vp, int dstSlice)
		{
			projMatrix = proj;
			viewMatrix = view;
			viewport = vp;
			occlusionMesh = null;
			textureArraySlice = dstSlice;
		}

		internal XRView(XRDisplaySubsystem.XRRenderPass renderPass, XRDisplaySubsystem.XRRenderParameter renderParameter)
		{
			projMatrix = renderParameter.projection;
			viewMatrix = renderParameter.view;
			viewport = renderParameter.viewport;
			occlusionMesh = renderParameter.occlusionMesh;
			textureArraySlice = renderParameter.textureArraySlice;
			viewport.x *= renderPass.renderTargetDesc.width;
			viewport.width *= renderPass.renderTargetDesc.width;
			viewport.y *= renderPass.renderTargetDesc.height;
			viewport.height *= renderPass.renderTargetDesc.height;
		}
	}
}
