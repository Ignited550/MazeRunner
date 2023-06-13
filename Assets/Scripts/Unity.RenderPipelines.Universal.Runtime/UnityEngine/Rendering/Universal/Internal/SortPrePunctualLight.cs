using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal.Internal
{
	internal class SortPrePunctualLight : IComparer<DeferredTiler.PrePunctualLight>
	{
		public int Compare(DeferredTiler.PrePunctualLight a, DeferredTiler.PrePunctualLight b)
		{
			if (a.minDist < b.minDist)
			{
				return -1;
			}
			if (a.minDist > b.minDist)
			{
				return 1;
			}
			return 0;
		}
	}
}
