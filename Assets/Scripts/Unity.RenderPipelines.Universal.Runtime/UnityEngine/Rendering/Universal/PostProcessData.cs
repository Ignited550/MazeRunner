using System;

namespace UnityEngine.Rendering.Universal
{
	[Serializable]
	public class PostProcessData : ScriptableObject
	{
		[Serializable]
		[ReloadGroup]
		public sealed class ShaderResources
		{
			[Reload("Shaders/PostProcessing/StopNaN.shader", ReloadAttribute.Package.Root)]
			public Shader stopNanPS;

			[Reload("Shaders/PostProcessing/SubpixelMorphologicalAntialiasing.shader", ReloadAttribute.Package.Root)]
			public Shader subpixelMorphologicalAntialiasingPS;

			[Reload("Shaders/PostProcessing/GaussianDepthOfField.shader", ReloadAttribute.Package.Root)]
			public Shader gaussianDepthOfFieldPS;

			[Reload("Shaders/PostProcessing/BokehDepthOfField.shader", ReloadAttribute.Package.Root)]
			public Shader bokehDepthOfFieldPS;

			[Reload("Shaders/PostProcessing/CameraMotionBlur.shader", ReloadAttribute.Package.Root)]
			public Shader cameraMotionBlurPS;

			[Reload("Shaders/PostProcessing/PaniniProjection.shader", ReloadAttribute.Package.Root)]
			public Shader paniniProjectionPS;

			[Reload("Shaders/PostProcessing/LutBuilderLdr.shader", ReloadAttribute.Package.Root)]
			public Shader lutBuilderLdrPS;

			[Reload("Shaders/PostProcessing/LutBuilderHdr.shader", ReloadAttribute.Package.Root)]
			public Shader lutBuilderHdrPS;

			[Reload("Shaders/PostProcessing/Bloom.shader", ReloadAttribute.Package.Root)]
			public Shader bloomPS;

			[Reload("Shaders/PostProcessing/UberPost.shader", ReloadAttribute.Package.Root)]
			public Shader uberPostPS;

			[Reload("Shaders/PostProcessing/FinalPost.shader", ReloadAttribute.Package.Root)]
			public Shader finalPostPassPS;
		}

		[Serializable]
		[ReloadGroup]
		public sealed class TextureResources
		{
			[Reload("Textures/BlueNoise16/L/LDR_LLL1_{0}.png", 0, 32, ReloadAttribute.Package.Root)]
			public Texture2D[] blueNoise16LTex;

			[Reload(new string[] { "Textures/FilmGrain/Thin01.png", "Textures/FilmGrain/Thin02.png", "Textures/FilmGrain/Medium01.png", "Textures/FilmGrain/Medium02.png", "Textures/FilmGrain/Medium03.png", "Textures/FilmGrain/Medium04.png", "Textures/FilmGrain/Medium05.png", "Textures/FilmGrain/Medium06.png", "Textures/FilmGrain/Large01.png", "Textures/FilmGrain/Large02.png" }, ReloadAttribute.Package.Root)]
			public Texture2D[] filmGrainTex;

			[Reload("Textures/SMAA/AreaTex.tga", ReloadAttribute.Package.Root)]
			public Texture2D smaaAreaTex;

			[Reload("Textures/SMAA/SearchTex.tga", ReloadAttribute.Package.Root)]
			public Texture2D smaaSearchTex;
		}

		public ShaderResources shaders;

		public TextureResources textures;
	}
}
