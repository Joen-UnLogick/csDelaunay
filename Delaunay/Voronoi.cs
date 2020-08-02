using System;
using System.Collections.Generic;

namespace csDelaunay {

	public class Voronoi {

		private VoronoiManager manager;

		private SiteList sites;

		private List<Edge> edges;
		public List<Edge> Edges {get{return edges;}}

		// TODO generalize this so it doesn't have to be a rectangle;
		// then we can make the fractal voronois-within-voronois
		private Rectf plotBounds;
		public Rectf PlotBounds {get{return plotBounds;}}

		public List<Site> Sites { get { return sites.Sites; } }

		private Random weigthDistributor;
		private List<Vertex> vertices;

		public void Dispose() {
			sites.Dispose();

			foreach (Edge e in edges) {
				e.Dispose();
			}
			manager.Release(edges);

			foreach (Vertex ve in vertices)
			{
				ve.Dispose(manager);
			}
			manager.Release(vertices);

			plotBounds = Rectf.zero;
		}

		public Voronoi(List<Vector2f> points, Rectf plotBounds)
			: this(new VoronoiManager(points.Count), points, plotBounds, 0)
		{
		}

		public Voronoi(List<Vector2f> points, Rectf plotBounds, int lloydIterations) 
			: this(new VoronoiManager(points.Count), points, plotBounds, lloydIterations)
		{
		}

		public Voronoi(VoronoiManager manager, List<Vector2f> points, Rectf plotBounds, int lloydIterations)
		{
			this.manager = manager;
			sites = new SiteList(manager);
			weigthDistributor = new Random();
			Init(points, plotBounds);
			if (lloydIterations > 0)
			{
				LloydRelaxation(lloydIterations);
			}
		}

		public void Rebuild(List<Vector2f> points, Rectf plotBounds, int lloydIterations)
		{
			Dispose();
			Init(points, plotBounds);
			if (lloydIterations > 0)
			{
				LloydRelaxation(lloydIterations);
			}
		}

		private void Init(List<Vector2f> points, Rectf plotBounds) {
			sites.Init(points.Count);
			edges = manager.ObtainListEdge(points.Count * 3);
			vertices = manager.ObtainListVertex(points.Count * 3);
			AddSites(points);
			this.plotBounds = plotBounds;
			
			FortunesAlgorithm();
		}

		private void AddSites(List<Vector2f> points) {
			for (int i = 0; i < points.Count; i++) {
				AddSite(points[i], i);
			}
		}

		private void AddSite(Vector2f p, int index) {
			float weigth = (float)weigthDistributor.NextDouble() * 100;
			Site site = manager.ObtainSite(p, index, weigth);
			sites.Add(site);
		}

		public List<Circle> Circles() {
			return sites.Circles();
		}

		public List<LineSegment> VoronoiBoundarayForSite(Vector2f coord) {
			return LineSegment.VisibleLineSegments(Edge.SelectEdgesForSitePoint(coord, edges));
		}
		/*
		public List<LineSegment> DelaunayLinesForSite(Vector2f coord) {
			return DelaunayLinesForEdges(Edge.SelectEdgesForSitePoint(coord, edges));
		}*/

		public List<LineSegment> VoronoiDiagram() {
			return LineSegment.VisibleLineSegments(edges);
		}
		/*
		public List<LineSegment> Hull() {
			return DelaunayLinesForEdges(HullEdges());
		}*/

		public List<Edge> HullEdges() {
			return edges.FindAll(edge=>edge.IsPartOfConvexHull());
		}

		public List<Vector2f> HullPointsInOrder() {
			List<Edge> hullEdges = HullEdges();

			List<Vector2f> points = new List<Vector2f>();
			if (hullEdges.Count == 0) {
				return points;
			}

			EdgeReorderer reorderer = new EdgeReorderer(hullEdges, typeof(Site));
			hullEdges = reorderer.Edges;
			List<LR> orientations = reorderer.EdgeOrientations;
			reorderer.Dispose();

			LR orientation;
			for (int i = 0; i < hullEdges.Count; i++) {
				Edge edge = hullEdges[i];
				orientation = orientations[i];
				points.Add(edge.Site(orientation).Coord);
			}
			return points;
		}

		public List<List<Vector2f>> Regions() {
			return sites.Regions(plotBounds);
		}

		public List<Vector2f> SiteCoords() {
			return sites.SiteCoords();
		}

