using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace UnityEngine.ProBuilder
{
	internal static class InternalUtility
	{
		[Obsolete]
		public static T[] GetComponents<T>(this IEnumerable<GameObject> gameObjects) where T : Component
		{
			List<T> list = new List<T>();
			foreach (GameObject gameObject in gameObjects)
			{
				list.AddRange(gameObject.transform.GetComponentsInChildren<T>());
			}
			return list.ToArray();
		}

		public static T[] GetComponents<T>(GameObject go) where T : Component
		{
			return go.transform.GetComponentsInChildren<T>();
		}

		public static T[] GetComponents<T>(this IEnumerable<Transform> transforms) where T : Component
		{
			List<T> list = new List<T>();
			foreach (Transform transform in transforms)
			{
				list.AddRange(transform.GetComponentsInChildren<T>());
			}
			return list.ToArray();
		}

		public static GameObject EmptyGameObjectWithTransform(Transform t)
		{
			GameObject gameObject = new GameObject();
			gameObject.transform.position = t.position;
			gameObject.transform.localRotation = t.localRotation;
			gameObject.transform.localScale = t.localScale;
			return gameObject;
		}

		public static T NextEnumValue<T>(this T current) where T : IConvertible
		{
			Array values = Enum.GetValues(current.GetType());
			int i = 0;
			for (int length = values.Length; i < length; i++)
			{
				if (current.Equals(values.GetValue(i)))
				{
					return (T)values.GetValue((i + 1) % length);
				}
			}
			return current;
		}

		public static string ControlKeyString(char character)
		{
			return character switch
			{
				'⌘' => "Control", 
				'⇧' => "Shift", 
				'⌥' => "Alt", 
				'⎇' => "Alt", 
				'⌫' => "Delete", 
				_ => character.ToString(), 
			};
		}

		public static bool TryParseColor(string value, ref Color col)
		{
			string valid = "01234567890.,";
			value = new string(value.Where((char c) => valid.Contains(c)).ToArray());
			string[] array = value.Split(',');
			if (array.Length < 4)
			{
				return false;
			}
			try
			{
				float r = float.Parse(array[0]);
				float g = float.Parse(array[1]);
				float b = float.Parse(array[2]);
				float a = float.Parse(array[3]);
				col.r = r;
				col.g = g;
				col.b = b;
				col.a = a;
			}
			catch
			{
				return false;
			}
			return true;
		}

		public static Vector3[] StringToVector3Array(string str)
		{
			List<Vector3> list = new List<Vector3>();
			str = str.Replace(" ", "");
			string[] array = str.Split('\n');
			foreach (string text in array)
			{
				if (!text.Contains("//"))
				{
					string[] array2 = text.Split(',');
					if (array2.Length >= 3 && float.TryParse(array2[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var result) && float.TryParse(array2[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var result2) && float.TryParse(array2[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var result3))
					{
						list.Add(new Vector3(result, result2, result3));
					}
				}
			}
			return list.ToArray();
		}

		public static T DemandComponent<T>(this Component component) where T : Component
		{
			return component.gameObject.DemandComponent<T>();
		}

		public static T DemandComponent<T>(this GameObject gameObject) where T : Component
		{
			if (!gameObject.TryGetComponent<T>(out var component))
			{
				return gameObject.AddComponent<T>();
			}
			return component;
		}
	}
}
