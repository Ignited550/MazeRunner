using System;
using System.Diagnostics;

namespace UnityEngine.Rendering
{
	[Serializable]
	[DebuggerDisplay("{m_Value} ({m_OverrideState})")]
	public class ClampedIntParameter : IntParameter
	{
		public int min;

		public int max;

		public override int value
		{
			get
			{
				return m_Value;
			}
			set
			{
				m_Value = Mathf.Clamp(value, min, max);
			}
		}

		public ClampedIntParameter(int value, int min, int max, bool overrideState = false)
			: base(value, overrideState)
		{
			this.min = min;
			this.max = max;
		}
	}
}
