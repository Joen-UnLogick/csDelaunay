using System.Collections.Generic;

namespace csDelaunay
{
	/// <summary>
	/// Primitive Pool that makes Voronoi "garbage free"
	/// </summary>
	/// <typeparam name="T">Type of objects to be pooled</typeparam>
	public class Pool<T>  
		where T : new()
	{
		List<T> pool;

		int allocated;
		int retrieved;
		int released;

#if VORONOI_POOL_LEAK_HUNTING
		public IEnumerable<KeyValuePair<T, System.Diagnostics.StackFrame>> GetActiveDebug()
		{
			return active;
		}

		Dictionary<T, System.Diagnostics.StackFrame> active;
		bool debug;
		public int stackLevel;

		public bool DebugPool
		{
			get { return debug; }
			set
			{
				if (debug == value)
					return;
				debug = value;
				if (debug)
				{
					active = new Dictionary<T, System.Diagnostics.StackFrame>();
				}
				else
				{
					active = null;
				}
			}
		}
#endif

		public int Retrieved { get { return retrieved; } }
		public Pool(int initialCapacity)
		{
			pool = new List<T>(initialCapacity);
		}
		public Pool()
		{
			pool = new List<T>();
		}

		public T Get()
		{
			retrieved++;
			if (pool.Count > 0)
			{
				int index = pool.Count - 1;
				var result = pool[index];
				pool.RemoveAt(index);
#if VORONOI_POOL_LEAK_HUNTING
				if (debug)
				{
					active[result] = new System.Diagnostics.StackFrame(stackLevel, true);
				}
#endif
				return result;
			}
			allocated++;
#if VORONOI_POOL_LEAK_HUNTING
			if (debug)
			{
				var result = new T();
				active.Add(result, new System.Diagnostics.StackFrame(stackLevel, true));
				return result;
			}
#endif
			return new T();
		}

		public void Release(T obj)
		{
#if VORONOI_POOL_LEAK_HUNTING
			if (debug)
			{
				active.Remove(obj);
			}
#endif
			var activeInstances = retrieved - released;
			if (pool.Capacity * 2 < pool.Count + activeInstances)
			{
				pool.Capacity = activeInstances * 2;
			}

			released++;
			pool.Add(obj);
		}

		public void EnsureCapacity(int count)
		{
			if (pool.Capacity < count)
			{
				pool.Capacity = count;
			}
		}

		public string Status()
		{
			if (retrieved != released)
			{
				return string.Format("Pool<{4}> - Allocated: {0}, Retrievals: {1}, Released: {2}, Active: {3}", allocated, retrieved, released, retrieved - released, typeof(T).Name);
			}
			return null;
		}
	}

	/// <summary>
	/// Primitive Pool that makes Voronoi "garbage free"
	/// </summary>
	/// <typeparam name="T">Type of objects to be pooled</typeparam>
	public class ArrayPool<T>
		where T : new()
	{
		List<T[]> pool;

		int allocated;
		int retrieved;
		int released;
		public readonly int size;

		public int Retrieved { get { return retrieved; } }
		public ArrayPool(int initialCapacity, int size)
		{
			this.size = size;
			pool = new List<T[]>(initialCapacity);
		}
		public ArrayPool(int size)
		{
			this.size = size;
			pool = new List<T[]>();
		}

		public T[] Get()
		{
			retrieved++;
			if (pool.Count > 0)
			{
				int index = pool.Count - 1;
				var result = pool[index];
				pool.RemoveAt(index);
				return result;
			}
			allocated++;
			return new T[size];
		}

		public void Release(T[] obj)
		{
			released++;
			pool.Add(obj);
		}

		public void EnsureCapacity(int count)
		{
			if (pool.Capacity < count)
			{
				pool.Capacity = count;
			}
		}
	}

	/// <summary>
	/// Primitive Pool that makes Voronoi "garbage free"
	/// </summary>
	/// <typeparam name="T">Type of objects to be pooled</typeparam>
	public class TrackingPool<T>
		where T : new()
	{
		List<T> pool;
		List<T> tracking;

		int retrieved;
		int released;

		public List<T> AllNodes { get { return tracking; } }

		public TrackingPool(int initialCapacity)
		{
			pool = new List<T>(initialCapacity);
			tracking = new List<T>(initialCapacity);
		}
		public TrackingPool()
		{
			pool = new List<T>();
			tracking = new List<T>();
		}

		public T Get()
		{
			retrieved++;
			if (pool.Count > 0)
			{
				var index = pool.Count - 1;
				var result = pool[index];
				pool.RemoveAt(index);
				return result;
			}
			else
			{
				var result = new T();
				tracking.Add(result);
				return result;
			}
		}

		public void Release(T obj)
		{
			released++;
			pool.Add(obj);
		}

		public void EnsureCapacity(int count)
		{
			if (pool.Capacity < count)
			{
				pool.Capacity = count;
			}
			if (tracking.Capacity < count)
			{
				tracking.Capacity = count;
			}
		}

		public void Cleanup()
		{
			if (pool.Count != tracking.Count)
			{
				released += tracking.Count - pool.Count;
				pool.Clear();
				pool.AddRange(tracking);
			}
		}
	}
}