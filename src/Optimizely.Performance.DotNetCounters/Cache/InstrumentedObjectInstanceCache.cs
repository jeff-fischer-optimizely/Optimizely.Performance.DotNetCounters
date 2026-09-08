#if !NET472
using System;
using System.Diagnostics;
using EPiServer.Framework.Cache;

namespace Optimizely.Performance.DotNetCounters.Cache
{
    /// <summary>
    /// Shared measurement logic for the cache decorators.
    /// </summary>
    internal static class CacheOperationMeasurement
    {
        internal const string Remove = "Remove";
        internal const string RemoveLocal = "RemoveLocal";
        internal const string RemoveRemote = "RemoveRemote";
        internal const string Insert = "Insert";
        internal const string Clear = "Clear";

        internal static double ElapsedMilliseconds(long startTimestamp) =>
            (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;

        /// <summary>
        /// Extracts the requested lifetime from an eviction policy, if it set one.
        /// </summary>
        internal static TimeSpan TimeToLive(CacheEvictionPolicy? policy) =>
            policy == null ? TimeSpan.Zero : policy.Expiration;
    }

    /// <summary>
    /// Measures the dependency cascade behind every removal made through
    /// <see cref="IObjectInstanceCache"/>.
    /// </summary>
    /// <remarks>
    /// This decorator opens the counting scope and names the operation; the entries themselves
    /// are counted one layer further down, in <see cref="InstrumentedMemoryCache"/>. Removals
    /// that reach the cache through some other path still cost nothing, because counting only
    /// happens while a scope is open.
    /// </remarks>
    internal class InstrumentedObjectInstanceCache : IObjectInstanceCache
    {
        private readonly IObjectInstanceCache _inner;
        private readonly CacheInstrumentationRecorder _recorder;

        internal InstrumentedObjectInstanceCache(IObjectInstanceCache inner, CacheInstrumentationRecorder recorder)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        }

        /// <summary>
        /// Gets the cache being decorated.
        /// </summary>
        protected IObjectInstanceCache Inner => _inner;

        /// <summary>
        /// Gets the recorder metrics are reported to.
        /// </summary>
        protected CacheInstrumentationRecorder Recorder => _recorder;

        public object Get(string key) => _inner.Get(key);

        /// <summary>
        /// Inserts an entry, recording its requested lifetime and any cascade the insert causes.
        /// </summary>
        /// <remarks>
        /// Inserting is not only a write. Optimizely removes the existing entry first, and that
        /// removal cascades, so replacing a key that other entries depend on discards the whole
        /// subtree beneath it. That is a common and thoroughly unobvious source of cache churn,
        /// which is why inserts are measured alongside removals.
        /// </remarks>
        public void Insert(string key, object value, CacheEvictionPolicy evictionPolicy)
        {
            _recorder.RecordInsertTimeToLive(CacheOperationMeasurement.TimeToLive(evictionPolicy));

            // Every insert removes the previous entry first, so an insert with nothing
            // depending on it still counts as one removal. Reporting those would put a metric
            // call on the hottest path in the cache to say nothing, so inserts are reported
            // only once they have actually taken something else with them.
            Measure(CacheOperationMeasurement.Insert, key, () => _inner.Insert(key, value, evictionPolicy),
                minimumToReport: 2);
        }

        public void Remove(string key) =>
            Measure(CacheOperationMeasurement.Remove, key, () => _inner.Remove(key));

#if NET6_0
        /// <remarks>
        /// Present on Optimizely 12 only; <c>IObjectInstanceCache</c> dropped it in 13. Already
        /// obsolete there, but part of the interface, so it has to be forwarded.
        /// </remarks>
#pragma warning disable CS0618
        public void Clear() => _inner.Clear();
#pragma warning restore CS0618
#endif

        /// <summary>
        /// Runs a cache operation with cascade counting active, then reports what it cost.
        /// </summary>
        /// <remarks>
        /// The scope is closed in a <c>finally</c> so that an exception from the cache cannot
        /// leave the thread's counter stuck open, and reporting happens after the operation has
        /// finished so that no instrumentation work runs while the cache's write lock is held.
        /// </remarks>
        /// <param name="operation">The operation name used as the metric dimension.</param>
        /// <param name="key">The key the caller asked for.</param>
        /// <param name="operate">The underlying cache call.</param>
        /// <param name="minimumToReport">
        /// The smallest cascade worth a metric. Removals report from one, because the count of
        /// single-entry removals is the denominator of the amplification ratio.
        /// </param>
        protected void Measure(string operation, string key, Action operate, int minimumToReport = 1)
        {
            if (_recorder.IsDisabled)
            {
                operate();
                return;
            }

            var isOutermost = CacheRemovalScope.Begin();
            var start = Stopwatch.GetTimestamp();
            int removed;

            try
            {
                operate();
            }
            finally
            {
                removed = CacheRemovalScope.End(isOutermost);
            }

            if (isOutermost && removed >= minimumToReport)
            {
                _recorder.RecordRemoval(operation, key, removed, CacheOperationMeasurement.ElapsedMilliseconds(start));
            }
        }
    }

    /// <summary>
    /// Adds local and remote invalidation to <see cref="InstrumentedObjectInstanceCache"/>.
    /// </summary>
    /// <remarks>
    /// Worth decorating separately from <see cref="IObjectInstanceCache"/> because the
    /// distinction is diagnostic in itself: a cascade reported as <c>RemoveRemote</c> was
    /// caused by another node in the cluster, not by anything this instance did. Nested scopes
    /// mean decorating both layers cannot double-count a removal that passes through both.
    /// </remarks>
    internal sealed class InstrumentedSynchronizedObjectInstanceCache
        : InstrumentedObjectInstanceCache, ISynchronizedObjectInstanceCache
    {
        private readonly ISynchronizedObjectInstanceCache _inner;

        internal InstrumentedSynchronizedObjectInstanceCache(
            ISynchronizedObjectInstanceCache inner,
            CacheInstrumentationRecorder recorder)
            : base(inner, recorder)
        {
            _inner = inner;
        }

#pragma warning disable CS0618 // Obsolete on 13, still part of the interface we must implement.
        public FailureRecoveryAction SynchronizationFailedStrategy
        {
            get => _inner.SynchronizationFailedStrategy;
            set => _inner.SynchronizationFailedStrategy = value;
        }

        public IObjectInstanceCache ObjectInstanceCache => _inner.ObjectInstanceCache;
#pragma warning restore CS0618

        public void RemoveLocal(string key) =>
            Measure(CacheOperationMeasurement.RemoveLocal, key, () => _inner.RemoveLocal(key));

        public void RemoveRemote(string key) =>
            Measure(CacheOperationMeasurement.RemoveRemote, key, () => _inner.RemoveRemote(key));
    }
}
#endif
