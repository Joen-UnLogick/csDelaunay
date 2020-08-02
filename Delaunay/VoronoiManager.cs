using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_5_OR_NEWER
using UnityEngine;
#endif

namespace csDelaunay
{
	public class VoronoiManager
	{
		Pool<Site> sites;
		Pool<Edge> edges;
		Pool<RegionBuilder> regionBuilders;
		Pool<Halfedge> halfedges;
		Pool<HalfedgePriorityQueue> halfedgePriorityQueues = new Pool<HalfedgePriorityQueue>();
		Pool<Vertex> vertices;
		Dictionary<int, ArrayPool<Halfedge>> halfEdgeArrayLookup = new Dictionary<int, ArrayPool<Halfedge>>();
		Pool<EdgeList> edgeLists;
		Pool<List<Vertex>> listVertices;
		Pool<List<Edge>> listEdges;
		Pool<List<Site>> listSites;
		Pool<List<Vector2f>> listVector2f;
		Pool<List<Halfedge>> listHalfEdges;
		Pool<LRCollection<Vector2f>> lrCollectionVector2f;

		public VoronoiManager()
		{
			sites = new Pool<Site>();
			edges = new Pool<Edge>();
			halfedges = new Pool<Halfedge>();
			vertices = new Pool<Vertex>();
#if VORONOI_POOL_LEAK_HUNTING
			edges.DebugPool = true;
			edges.stackLevel = 2;
#endif
		}

		public VoronoiManager(int cells)
		{
			sites = new Pool<Site>(cells);
			edges = new Pool<Edge>(cells * 3);
			halfedges = new Pool<Halfedge>(cells * 3);
			vertices = new Pool<Vertex>(cells * 3);
		}

		public Halfedge ObtainHalfedge()
		{
			return halfedges.Get();
		}

		public Site ObtainSite(Vector2f p, int index, float weigth)
		{
			var site = sites.Get();
			site.manager = this;
			site.Init(p, index, weigth);
			return site;
		}

		public Halfedge[] ObtainHalfedgeArray(int hashSize)
		{
			ArrayPool<Halfedge> pool;
			if (!halfEdgeArrayLookup.TryGetValue(hashSize, out pool))
			{
				pool = new ArrayPool<Halfedge>(hashSize);
				halfEdgeArrayLookup.Add(hashSize, pool);
			}
			return pool.Get();
		}

		/*
		 * This is the only way to create a new Edge
		 * @param site0
		 * @param site1
		 * @return
		 */
		public Edge CreateBisectingEdge(Site s0, Site s1)
		{
			float dx, dy;
			float absdx, absdy;
			float a, b, c;

			dx = s1.x - s0.x;
			dy = s1.y - s0.y;
			absdx = dx > 0 ? dx : -dx;
			absdy = dy > 0 ? dy : -dy;
			c = s0.x * dx + s0.y * dy + (dx * dx + dy * dy) * 0.5f;

			if (absdx > absdy)
			{
				a = 1;
				b = dy / dx;
				c /= dx;
			}
			else
			{
				b = 1;
				a = dx / dy;
				c /= dy;
			}

			Edge edge = edges.Get();
			edge.EdgeIndex = edges.Retrieved;
			edge.manager = this;

			edge.LeftSite = s0;
			edge.RightSite = s1;
			s0.AddEdge(edge);
			s1.AddEdge(edge);

			edge.a = a;
			edge.b = b;
			edge.c = c;

			return edge;
		}

		public Vertex ObtainVertex()
		{
			var vertex = vertices.Get();
			vertex.VertexIndex = vertices.Retrieved;
			return vertex;
		}

		public RegionBuilder ObtainRegionBuilder()
		{
			if (regionBuilders == null)
			{
				regionBuilders = new Pool<RegionBuilder>();
			}
			return regionBuilders.Get();
		}


		public LRCollection<Vector2f> ObtainLRCollectionVector2f()
		{
			if (lrCollectionVector2f == null)
			{
				lrCollectionVector2f = new Pool<LRCollection<Vector2f>>();
			}
			return lrCollectionVector2f.Get();
		}

