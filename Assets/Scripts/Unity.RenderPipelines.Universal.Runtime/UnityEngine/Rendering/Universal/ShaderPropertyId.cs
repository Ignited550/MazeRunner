namespace UnityEngine.Rendering.Universal
{
	internal static class ShaderPropertyId
	{
		public static readonly int glossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");

		public static readonly int subtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

		public static readonly int ambientSkyColor = Shader.PropertyToID("unity_AmbientSky");

		public static readonly int ambientEquatorColor = Shader.PropertyToID("unity_AmbientEquator");

		public static readonly int ambientGroundColor = Shader.PropertyToID("unity_AmbientGround");

		public static readonly int time = Shader.PropertyToID("_Time");

		public static readonly int sinTime = Shader.PropertyToID("_SinTime");

		public static readonly int cosTime = Shader.PropertyToID("_CosTime");

		public static readonly int deltaTime = Shader.PropertyToID("unity_DeltaTime");

		public static readonly int timeParameters = Shader.PropertyToID("_TimeParameters");

		public static readonly int scaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");

		public static readonly int worldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");

		public static readonly int screenParams = Shader.PropertyToID("_ScreenParams");

		public static readonly int projectionParams = Shader.PropertyToID("_ProjectionParams");

		public static readonly int zBufferParams = Shader.PropertyToID("_ZBufferParams");

		public static readonly int orthoParams = Shader.PropertyToID("unity_OrthoParams");

		public static readonly int viewMatrix = Shader.PropertyToID("unity_MatrixV");

		public static readonly int projectionMatrix = Shader.PropertyToID("glstate_matrix_projection");

		public static readonly int viewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixVP");

		public static readonly int inverseViewMatrix = Shader.PropertyToID("unity_MatrixInvV");

		public static readonly int inverseProjectionMatrix = Shader.PropertyToID("unity_MatrixInvP");

		public static readonly int inverseViewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixInvVP");

		public static readonly int cameraProjectionMatrix = Shader.PropertyToID("unity_CameraProjection");

		public static readonly int inverseCameraProjectionMatrix = Shader.PropertyToID("unity_CameraInvProjection");

		public static readonly int worldToCameraMatrix = Shader.PropertyToID("unity_WorldToCamera");

		public static readonly int cameraToWorldMatrix = Shader.PropertyToID("unity_CameraToWorld");

		public static readonly int sourceTex = Shader.PropertyToID("_SourceTex");

		public static readonly int scaleBias = Shader.PropertyToID("_ScaleBias");

		public static readonly int scaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");

		public static readonly int rendererColor = Shader.PropertyToID("_RendererColor");
	}
}
