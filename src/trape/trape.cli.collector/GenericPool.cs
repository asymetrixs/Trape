using System;
using System.Collections.Concurrent;

namespace trape.cli.collector
{
    /// <summary>
    /// Pool to avoid heavy initialization of objects by reuse
    /// </summary>
    /// <typeparam name="T">Class of cached instances</typeparam>
    public class ObjectPool<T>
    {
        #region Fields

        /// <summary>
        /// Function to call for creating the object
        /// </summary>
        private readonly Func<T> ctor;

        /// <summary>
        /// Holds the instances
        /// </summary>
        private readonly ConcurrentBag<T> objects;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class
        /// </summary>
        /// <param name="ctor">Constructor or factory to create new instances</param>
        internal ObjectPool(Func<T> ctor)
        {
            #region Argument checks

            _ = ctor ?? throw new ArgumentNullException(paramName: nameof(ctor));

            #endregion

            this.ctor = ctor;
            this.objects = new ConcurrentBag<T>();
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        /// Removes all objects of this pool
        /// </summary>
        internal void ClearUnused()
        {
            while (this.objects.TryTake(out T obj))
            {
                if (obj is IDisposable)
                {
                    (obj as IDisposable).Dispose();
                }
            }
        }

        /// <summary>
        /// Returns an object from the pool or creates a new one if pool is empty
        /// </summary>
        /// <returns>Cached or new instance</returns>
        public T Get()
        {
            if (!this.objects.TryTake(out T item))
            {
                item = this.ctor();
            }

            return item;
        }

        /// <summary>
        /// Returns an object to the pool for reuse. Use <c>Clear</c> if class implements method prior to <c>Put</c>!
        /// </summary>
        /// <param name="item">Instance to put back into cache</param>
        public void Put(T item)
        {
            this.objects.Add(item);
        }

        #endregion Methods
    }
}