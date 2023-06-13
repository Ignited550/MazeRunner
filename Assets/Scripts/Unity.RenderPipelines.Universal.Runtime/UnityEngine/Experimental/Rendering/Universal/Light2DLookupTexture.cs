using System;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
	internal static class Light2DLookupTexture
	{
		private static Texture2D s_PointLightLookupTexture;

		private static Texture2D s_FalloffLookupTexture;

		public static Texture GetLightLookupTexture()
		{
			if (s_PointLightLookupTexture == null)
			{
				s_PointLightLookupTexture = CreatePointLightLookupTexture();
			}
			return s_PointLightLookupTexture;
		}

		public static Texture GetFalloffLookupTexture()
		{
			if (s_FalloffLookupTexture == null)
			{
				s_FalloffLookupTexture = CreateFalloffLookupTexture();
			}
			return s_FalloffLookupTexture;
		}

		private static Texture2D CreatePointLightLookupTexture()
		{
			GraphicsFormat format = GraphicsFormat.R8G8B8A8_UNorm;
			if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.SetPixels))
			{
				format = GraphicsFormat.R16G16B16A16_SFloat;
			}
			else if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R32G32B32A32_SFloat, FormatUsage.SetPixels))
			{
				format = GraphicsFormat.R32G32B32A32_SFloat;
			}
			Texture2D texture2D = new Texture2D(256, 256, format, TextureCreationFlags.None);
			texture2D.filterMode = FilterMode.Bilinear;
			texture2D.wrapMode = TextureWrapMode.Clamp;
			Vector2 vector = new Vector2(128f, 128f);
			for (int i = 0; i < 256; i++)
			{
				for (int j = 0; j < 256; j++)
				{
					Vector2 vector2 = new Vector2(j, i);
					float num = Vector2.Distance(vector2, vector);
					Vector2 vector3 = vector2 - vector;
					Vector2 vector4 = vector - vector2;
					vector4.Normalize();
					float r = ((j != 255 && i != 255) ? Mathf.Clamp(1f - 2f * num / 256f, 0f, 1f) : 0f);
					float num2 = Mathf.Acos(Vector2.Dot(Vector2.down, vector3.normalized)) / (float)Math.PI;
					float g = Mathf.Clamp(1f - num2, 0f, 1f);
					float x = vector4.x;
					float y = vector4.y;
					Color color = new Color(r, g, x, y);
					texture2D.SetPixel(j, i, color);
				}
			}
			texture2D.Apply();
			return texture2D;
		}

		private static Texture2D CreateFalloffLookupTexture()
		{
			Texture2D texture2D = new Texture2D(2048, 128, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
			texture2D.filterMode = FilterMode.Bilinear;
			texture2D.wrapMode = TextureWrapMode.Clamp;
			for (int i = 0; i < 192; i++)
			{
				float num = (float)(i + 32) / 256f;
				float p = Mathf.Log(0f - num + 1f) / Mathf.Log(num);
				for (int j = 0; j < 2048; j++)
				{
					float r = Mathf.Pow((float)j / 2048f, p);
					Color color = new Color(r, 0f, 0f, 1f);
					if (i >= 32 && i < 160)
					{
						texture2D.SetPixel(j, i - 32, color);
					}
				}
			}
			texture2D.Apply();
			return texture2D;
		}
	}
}