		private void FortunesAlgorithm() {
			Site newSite, bottomSite, topSite, tempSite;
			Vertex v, vertex;
			Vector2f newIntStar = Vector2f.zero;
			LR leftRight;
			Halfedge lbnd, rbnd, llbnd, rrbnd, bisector;
			Edge edge;

			Rectf dataBounds = sites.GetSitesBounds();

			int sqrtSitesNb = (int)Math.Sqrt(sites.Count() + 4);
			HalfedgePriorityQueue heap = manager.ObtainHalfedgePriorityQueue();
			heap.Init(dataBounds.y, dataBounds.height, sqrtSitesNb);
			EdgeList edgeList = manager.ObtainEdgeList(dataBounds.x, dataBounds.width, sqrtSitesNb);
			List<Halfedge> halfEdges = manager.ObtainListHalfedge();

			sites.ResetListIndex();
			Site bottomMostSite = sites.Next();
			newSite = sites.Next();

			while (true) {
				if (!heap.Empty()) {
					newIntStar = heap.Min();
				}

				if (newSite != null &&
				    (heap.Empty() || CompareByYThenX(newSite, newIntStar) < 0)) {
					// New site is smallest
					//Debug.Log("smallest: new site " + newSite);

					// Step 8:
					lbnd = edgeList.EdgeListLeftNeighbor(newSite.Coord);	// The halfedge just to the left of newSite
					//UnityEngine.Debug.Log("lbnd: " + lbnd);
					rbnd = lbnd.edgeListRightNeighbor;		// The halfedge just to the right
					//UnityEngine.Debug.Log("rbnd: " + rbnd);
					bottomSite = RightRegion(lbnd, bottomMostSite);			// This is the same as leftRegion(rbnd)
					// This Site determines the region containing the new site
					//UnityEngine.Debug.Log("new Site is in region of existing site: " + bottomSite);

					// Step 9
					edge = manager.CreateBisectingEdge(bottomSite, newSite);
					//UnityEngine.Debug.Log("new edge: " + edge);
					edges.Add(edge);

					bisector = Halfedge.Create(manager, edge, LR.LEFT);
					halfEdges.Add(bisector);
					// Inserting two halfedges into edgelist constitutes Step 10:
					// Insert bisector to the right of lbnd:
					edgeList.Insert(lbnd, bisector);

					// First half of Step 11:
					if ((vertex = Vertex.Intersect(manager, lbnd, bisector)) != null) {
						vertices.Add(vertex);
						heap.Remove(lbnd);
						lbnd.vertex = vertex;
						lbnd.ystar = vertex.y + newSite.Dist(vertex);
						heap.Insert(lbnd);
					}

					lbnd = bisector;
					bisector = Halfedge.Create(manager, edge, LR.RIGHT);
					halfEdges.Add(bisector);
					// Second halfedge for Step 10::
					// Insert bisector to the right of lbnd:
					edgeList.Insert(lbnd, bisector);

					// Second half of Step 11:
					if ((vertex = Vertex.Intersect(manager, bisector, rbnd)) != null) {
						vertices.Add(vertex);
						bisector.vertex = vertex;
						bisector.ystar = vertex.y + newSite.Dist(vertex);
						heap.Insert(bisector);
					}

					newSite = sites.Next();
				} else if (!heap.Empty()) {
					// Intersection is smallest
					lbnd = heap.ExtractMin();
					llbnd = lbnd.edgeListLeftNeighbor;
					rbnd = lbnd.edgeListRightNeighbor;
					rrbnd = rbnd.edgeListRightNeighbor;
					bottomSite = LeftRegion(lbnd, bottomMostSite);
					topSite = RightRegion(rbnd, bottomMostSite);

					// These three sites define a Delaunay triangle (not actually using these for anything... so I removed it)
					// triangles.Add(new Triangle(bottomSite, topSite, RightRegion(lbnd, bottomMostSite)));

					v = lbnd.vertex;
					v.SetIndex();
					lbnd.edge.SetVertex(lbnd.leftRight, v);
					rbnd.edge.SetVertex(rbnd.leftRight, v);
					edgeList.Remove(lbnd);
					heap.Remove(rbnd);
					edgeList.Remove(rbnd);
					leftRight = LR.LEFT;
					if (bottomSite.y > topSite.y) {
						tempSite = bottomSite;
						bottomSite = topSite;
						topSite = tempSite;
						leftRight = LR.RIGHT;
					}
					edge = manager.CreateBisectingEdge(bottomSite, topSite);
					edges.Add(edge);
					bisector = Halfedge.Create(manager, edge, leftRight);
					halfEdges.Add(bisector);
					edgeList.Insert(llbnd, bisector);
					edge.SetVertex(LR.Other(leftRight), v);
					if ((vertex = Vertex.Intersect(manager, llbnd, bisector)) != null) {
						vertices.Add(vertex);
						heap.Remove(llbnd);
						llbnd.vertex = vertex;
						llbnd.ystar = vertex.y + bottomSite.Dist(vertex);
						heap.Insert(llbnd);
					}
					if ((vertex = Vertex.Intersect(manager, bisector, rrbnd)) != null) {
						vertices.Add(vertex);
						bisector.vertex = vertex;
						bisector.ystar = vertex.y + bottomSite.Dist(vertex);
						heap.Insert(bisector);
					}
				} else {
					break;
				}
			}

			edgeList.Dispose();

			// Heap should be empty now
			heap.Dispose();

			foreach (Halfedge halfedge in halfEdges)
			{
				halfedge.ReallyDispose(manager);
			}
			manager.Release(halfEdges);

			foreach (Edge e in edges) {
				e.ClipVertices(plotBounds);
			}
		}

