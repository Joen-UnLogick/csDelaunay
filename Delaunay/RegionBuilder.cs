using System.Collections.Generic;

namespace csDelaunay
{
	public class RegionBuilder 
	{
		EdgeReorderer reorderer = new EdgeReorderer(null, null);
		List<Vector2f> points = new List<Vector2f>();
		List<LR> edgeOrientations;
		List<Edge> edges;
		Polygon polygon;

		public RegionBuilder()
		{
			edges = reorderer.Edges;
			edgeOrientations = reorderer.EdgeOrientations;
			polygon = new Polygon(points);
		}

		public void Release()
		{
			for (int i = 0; i < edges.Count; i++)
			{
				edges[i] = null; // null edges to allow garbage collector to collect them. List<T>.Clear still keep the backing array that references the instances
			}
		}

		public List<Vector2f> Build(List<Edge> edges, Rectf bounds)
		{
			Release();
			reorderer.ReorderEdges(edges, typeof(Vertex));
			ClipToBounds(bounds);
			if (polygon.PolyWinding() == Winding.CLOCKWISE)
			{
				points.Reverse();
			}
			return points;
		}

		private void ClipToBounds(Rectf bounds)
		{
			int n = edges.Count;
			int i = 0;
			Edge edge;

			while (i < n && !edges[i].Visible())
			{
				i++;
			}

			if (i == n)
			{
				// No edges visible
				return;
			}

			edge = edges[i];
			LR orientation = edgeOrientations[i];
			points.Clear();
			points.Add(edge.ClippedEnds[orientation]);
			points.Add(edge.ClippedEnds[LR.Other(orientation)]);

			for (int j = i + 1; j < n; j++)
			{
				edge = edges[j];
				if (!edge.Visible())
				{
					continue;
				}
				Connect(j, bounds);
			}
			// Close up the polygon by adding another corner point of the bounds if needed:
			Connect(i, bounds, true);
		}

		private void Connect(int j, Rectf bounds, bool closingUp = false)
		{
			Vector2f rightPoint = points[points.Count - 1];
			Edge newEdge = edges[j];
			LR newOrientation = edgeOrientations[j];

			// The point that must be conected to rightPoint:
			Vector2f newPoint = newEdge.ClippedEnds[newOrientation];

			if (!CloseEnough(rightPoint, newPoint))
			{
				// The points do not coincide, so they must have been clipped at the bounds;
				// see if they are on the same border of the bounds:
				if (rightPoint.x != newPoint.x && rightPoint.y != newPoint.y)
				{
					// They are on different borders of the bounds;
					// insert one or two corners of bounds as needed to hook them up:
					// (NOTE this will not be correct if the region should take up more than
					// half of the bounds rect, for then we will have gone the wrong way
					// around the bounds and included the smaller part rather than the larger)
					int rightCheck = BoundsCheck.Check(rightPoint, bounds);
					int newCheck = BoundsCheck.Check(newPoint, bounds);
					float px, py;
					if ((rightCheck & BoundsCheck.RIGHT) != 0)
					{
						px = bounds.right;

						if ((newCheck & BoundsCheck.BOTTOM) != 0)
						{
							py = bounds.bottom;
							points.Add(new Vector2f(px, py));

						}
						else if ((newCheck & BoundsCheck.TOP) != 0)
						{
							py = bounds.top;
							points.Add(new Vector2f(px, py));

						}
						else if ((newCheck & BoundsCheck.LEFT) != 0)
						{
							if (rightPoint.y - bounds.y + newPoint.y - bounds.y < bounds.height)
							{
								py = bounds.top;
							}
							else
							{
								py = bounds.bottom;
							}
							points.Add(new Vector2f(px, py));
							points.Add(new Vector2f(bounds.left, py));
						}
					}
					else if ((rightCheck & BoundsCheck.LEFT) != 0)
					{
						px = bounds.left;

						if ((newCheck & BoundsCheck.BOTTOM) != 0)
						{
							py = bounds.bottom;
							points.Add(new Vector2f(px, py));

						}
						else if ((newCheck & BoundsCheck.TOP) != 0)
						{
							py = bounds.top;
							points.Add(new Vector2f(px, py));

						}
						else if ((newCheck & BoundsCheck.RIGHT) != 0)
						{
							if (rightPoint.y - bounds.y + newPoint.y - bounds.y < bounds.height)
							{
								py = bounds.top;
							}
							else
							{
								py = bounds.bottom;
							}
							points.Add(new Vector2f(px, py));
							points.Add(new Vector2f(bounds.right, py));
						}
					}
					else if ((rightCheck & BoundsCheck.TOP) != 0)
					{
						py = bounds.top;

						if ((newCheck & BoundsCheck.RIGHT) != 0)
						{
							px = bounds.right;
							points.Add(new Vector2f(px, py));

						}
						else if ((newCheck & BoundsCheck.LEFT) != 0)
						{
							px = bounds.left;
							points.Add(new Vector2f(px, py));

						}
						else if ((newCheck & BoundsCheck.BOTTOM) != 0)
						{
							if (rightPoint.x - bounds.x + newPoint.x - bounds.x < bounds.width)
							{
								px = bounds.left;
							}
							else
							{
								px = bounds.right;
							}
							points.Add(new Vector2f(px, py));
							points.Add(new Vector2f(px, bounds.bottom));
						}
					}
					else if ((rightCheck & BoundsCheck.BOTTOM) != 0)
					{
						py = bounds.bottom;

						if ((newCheck & BoundsCheck.RIGHT) != 0)
						{
							px = bounds.right;
							points.Add(new Vector2f(px, py));

						}
						else if ((newCheck & BoundsCheck.LEFT) != 0)
						{
							px = bounds.left;
							points.Add(new Vector2f(px, py));

						}
						else if ((newCheck & BoundsCheck.TOP) != 0)
						{
							if (rightPoint.x - bounds.x + newPoint.x - bounds.x < bounds.width)
							{
								px = bounds.left;
							}
							else
							{
								px = bounds.right;
							}
							points.Add(new Vector2f(px, py));
							points.Add(new Vector2f(px, bounds.top));
						}
					}
				}
				if (closingUp)
				{
					// newEdge's ends have already been added
					return;
				}
				points.Add(newPoint);
			}
			Vector2f newRightPoint = newEdge.ClippedEnds[LR.Other(newOrientation)];
			if (!CloseEnough(points[0], newRightPoint))
			{
				points.Add(newRightPoint);
			}
		}

		private const float EPSILON = 0.005f;
		private static bool CloseEnough(Vector2f p0, Vector2f p1)
		{
			return (p0 - p1).magnitude < EPSILON;
		}
	}
}