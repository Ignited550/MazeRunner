using System.Collections.Generic;

namespace UnityEngine.ProBuilder.Csg
{
	internal sealed class Polygon
	{
		public List<Vertex> vertices;

		public Plane plane;

		public Material material;

		public Polygon(List<Vertex> list, Material mat)
		{
			vertices = list;
			plane = new Plane(list[0].position, list[1].position, list[2].position);
			material = mat;
		}

		public void Flip()
		{
			vertices.Reverse();
			for (int i = 0; i < vertices.Count; i++)
			{
				vertices[i].Flip();
			}
			plane.Flip();
		}

		public override string ToString()
		{
			Vector3 normal = plane.normal;
			return "normal: " + normal.ToString();
		}
	}
}