		public void LloydRelaxation(int nbIterations) {
			var regionBuilder = manager.ObtainRegionBuilder();
			List<Vector2f> newPoints = manager.ObtainListVector2f(sites.Count());

			// Reapeat the whole process for the number of iterations asked
			for (int i = 0; i < nbIterations; i++) {
#if UNITY_EDITOR
				UnityEngine.Profiling.Profiler.BeginSample("Lloyd" + i);
#endif
				// Go thourgh all sites
				sites.ResetListIndex();
				Site site = sites.Next();
				while (site != null) {

					var region = regionBuilder.Build(site.Edges, plotBounds);
					//var region = site.Region(plotBounds);
					// Loop all corners of the site to calculate the centroid
					if (region.Count < 1)
					{
						site = sites.Next();
						continue;
					}

					Vector2f centroid = GetCentroid(region);
					//region.Reverse();
					//Vector2f centroidReverse = GetCentroid(region);
					//UnityEngine.Debug.Log((centroid - centroidReverse).magnitude);

					// Move site to the centroid of its Voronoi cell
					newPoints.Add(centroid);
					site = sites.Next();
				}

				// Between each replacement of the cendroid of the cell,
				// we need to recompute Voronoi diagram:
				Rectf origPlotBounds = this.plotBounds;
				Dispose();

				manager.CheckDebug();

				Init(newPoints,origPlotBounds);
				newPoints.Clear();
#if UNITY_EDITOR
				UnityEngine.Profiling.Profiler.EndSample();
#endif
			}
			manager.Release(regionBuilder);
			manager.Release(newPoints);
		}

		private Vector2f GetCentroid(List<Vector2f> region)
		{
			Vector2f centroid = Vector2f.zero;
			float signedArea = 0;
			float x0 = 0;
			float y0 = 0;
			float x1 = 0;
			float y1 = 0;
			float a = 0;
			// For all vertices except last
			for (int j = 0; j < region.Count - 1; j++)
			{
				x0 = region[j].x;
				y0 = region[j].y;
				x1 = region[j + 1].x;
				y1 = region[j + 1].y;
				a = x0 * y1 - x1 * y0;
				signedArea += a;
				centroid.x += (x0 + x1) * a;
				centroid.y += (y0 + y1) * a;
			}
			// Do last vertex
			x0 = region[region.Count - 1].x;
			y0 = region[region.Count - 1].y;
			x1 = region[0].x;
			y1 = region[0].y;
			a = x0 * y1 - x1 * y0;
			signedArea += a;
			centroid.x += (x0 + x1) * a;
			centroid.y += (y0 + y1) * a;

			signedArea *= 0.5f;
			centroid.x /= (6 * signedArea);
			centroid.y /= (6 * signedArea);

			return centroid;
		}

		private Site LeftRegion(Halfedge he, Site bottomMostSite) {
			Edge edge = he.edge;
			if (edge == null) {
				return bottomMostSite;
			}
			return edge.Site(he.leftRight);
		}
		
		private Site RightRegion(Halfedge he, Site bottomMostSite) {
			Edge edge = he.edge;
			if (edge == null) {
				return bottomMostSite;
			}
			return edge.Site(LR.Other(he.leftRight));
		}

		public static int CompareByYThenX(Site s1, Site s2) {
			if (s1.y < s2.y) return -1;
			if (s1.y > s2.y) return 1;
			if (s1.x < s2.x) return -1;
			if (s1.x > s2.x) return 1;
			return 0;
		}
		
		public static int CompareByYThenX(Site s1, Vector2f s2) {
			if (s1.y < s2.y) return -1;
			if (s1.y > s2.y) return 1;
			if (s1.x < s2.x) return -1;
			if (s1.x > s2.x) return 1;
			return 0;
		}
	}
}
