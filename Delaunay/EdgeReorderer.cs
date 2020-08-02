using System;
using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {

	public class EdgeReorderer {

		private List<Edge> edges;
		private List<LR> edgeOrientations;
		private List<bool> done = new List<bool>();


		public List<Edge> Edges {get{return edges;}}
		public List<LR> EdgeOrientations {get{return edgeOrientations;}}

		public EdgeReorderer(List<Edge> origEdges, Type criterion) {
			edges = new List<Edge>();
			edgeOrientations = new List<LR>();
			if (origEdges != null && origEdges.Count > 0) {
				ReorderEdges(origEdges, criterion);
			}
		}

		public void Dispose() {
			edges = null;
			edgeOrientations = null;
		}

		public void ReorderEdges(List<Edge> origEdges, Type criterion) {
			int i;
			int n;
			Edge edge;
			// We're going to reorder the edges in order of traversal
			int nDone = 0;
			n = done.Count;
			for (int b = 0; b < n; b++) done[b] = false;
			n = origEdges.Count;
			for (int b = done.Count; b < n; b++) done.Add(false);

			edges.Clear();
			edgeOrientations.Clear();

			i = 0;
			edge = origEdges[i];
			edges.Add(edge);
			edgeOrientations.Add(LR.LEFT);
			ICoord firstPoint; 
			ICoord lastPoint;
			if (criterion == typeof(Vertex)) {
				firstPoint = edge.LeftVertex;
				lastPoint = edge.RightVertex;
			} else {
				firstPoint = edge.LeftSite;
				lastPoint = edge.RightSite;
			}

			if (firstPoint == Vertex.VERTEX_AT_INFINITY || lastPoint == Vertex.VERTEX_AT_INFINITY) {
				edges.Clear();
				return;
			}

			done[i] = true;
			nDone++;

			while (nDone < n) {
				for (i = 1; i < n; i++) {
					if (done[i]) {
						continue;
					}
					edge = origEdges[i];
					ICoord leftPoint; 
					ICoord rightPoint;
					if (criterion == typeof(Vertex)) {
						leftPoint = edge.LeftVertex;
						rightPoint = edge.RightVertex;
					} else {
						leftPoint = edge.LeftSite;
						rightPoint = edge.RightSite;
					}
					if (leftPoint == Vertex.VERTEX_AT_INFINITY || rightPoint == Vertex.VERTEX_AT_INFINITY) {
						edges.Clear();
						return;
					}
					if (leftPoint == lastPoint) {
						lastPoint = rightPoint;
						edgeOrientations.Add(LR.LEFT);
						edges.Add(edge);
						done[i] = true;
					} else if (rightPoint == firstPoint) {
						firstPoint = leftPoint;
						edgeOrientations.Insert(0, LR.LEFT);
						edges.Insert(0, edge);
						done[i] = true;
					} else if (leftPoint == firstPoint) {
						firstPoint = rightPoint;
						edgeOrientations.Insert(0, LR.RIGHT);
						edges.Insert(0, edge);
						done[i] = true;
					} else if (rightPoint == lastPoint) {
						lastPoint = leftPoint;
						edgeOrientations.Add(LR.RIGHT);
						edges.Add(edge);
						done[i] = true;
					}
					if (done[i]) {
						nDone++;
					}
				}
			}
			return;
		}
	}
}
