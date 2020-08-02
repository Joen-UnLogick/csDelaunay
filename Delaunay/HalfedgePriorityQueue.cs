using System;
using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {

	// Also know as heap
	public class HalfedgePriorityQueue {

		public VoronoiManager manager;
		private Halfedge[] hash;
		private int count;
		private int minBucked;
		private int hashSize;

		private float ymin;
		private float deltaY;

		public HalfedgePriorityQueue(VoronoiManager manager, float ymin, float deltaY, int sqrtSitesNb) {
			this.manager = manager;
			Init(ymin, deltaY, sqrtSitesNb);
		}

		public HalfedgePriorityQueue()
		{
		}

		public void Init(float ymin, float deltaY, int sqrtSitesNb)
		{
			this.ymin = ymin;
			this.deltaY = deltaY;
			hashSize = 4 * sqrtSitesNb;
			Init();
		}

		public void Dispose() {
			// Get rid of dummies
			for (int i = 0; i < hashSize; i++) {
				hash[i].Dispose(manager);
				hash[i] = null;
			}
			manager.Release(hash);
			hash = null;
			manager.Release(this);
		}

		public void Init() {
			count = 0;
			minBucked = 0;
			hash = manager.ObtainHalfedgeArray(hashSize);
			// Dummy Halfedge at the top of each hash
			for (int i = 0; i < hashSize; i++) {
				hash[i] = Halfedge.CreateDummy(manager);
				hash[i].nextInPriorityQueue = null;
			}
		}

		public void Insert(Halfedge halfedge) {
			Halfedge previous, next;

			int insertionBucket = Bucket(halfedge);
			if (insertionBucket < minBucked) {
				minBucked = insertionBucket;
			}
			previous = hash[insertionBucket];
			while ((next = previous.nextInPriorityQueue) != null &&
			       (halfedge.ystar > next.ystar || (halfedge.ystar == next.ystar && halfedge.vertex.x > next.vertex.x))) {
				previous = next;
			}
			halfedge.nextInPriorityQueue = previous.nextInPriorityQueue;
			previous.nextInPriorityQueue = halfedge;
			count++;
		}

		public void Remove(Halfedge halfedge) {
			Halfedge previous;
			int removalBucket = Bucket(halfedge);

			if (halfedge.vertex != null) {
				previous = hash[removalBucket];
				while (previous.nextInPriorityQueue != halfedge) {
					previous = previous.nextInPriorityQueue;
				}
				previous.nextInPriorityQueue = halfedge.nextInPriorityQueue;
				count--;
				halfedge.vertex = null;
				halfedge.nextInPriorityQueue = null;
				halfedge.Dispose(manager);
			}
		}

		private int Bucket(Halfedge halfedge) {
			int theBucket = (int)((halfedge.ystar - ymin)/deltaY * hashSize);
			if (theBucket < 0) theBucket = 0;
			if (theBucket >= hashSize) theBucket = hashSize - 1;
			return theBucket;
		}

		private bool IsEmpty(int bucket) {
			return (hash[bucket].nextInPriorityQueue == null);
		}

		/*
		 * move minBucket until it contains an actual Halfedge (not just the dummy at the top);
		 */
		private void AdjustMinBucket() {
			while (minBucked < hashSize - 1 && IsEmpty(minBucked)) {
				minBucked++;
			}
		}

		public bool Empty() {
			return count == 0;
		}

		/*
		 * @return coordinates of the Halfedge's vertex in V*, the transformed Voronoi diagram
		 */
		public Vector2f Min() {
			AdjustMinBucket();
			Halfedge answer = hash[minBucked].nextInPriorityQueue;
			return new Vector2f(answer.vertex.x, answer.ystar);
		}

		/*
		 * Remove and return the min Halfedge
		 */
		public Halfedge ExtractMin() {
			Halfedge answer;

			// Get the first real Halfedge in minBucket
			answer = hash[minBucked].nextInPriorityQueue;

			hash[minBucked].nextInPriorityQueue = answer.nextInPriorityQueue;
			count--;
			answer.nextInPriorityQueue = null;

			return answer;
		}
	}
}