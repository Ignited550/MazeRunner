using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Internal
{
	public class ForwardLights
	{
		private static class LightConstantBuffer
		{
			public static int _MainLightPosition;

			public static int _MainLightColor;

			public static int _MainLightOcclusionProbesChannel;

			public static int _AdditionalLightsCount;

			public static int _AdditionalLightsPosition;

			public static int _AdditionalLightsColor;

			public static int _AdditionalLightsAttenuation;

			public static int _AdditionalLightsSpotDir;

			public static int _AdditionalLightOcclusionProbeChannel;
		}

		private int m_AdditionalLightsBufferId;

		private int m_AdditionalLightsIndicesId;

		private const string k_SetupLightConstants = "Setup Light Constants";

		private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Setup Light Constants");

		private MixedLightingSetup m_MixedLightingSetup;

		private Vector4[] m_AdditionalLightPositions;

		private Vector4[] m_AdditionalLightColors;

		private Vector4[] m_AdditionalLightAttenuations;

		private Vector4[] m_AdditionalLightSpotDirections;

		private Vector4[] m_AdditionalLightOcclusionProbeChannels;

		private bool m_UseStructuredBuffer;

		public ForwardLights()
		{
			m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;
			LightConstantBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
			LightConstantBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
			LightConstantBuffer._MainLightOcclusionProbesChannel = Shader.PropertyToID("_MainLightOcclusionProbes");
			LightConstantBuffer._AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");
			if (m_UseStructuredBuffer)
			{
				m_AdditionalLightsBufferId = Shader.PropertyToID("_AdditionalLightsBuffer");
				m_AdditionalLightsIndicesId = Shader.PropertyToID("_AdditionalLightsIndices");
				return;
			}
			LightConstantBuffer._AdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
			LightConstantBuffer._AdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
			LightConstantBuffer._AdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
			LightConstantBuffer._AdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");
			LightConstantBuffer._AdditionalLightOcclusionProbeChannel = Shader.PropertyToID("_AdditionalLightsOcclusionProbes");
			int maxVisibleAdditionalLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
			m_AdditionalLightPositions = new Vector4[maxVisibleAdditionalLights];
			m_AdditionalLightColors = new Vector4[maxVisibleAdditionalLights];
			m_AdditionalLightAttenuations = new Vector4[maxVisibleAdditionalLights];
			m_AdditionalLightSpotDirections = new Vector4[maxVisibleAdditionalLights];
			m_AdditionalLightOcclusionProbeChannels = new Vector4[maxVisibleAdditionalLights];
		}

		public void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			int additionalLightsCount = renderingData.lightData.additionalLightsCount;
			bool shadeAdditionalLightsPerVertex = renderingData.lightData.shadeAdditionalLightsPerVertex;
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
			{
				SetupShaderLightConstants(commandBuffer, ref renderingData);
				CoreUtils.SetKeyword(commandBuffer, ShaderKeywordStrings.AdditionalLightsVertex, additionalLightsCount > 0 && shadeAdditionalLightsPerVertex);
				CoreUtils.SetKeyword(commandBuffer, ShaderKeywordStrings.AdditionalLightsPixel, additionalLightsCount > 0 && !shadeAdditionalLightsPerVertex);
				bool flag = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.ShadowMask;
				bool flag2 = flag && QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask;
				bool flag3 = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.Subtractive;
				CoreUtils.SetKeyword(commandBuffer, ShaderKeywordStrings.LightmapShadowMixing, flag3 || flag2);
				CoreUtils.SetKeyword(commandBuffer, ShaderKeywordStrings.ShadowsShadowMask, flag);
				CoreUtils.SetKeyword(commandBuffer, ShaderKeywordStrings.MixedLightingSubtractive, flag3);
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}

		private void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel)
		{
			UniversalRenderPipeline.InitializeLightConstants_Common(lights, lightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionProbeChannel);
			if (lightIndex < 0)
			{
				return;
			}
			VisibleLight visibleLight = lights[lightIndex];
			Light light = visibleLight.light;
			if (!(light == null) && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed && visibleLight.light.shadows != 0 && m_MixedLightingSetup == MixedLightingSetup.None)
			{
				switch (light.bakingOutput.mixedLightingMode)
				{
				case MixedLightingMode.Subtractive:
					m_MixedLightingSetup = MixedLightingSetup.Subtractive;
					break;
				case MixedLightingMode.Shadowmask:
					m_MixedLightingSetup = MixedLightingSetup.ShadowMask;
					break;
				}
			}
		}

		private void SetupShaderLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
		{
			m_MixedLightingSetup = MixedLightingSetup.None;
			SetupMainLightConstants(cmd, ref renderingData.lightData);
			SetupAdditionalLightConstants(cmd, ref renderingData);
		}

		private void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
		{
			InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out var lightPos, out var lightColor, out var _, out var _, out var lightOcclusionProbeChannel);
			cmd.SetGlobalVector(LightConstantBuffer._MainLightPosition, lightPos);
			cmd.SetGlobalVector(LightConstantBuffer._MainLightColor, lightColor);
			cmd.SetGlobalVector(LightConstantBuffer._MainLightOcclusionProbesChannel, lightOcclusionProbeChannel);
		}

		private void SetupAdditionalLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
		{
			ref LightData lightData = ref renderingData.lightData;
			CullingResults cullResults = renderingData.cullResults;
			NativeArray<VisibleLight> visibleLights = lightData.visibleLights;
			int maxVisibleAdditionalLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
			int num = SetupPerObjectLightIndices(cullResults, ref lightData);
			if (num > 0)
			{
				if (m_UseStructuredBuffer)
				{
					NativeArray<ShaderInput.LightData> data = new NativeArray<ShaderInput.LightData>(num, Allocator.Temp);
					int i = 0;
					int num2 = 0;
					ShaderInput.LightData value = default(ShaderInput.LightData);
					for (; i < visibleLights.Length; i++)
					{
						if (num2 >= maxVisibleAdditionalLights)
						{
							break;
						}
						_ = visibleLights[i];
						if (lightData.mainLightIndex != i)
						{
							InitializeLightConstants(visibleLights, i, out value.position, out value.color, out value.attenuation, out value.spotDirection, out value.occlusionProbeChannels);
							data[num2] = value;
							num2++;
						}
					}
					ComputeBuffer lightDataBuffer = ShaderData.instance.GetLightDataBuffer(num);
					lightDataBuffer.SetData(data);
					int lightAndReflectionProbeIndexCount = cullResults.lightAndReflectionProbeIndexCount;
					ComputeBuffer lightIndicesBuffer = ShaderData.instance.GetLightIndicesBuffer(lightAndReflectionProbeIndexCount);
					cmd.SetGlobalBuffer(m_AdditionalLightsBufferId, lightDataBuffer);
					cmd.SetGlobalBuffer(m_AdditionalLightsIndicesId, lightIndicesBuffer);
					data.Dispose();
				}
				else
				{
					int j = 0;
					int num3 = 0;
					for (; j < visibleLights.Length; j++)
					{
						if (num3 >= maxVisibleAdditionalLights)
						{
							break;
						}
						_ = visibleLights[j];
						if (lightData.mainLightIndex != j)
						{
							InitializeLightConstants(visibleLights, j, out m_AdditionalLightPositions[num3], out m_AdditionalLightColors[num3], out m_AdditionalLightAttenuations[num3], out m_AdditionalLightSpotDirections[num3], out m_AdditionalLightOcclusionProbeChannels[num3]);
							num3++;
						}
					}
					cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsPosition, m_AdditionalLightPositions);
					cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsColor, m_AdditionalLightColors);
					cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsAttenuation, m_AdditionalLightAttenuations);
					cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsSpotDir, m_AdditionalLightSpotDirections);
					cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightOcclusionProbeChannel, m_AdditionalLightOcclusionProbeChannels);
				}
				cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCount, new Vector4(lightData.maxPerObjectAdditionalLightsCount, 0f, 0f, 0f));
			}
			else
			{
				cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCount, Vector4.zero);
			}
		}

		private int SetupPerObjectLightIndices(CullingResults cullResults, ref LightData lightData)
		{
			if (lightData.additionalLightsCount == 0)
			{
				return lightData.additionalLightsCount;
			}
			NativeArray<VisibleLight> visibleLights = lightData.visibleLights;
			NativeArray<int> lightIndexMap = cullResults.GetLightIndexMap(Allocator.Temp);
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < visibleLights.Length; i++)
			{
				if (num2 >= UniversalRenderPipeline.maxVisibleAdditionalLights)
				{
					break;
				}
				_ = visibleLights[i];
				if (i == lightData.mainLightIndex)
				{
					lightIndexMap[i] = -1;
					num++;
				}
				else
				{
					lightIndexMap[i] -= num;
					num2++;
				}
			}
			for (int j = num + num2; j < lightIndexMap.Length; j++)
			{
				lightIndexMap[j] = -1;
			}
			cullResults.SetLightIndexMap(lightIndexMap);
			if (m_UseStructuredBuffer && num2 > 0)
			{
				int lightAndReflectionProbeIndexCount = cullResults.lightAndReflectionProbeIndexCount;
				cullResults.FillLightAndReflectionProbeIndices(ShaderData.instance.GetLightIndicesBuffer(lightAndReflectionProbeIndexCount));
			}
			lightIndexMap.Dispose();
			return num2;
		}
	}
}