		public HalfedgePriorityQueue ObtainHalfedgePriorityQueue()
		{
			var queue = halfedgePriorityQueues.Get();
			queue.manager = this;
			return queue;
		}

		public EdgeList ObtainEdgeList(float xmin, float deltaX, int sqrtSitesNb)
		{
			if (edgeLists == null)
			{
				edgeLists = new Pool<EdgeList>();
			}
			var edgeList = edgeLists.Get();
			edgeList.manager = this;
			edgeList.Init(xmin, deltaX, sqrtSitesNb);
			return edgeList;
		}

		public List<Vector2f> ObtainListVector2f(int count)
		{
			if (listVector2f == null)
			{
				listVector2f = new Pool<List<Vector2f>>();
			}
			var list = listVector2f.Get();
			if (list.Capacity < count)
			{
				list.Capacity = count;
			}
			return list;
		}

		public List<Site> ObtainListSite(int capacity)
		{
			if (listSites == null)
			{
				listSites = new Pool<List<Site>>();
			}
			var list = listSites.Get();
			if (list.Capacity < capacity)
			{
				list.Capacity = capacity;
			}
			return list;
		}

		public List<Halfedge> ObtainListHalfedge()
		{
			if (listHalfEdges == null)
			{
				listHalfEdges = new Pool<List<Halfedge>>();
			}
			return listHalfEdges.Get();
		}

		public List<Edge> ObtainListEdge(int capacity = 0)
		{
			if (listEdges == null)
			{
				listEdges = new Pool<List<Edge>>();
			}
			var result = listEdges.Get();
			if (result.Capacity < capacity)
			{
				result.Capacity = capacity;
			}
			return result;
		}

		public List<Vertex> ObtainListVertex(int capacity = 0)
		{
			if (listVertices == null)
			{
				listVertices = new Pool<List<Vertex>>();
			}
			var result = listVertices.Get();
			if (result.Capacity < capacity)
			{
				result.Capacity = capacity;
			}
			return result;
		}



		public void Release(Halfedge halfedge)
		{
			halfedges.Release(halfedge);
		}

		public void Release(RegionBuilder regionBuilder)
		{
			regionBuilder.Release();
			regionBuilders.Release(regionBuilder);
		}


		public void Release(LRCollection<Vector2f> clippedVerticesList)
		{
			clippedVerticesList.Clear();
			lrCollectionVector2f.Release(clippedVerticesList);
		}

		public void Release(Vertex vertex)
		{
			vertices.Release(vertex);
		}

		public void Release(EdgeList edgeList)
		{
			edgeLists.Release(edgeList);
		}

		internal void Release(HalfedgePriorityQueue halfedgePriorityQueue)
		{
			halfedgePriorityQueues.Release(halfedgePriorityQueue);
		}

		public void Release(Site site)
		{
			site.Clear();
			sites.Release(site);
		}

		public void Release(Halfedge[] hash)
		{
			for (int i = 0; i < hash.Length; i++)
			{
				hash[i] = null;
			}
			halfEdgeArrayLookup[hash.Length].Release(hash);
		}

		public void Release(Edge edge)
		{
			edges.Release(edge);
		}


		public void Release(List<Vector2f> list)
		{
			list.Clear();
			listVector2f.Release(list);
		}

		public void Release(List<Edge> list)
		{
			list.Clear();
			listEdges.Release(list);
		}

		public void Release(List<Halfedge> list)
		{
			list.Clear();
			listHalfEdges.Release(list);
		}

		public void Release(List<Site> list)
		{
			list.Clear();
			listSites.Release(list);
		}

		public void Release(List<Vertex> list)
		{
			list.Clear();
			listVertices.Release(list);
		}

		public void CheckDebug()
		{
#if VORONOI_POOL_LEAK_HUNTING
			if (edges.DebugPool)
			{
				foreach (var debugEntry in edges.GetActiveDebug())
				{
#if UNITY_EDITOR
					UnityEngine.Debug.LogFormat("{0}:{1} - {2}", debugEntry.Value.GetFileName(), debugEntry.Value.GetFileLineNumber(), debugEntry.Key);
#endif
				}
			}
#endif
		}
	}
}