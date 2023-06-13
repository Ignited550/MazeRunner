using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.Universal
{
	internal class Light2DCullResult : ILight2DCullResult
	{
		private List<Light2D> m_VisibleLights = new List<Light2D>();

		public List<Light2D> visibleLights => m_VisibleLights;

		public bool IsSceneLit()
		{
			if (visibleLights.Count > 0)
			{
				return true;
			}
			foreach (Light2D light in Light2DManager.lights)
			{
				if (light.lightType == Light2D.LightType.Global)
				{
					return true;
				}
			}
			return false;
		}

		public LightStats GetLightStatsByLayer(int layer)
		{
			LightStats result = default(LightStats);
			foreach (Light2D visibleLight in visibleLights)
			{
				if (visibleLight.IsLitLayer(layer))
				{
					result.totalLights++;
					if (visibleLight.useNormalMap)
					{
						result.totalNormalMapUsage++;
					}
					if (visibleLight.volumeOpacity > 0f)
					{
						result.totalVolumetricUsage++;
					}
					result.blendStylesUsed |= (uint)(1 << visibleLight.blendStyleIndex);
				}
			}
			return result;
		}

		public void SetupCulling(ref ScriptableCullingParameters cullingParameters, Camera camera)
		{
			m_VisibleLights.Clear();
			foreach (Light2D light in Light2DManager.lights)
			{
				if ((camera.cullingMask & (1 << light.gameObject.layer)) == 0)
				{
					continue;
				}
				if (light.lightType == Light2D.LightType.Global)
				{
					m_VisibleLights.Add(light);
					continue;
				}
				Vector3 position = light.boundingSphere.position;
				bool flag = false;
				for (int i = 0; i < cullingParameters.cullingPlaneCount; i++)
				{
					Plane cullingPlane = cullingParameters.GetCullingPlane(i);
					if (math.dot(position, cullingPlane.normal) + cullingPlane.distance < 0f - light.boundingSphere.radius)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					m_VisibleLights.Add(light);
				}
			}
			m_VisibleLights.Sort((Light2D l1, Light2D l2) => l1.lightOrder - l2.lightOrder);
		}
	}
}
