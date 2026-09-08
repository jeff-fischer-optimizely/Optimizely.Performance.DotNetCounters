#if !NET472
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Optimizely.Performance.DotNetCounters.Cache
{
    /// <summary>
    /// Wraps the application's <see cref="IMemoryCache"/> so that dependency cascades can be
    /// counted and eviction reasons observed. Every other operation is passed straight
    /// through untouched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two things are observed here, both of which are invisible from the
    /// <c>IObjectInstanceCache</c> layer above:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <see cref="Remove"/> is where every entry in a dependency cascade ends up, on the
    /// thread that started the cascade, so counting calls gives the exact fan-out.
    /// </description></item>
    /// <item><description>
    /// Eviction reasons only reach a per-entry callback. Rather than adding a second callback
    /// to every entry, we substitute the one Optimizely already registers, so the reason codes
    /// cost no retained memory. See <see cref="InstrumentedCacheEntry"/>.
    /// </description></item>
    /// </list>
    /// <para>
    /// The application's memory cache is shared, so entries belonging to other components pass
    /// through this type as well. They are left strictly alone: the callback substitution
    /// applies only to entries whose callback belongs to Optimizely's cache, and removals
    /// outside a tracked cascade cost a thread-static read.
    /// </para>
    /// </remarks>
    internal sealed class InstrumentedMemoryCache : IMemoryCache
    {
        private readonly IMemoryCache _inner;
        private readonly CacheInstrumentationRecorder _recorder;

        // One delegate for the lifetime of the process, reused by every instrumented entry,
        // so substituting a callback allocates only the registration that replaces it.
        private readonly PostEvictionDelegate _evictionCallback;

        internal InstrumentedMemoryCache(IMemoryCache inner, CacheInstrumentationRecorder recorder)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
            _evictionCallback = OnEvicted;
        }

        internal PostEvictionDelegate EvictionCallback => _evictionCallback;

        public ICacheEntry CreateEntry(object key)
        {
            var entry = _inner.CreateEntry(key);
            return _recorder.IsDisabled ? entry : new InstrumentedCacheEntry(entry, this);
        }

        public void Remove(object key)
        {
            CacheRemovalScope.CountRemoval();
            _inner.Remove(key);
        }

        public bool TryGetValue(object key, out object? value) => _inner.TryGetValue(key, out value);

        public void Dispose() => _inner.Dispose();

        /// <summary>
        /// Runs in place of Optimizely's own post-eviction callback, records the reason, then
        /// hands over to theirs.
        /// </summary>
        /// <remarks>
        /// Optimizely's callback is what prunes the dependency dictionary and cascades the
        /// eviction, so it is correctness-critical: if it stopped running, dependent entries
        /// would survive invalidations meant to kill them and the dependency dictionary would
        /// grow without bound. It is therefore invoked from a <c>finally</c>, and our own work
        /// happens first and cannot escape.
        /// </remarks>
        private void OnEvicted(object key, object? value, EvictionReason reason, object? state)
        {
            var original = state as PostEvictionCallbackRegistration;

            try
            {
                // Removals are already counted exactly, and synchronously, by
                // CacheRemovalScope. Recording them again here would double-count the
                // metric and would put a metric call on every entry of every cascade.
                if (reason != EvictionReason.Removed)
                {
                    _recorder.RecordEviction(ReasonName(reason));
                }
            }
            catch
            {
                // The recorder already swallows and counts its own failures. This is the
                // backstop that guarantees nothing reaches the line below.
            }
            finally
            {
                original?.EvictionCallback?.Invoke(key, value, reason, original.State);
            }
        }

        /// <summary>
        /// Maps an eviction reason to a stable, allocation-free dimension value.
        /// </summary>
        /// <remarks>
        /// <see cref="Enum.ToString()"/> allocates on every call, and this runs once per
        /// evicted entry.
        /// </remarks>
        private static string ReasonName(EvictionReason reason) => reason switch
        {
            EvictionReason.Removed => "Removed",
            EvictionReason.Replaced => "Replaced",
            EvictionReason.Expired => "Expired",
            EvictionReason.TokenExpired => "TokenExpired",
            EvictionReason.Capacity => "Capacity",
            _ => "None",
        };
    }

    /// <summary>
    /// A cache entry that swaps Optimizely's post-eviction callback for ours on the way in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The framework builds an entry by calling <c>IMemoryCache.CreateEntry</c>, copying the
    /// caller's options onto it - which appends each <see cref="PostEvictionCallbackRegistration"/>
    /// to <see cref="ICacheEntry.PostEvictionCallbacks"/> one at a time - and committing it on
    /// <see cref="Dispose"/>. Rewriting the list just before that commit is therefore enough
    /// to intercept eviction, and it replaces the existing registration rather than adding
    /// one, so the entry carries no extra retained state.
    /// </para>
    /// <para>
    /// Only entries carrying exactly the callback shape Optimizely's cache produces are
    /// touched. Anything else - a different component using the shared memory cache, or a
    /// future Optimizely release that registers callbacks differently - is committed exactly
    /// as it was built, and simply goes unmeasured.
    /// </para>
    /// </remarks>
    internal sealed class InstrumentedCacheEntry : ICacheEntry
    {
        private const string OptimizelyCacheTypeName = "EPiServer.Framework.Cache.Internal.MemoryObjectInstanceCache";

        // Cached after the first entry so the common path is a reference comparison rather
        // than a string comparison. Not synchronised: races only cost a repeated string
        // compare, and every thread would store the same value.
        private static Type? _knownOwnerType;

        private readonly ICacheEntry _inner;
        private readonly InstrumentedMemoryCache _owner;

        internal InstrumentedCacheEntry(ICacheEntry inner, InstrumentedMemoryCache owner)
        {
            _inner = inner;
            _owner = owner;
        }

        public object Key => _inner.Key;

        public object? Value
        {
            get => _inner.Value;
            set => _inner.Value = value;
        }

        public DateTimeOffset? AbsoluteExpiration
        {
            get => _inner.AbsoluteExpiration;
            set => _inner.AbsoluteExpiration = value;
        }

        public TimeSpan? AbsoluteExpirationRelativeToNow
        {
            get => _inner.AbsoluteExpirationRelativeToNow;
            set => _inner.AbsoluteExpirationRelativeToNow = value;
        }

        public TimeSpan? SlidingExpiration
        {
            get => _inner.SlidingExpiration;
            set => _inner.SlidingExpiration = value;
        }

        public IList<IChangeToken> ExpirationTokens => _inner.ExpirationTokens;

        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => _inner.PostEvictionCallbacks;

        public CacheItemPriority Priority
        {
            get => _inner.Priority;
            set => _inner.Priority = value;
        }

        public long? Size
        {
            get => _inner.Size;
            set => _inner.Size = value;
        }

        public void Dispose()
        {
            TrySubstituteCallback();
            _inner.Dispose();
        }

        private void TrySubstituteCallback()
        {
            try
            {
                var callbacks = _inner.PostEvictionCallbacks;

                // Optimizely registers exactly one callback per entry. Anything else is a
                // shape we do not recognise, and we leave it alone rather than guess.
                if (callbacks == null || callbacks.Count != 1)
                {
                    return;
                }

                var registration = callbacks[0];
                var target = registration?.EvictionCallback?.Target;
                if (target == null || !IsOptimizelyCache(target.GetType()))
                {
                    return;
                }

                callbacks[0] = new PostEvictionCallbackRegistration
                {
                    EvictionCallback = _owner.EvictionCallback,
                    State = registration,
                };
            }
            catch
            {
                // Instrumentation must never stop an entry being cached. Leaving the
                // callbacks untouched means this entry is simply not measured.
            }
        }

        private static bool IsOptimizelyCache(Type type)
        {
            if (ReferenceEquals(type, _knownOwnerType))
            {
                return true;
            }

            if (!string.Equals(type.FullName, OptimizelyCacheTypeName, StringComparison.Ordinal))
            {
                return false;
            }

            _knownOwnerType = type;
            return true;
        }
    }
}
#endif
