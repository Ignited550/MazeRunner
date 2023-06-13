using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
	public class ColorGradingLutPass : ScriptableRenderPass
	{
		private static class ShaderConstants
		{
			public static readonly int _Lut_Params = Shader.PropertyToID("_Lut_Params");

			public static readonly int _ColorBalance = Shader.PropertyToID("_ColorBalance");

			public static readonly int _ColorFilter = Shader.PropertyToID("_ColorFilter");

			public static readonly int _ChannelMixerRed = Shader.PropertyToID("_ChannelMixerRed");

			public static readonly int _ChannelMixerGreen = Shader.PropertyToID("_ChannelMixerGreen");

			public static readonly int _ChannelMixerBlue = Shader.PropertyToID("_ChannelMixerBlue");

			public static readonly int _HueSatCon = Shader.PropertyToID("_HueSatCon");

			public static readonly int _Lift = Shader.PropertyToID("_Lift");

			public static readonly int _Gamma = Shader.PropertyToID("_Gamma");

			public static readonly int _Gain = Shader.PropertyToID("_Gain");

			public static readonly int _Shadows = Shader.PropertyToID("_Shadows");

			public static readonly int _Midtones = Shader.PropertyToID("_Midtones");

			public static readonly int _Highlights = Shader.PropertyToID("_Highlights");

			public static readonly int _ShaHiLimits = Shader.PropertyToID("_ShaHiLimits");

			public static readonly int _SplitShadows = Shader.PropertyToID("_SplitShadows");

			public static readonly int _SplitHighlights = Shader.PropertyToID("_SplitHighlights");

			public static readonly int _CurveMaster = Shader.PropertyToID("_CurveMaster");

			public static readonly int _CurveRed = Shader.PropertyToID("_CurveRed");

			public static readonly int _CurveGreen = Shader.PropertyToID("_CurveGreen");

			public static readonly int _CurveBlue = Shader.PropertyToID("_CurveBlue");

			public static readonly int _CurveHueVsHue = Shader.PropertyToID("_CurveHueVsHue");

			public static readonly int _CurveHueVsSat = Shader.PropertyToID("_CurveHueVsSat");

			public static readonly int _CurveLumVsSat = Shader.PropertyToID("_CurveLumVsSat");

			public static readonly int _CurveSatVsSat = Shader.PropertyToID("_CurveSatVsSat");
		}

		private readonly Material m_LutBuilderLdr;

		private readonly Material m_LutBuilderHdr;

		private readonly GraphicsFormat m_HdrLutFormat;

		private readonly GraphicsFormat m_LdrLutFormat;

		private RenderTargetHandle m_InternalLut;

		public ColorGradingLutPass(RenderPassEvent evt, PostProcessData data)
		{
			base.profilingSampler = new ProfilingSampler("ColorGradingLutPass");
			base.renderPassEvent = evt;
			base.overrideCameraTarget = true;
			m_LutBuilderLdr = Load(data.shaders.lutBuilderLdrPS);
			m_LutBuilderHdr = Load(data.shaders.lutBuilderHdrPS);
			if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Blend))
			{
				m_HdrLutFormat = GraphicsFormat.R16G16B16A16_SFloat;
			}
			else if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Blend))
			{
				m_HdrLutFormat = GraphicsFormat.B10G11R11_UFloatPack32;
			}
			else
			{
				m_HdrLutFormat = GraphicsFormat.R8G8B8A8_UNorm;
			}
			m_LdrLutFormat = GraphicsFormat.R8G8B8A8_UNorm;
			Material Load(Shader shader)
			{
				if (shader == null)
				{
					Debug.LogError("Missing shader. " + GetType().DeclaringType.Name + " render pass will not execute. Check for missing reference in the renderer resources.");
					return null;
				}
				return CoreUtils.CreateEngineMaterial(shader);
			}
		}

		public void Setup(in RenderTargetHandle internalLut)
		{
			m_InternalLut = internalLut;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(URPProfileId.ColorGradingLUT)))
			{
				VolumeStack stack = VolumeManager.instance.stack;
				ChannelMixer component = stack.GetComponent<ChannelMixer>();
				ColorAdjustments component2 = stack.GetComponent<ColorAdjustments>();
				ColorCurves component3 = stack.GetComponent<ColorCurves>();
				LiftGammaGain component4 = stack.GetComponent<LiftGammaGain>();
				ShadowsMidtonesHighlights component5 = stack.GetComponent<ShadowsMidtonesHighlights>();
				SplitToning component6 = stack.GetComponent<SplitToning>();
				Tonemapping component7 = stack.GetComponent<Tonemapping>();
				WhiteBalance component8 = stack.GetComponent<WhiteBalance>();
				ref PostProcessingData postProcessingData = ref renderingData.postProcessingData;
				bool flag = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;
				int lutSize = postProcessingData.lutSize;
				int num = lutSize * lutSize;
				GraphicsFormat colorFormat = (flag ? m_HdrLutFormat : m_LdrLutFormat);
				Material material = (flag ? m_LutBuilderHdr : m_LutBuilderLdr);
				RenderTextureDescriptor desc = new RenderTextureDescriptor(num, lutSize, colorFormat, 0);
				desc.vrUsage = VRTextureUsage.None;
				commandBuffer.GetTemporaryRT(m_InternalLut.id, desc, FilterMode.Bilinear);
				Vector3 vector = ColorUtils.ColorBalanceToLMSCoeffs(component8.temperature.value, component8.tint.value);
				Vector4 value = new Vector4(component2.hueShift.value / 360f, component2.saturation.value / 100f + 1f, component2.contrast.value / 100f + 1f, 0f);
				Vector4 value2 = new Vector4(component.redOutRedIn.value / 100f, component.redOutGreenIn.value / 100f, component.redOutBlueIn.value / 100f, 0f);
				Vector4 value3 = new Vector4(component.greenOutRedIn.value / 100f, component.greenOutGreenIn.value / 100f, component.greenOutBlueIn.value / 100f, 0f);
				Vector4 value4 = new Vector4(component.blueOutRedIn.value / 100f, component.blueOutGreenIn.value / 100f, component.blueOutBlueIn.value / 100f, 0f);
				Vector4 value5 = new Vector4(component5.shadowsStart.value, component5.shadowsEnd.value, component5.highlightsStart.value, component5.highlightsEnd.value);
				Vector4 inShadows = component5.shadows.value;
				Vector4 inMidtones = component5.midtones.value;
				Vector4 inHighlights = component5.highlights.value;
				(Vector4, Vector4, Vector4) tuple = ColorUtils.PrepareShadowsMidtonesHighlights(in inShadows, in inMidtones, in inHighlights);
				Vector4 item = tuple.Item1;
				Vector4 item2 = tuple.Item2;
				Vector4 item3 = tuple.Item3;
				inShadows = component4.lift.value;
				inMidtones = component4.gamma.value;
				inHighlights = component4.gain.value;
				(Vector4, Vector4, Vector4) tuple2 = ColorUtils.PrepareLiftGammaGain(in inShadows, in inMidtones, in inHighlights);
				Vector4 item4 = tuple2.Item1;
				Vector4 item5 = tuple2.Item2;
				Vector4 item6 = tuple2.Item3;
				inShadows = component6.shadows.value;
				inMidtones = component6.highlights.value;
				var (value6, value7) = ColorUtils.PrepareSplitToning(in inShadows, in inMidtones, component6.balance.value);
				material.SetVector(value: new Vector4(lutSize, 0.5f / (float)num, 0.5f / (float)lutSize, (float)lutSize / ((float)lutSize - 1f)), nameID: ShaderConstants._Lut_Params);
				material.SetVector(ShaderConstants._ColorBalance, vector);
				material.SetVector(ShaderConstants._ColorFilter, component2.colorFilter.value.linear);
				material.SetVector(ShaderConstants._ChannelMixerRed, value2);
				material.SetVector(ShaderConstants._ChannelMixerGreen, value3);
				material.SetVector(ShaderConstants._ChannelMixerBlue, value4);
				material.SetVector(ShaderConstants._HueSatCon, value);
				material.SetVector(ShaderConstants._Lift, item4);
				material.SetVector(ShaderConstants._Gamma, item5);
				material.SetVector(ShaderConstants._Gain, item6);
				material.SetVector(ShaderConstants._Shadows, item);
				material.SetVector(ShaderConstants._Midtones, item2);
				material.SetVector(ShaderConstants._Highlights, item3);
				material.SetVector(ShaderConstants._ShaHiLimits, value5);
				material.SetVector(ShaderConstants._SplitShadows, value6);
				material.SetVector(ShaderConstants._SplitHighlights, value7);
				material.SetTexture(ShaderConstants._CurveMaster, component3.master.value.GetTexture());
				material.SetTexture(ShaderConstants._CurveRed, component3.red.value.GetTexture());
				material.SetTexture(ShaderConstants._CurveGreen, component3.green.value.GetTexture());
				material.SetTexture(ShaderConstants._CurveBlue, component3.blue.value.GetTexture());
				material.SetTexture(ShaderConstants._CurveHueVsHue, component3.hueVsHue.value.GetTexture());
				material.SetTexture(ShaderConstants._CurveHueVsSat, component3.hueVsSat.value.GetTexture());
				material.SetTexture(ShaderConstants._CurveLumVsSat, component3.lumVsSat.value.GetTexture());
				material.SetTexture(ShaderConstants._CurveSatVsSat, component3.satVsSat.value.GetTexture());
				if (flag)
				{
					material.shaderKeywords = null;
					switch (component7.mode.value)
					{
					case TonemappingMode.Neutral:
						material.EnableKeyword(ShaderKeywordStrings.TonemapNeutral);
						break;
					case TonemappingMode.ACES:
						material.EnableKeyword(ShaderKeywordStrings.TonemapACES);
						break;
					}
				}
				renderingData.cameraData.xr.StopSinglePass(commandBuffer);
				Blit(commandBuffer, m_InternalLut.id, m_InternalLut.id, material);
				renderingData.cameraData.xr.StartSinglePass(commandBuffer);
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}

		public override void OnFinishCameraStackRendering(CommandBuffer cmd)
		{
			cmd.ReleaseTemporaryRT(m_InternalLut.id);
		}

		public void Cleanup()
		{
			CoreUtils.Destroy(m_LutBuilderLdr);
			CoreUtils.Destroy(m_LutBuilderHdr);
		}
	}
}
