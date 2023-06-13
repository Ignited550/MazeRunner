using UnityEngine.SceneManagement;

namespace UnityEngine.ProBuilder
{
	[DisallowMultipleComponent]
	internal sealed class TriggerBehaviour : EntityBehaviour
	{
		public override void Initialize()
		{
			Collider collider = base.gameObject.GetComponent<Collider>();
			if (!collider)
			{
				collider = base.gameObject.AddComponent<MeshCollider>();
			}
			MeshCollider meshCollider = collider as MeshCollider;
			if ((bool)meshCollider)
			{
				meshCollider.convex = true;
			}
			collider.isTrigger = true;
			SetMaterial(BuiltinMaterials.triggerMaterial);
			Renderer component = GetComponent<Renderer>();
			if (component != null)
			{
				component.hideFlags = HideFlags.DontSaveInBuild;
			}
		}

		public override void OnEnterPlayMode()
		{
			Renderer component = GetComponent<Renderer>();
			if (component != null)
			{
				component.enabled = false;
			}
		}

		public override void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Renderer component = GetComponent<Renderer>();
			if (component != null)
			{
				component.enabled = false;
			}
		}
	}
}
