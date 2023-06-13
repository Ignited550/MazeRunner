namespace UnityEngine.Rendering.Universal
{
	internal struct XRLayout
	{
		internal Camera camera;

		internal XRSystem xrSystem;

		internal XRPass CreatePass(XRPassCreateInfo passCreateInfo)
		{
			XRPass xRPass = XRPass.Create(passCreateInfo);
			xrSystem.AddPassToFrame(xRPass);
			return xRPass;
		}

		internal void AddViewToPass(XRViewCreateInfo viewCreateInfo, XRPass pass)
		{
			pass.AddView(viewCreateInfo.projMatrix, viewCreateInfo.viewMatrix, viewCreateInfo.viewport, viewCreateInfo.textureArraySlice);
		}
	}
}
