using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.Universal
{
	internal class ShadowCasterGroup2DManager
	{
		private static List<ShadowCasterGroup2D> s_ShadowCasterGroups;

		public static List<ShadowCasterGroup2D> shadowCasterGroups => s_ShadowCasterGroups;

		public static void AddShadowCasterGroupToList(ShadowCasterGroup2D shadowCaster, List<ShadowCasterGroup2D> list)
		{
			int num = 0;
			for (num = 0; num < list.Count && shadowCaster.GetShadowGroup() != list[num].GetShadowGroup(); num++)
			{
			}
			list.Insert(num, shadowCaster);
		}

		public static void RemoveShadowCasterGroupFromList(ShadowCasterGroup2D shadowCaster, List<ShadowCasterGroup2D> list)
		{
			list.Remove(shadowCaster);
		}

		private static CompositeShadowCaster2D FindTopMostCompositeShadowCaster(ShadowCaster2D shadowCaster)
		{
			CompositeShadowCaster2D result = null;
			Transform parent = shadowCaster.transform.parent;
			while (parent != null)
			{
				CompositeShadowCaster2D component = parent.GetComponent<CompositeShadowCaster2D>();
				if (component != null)
				{
					result = component;
				}
				parent = parent.parent;
			}
			return result;
		}

		public static bool AddToShadowCasterGroup(ShadowCaster2D shadowCaster, ref ShadowCasterGroup2D shadowCasterGroup)
		{
			ShadowCasterGroup2D shadowCasterGroup2D = FindTopMostCompositeShadowCaster(shadowCaster);
			if (shadowCasterGroup2D == null)
			{
				shadowCasterGroup2D = shadowCaster.GetComponent<ShadowCaster2D>();
			}
			if (shadowCasterGroup2D != null && shadowCasterGroup != shadowCasterGroup2D)
			{
				shadowCasterGroup2D.RegisterShadowCaster2D(shadowCaster);
				shadowCasterGroup = shadowCasterGroup2D;
				return true;
			}
			return false;
		}

		public static void RemoveFromShadowCasterGroup(ShadowCaster2D shadowCaster, ShadowCasterGroup2D shadowCasterGroup)
		{
			if (shadowCasterGroup != null)
			{
				shadowCasterGroup.UnregisterShadowCaster2D(shadowCaster);
			}
		}

		public static void AddGroup(ShadowCasterGroup2D group)
		{
			if (!(group == null))
			{
				if (s_ShadowCasterGroups == null)
				{
					s_ShadowCasterGroups = new List<ShadowCasterGroup2D>();
				}
				AddShadowCasterGroupToList(group, s_ShadowCasterGroups);
			}
		}

		public static void RemoveGroup(ShadowCasterGroup2D group)
		{
			if (group != null && s_ShadowCasterGroups != null)
			{
				RemoveShadowCasterGroupFromList(group, s_ShadowCasterGroups);
			}
		}
	}
}
