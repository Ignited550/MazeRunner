namespace UnityEngine.ProBuilder.Csg
{
	internal static class Boolean
	{
		internal const float k_Epsilon = 1E-05f;

		public static Model Union(GameObject lhs, GameObject rhs)
		{
			Model model = new Model(lhs);
			Model model2 = new Model(rhs);
			Node a = new Node(model.ToPolygons());
			Node b = new Node(model2.ToPolygons());
			return new Model(Node.Union(a, b).AllPolygons());
		}

		public static Model Subtract(GameObject lhs, GameObject rhs)
		{
			Model model = new Model(lhs);
			Model model2 = new Model(rhs);
			Node a = new Node(model.ToPolygons());
			Node b = new Node(model2.ToPolygons());
			return new Model(Node.Subtract(a, b).AllPolygons());
		}

		public static Model Intersect(GameObject lhs, GameObject rhs)
		{
			Model model = new Model(lhs);
			Model model2 = new Model(rhs);
			Node a = new Node(model.ToPolygons());
			Node b = new Node(model2.ToPolygons());
			return new Model(Node.Intersect(a, b).AllPolygons());
		}
	}
}
