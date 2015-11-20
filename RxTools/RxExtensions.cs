using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using RxTools.IO;

namespace RxTools
{
	public static class RxExtensions
	{
		/// <summary>
		/// Dumps the sequence data into a IWriter
		/// </summary>
		/// <param name="source">The source observable.</param>
		/// <param name="tag">Tag forwarded to the writer</param>
		/// <param name="writer">The destination IWriter</param>
		/// <returns>The source unmodified</returns>
		public static IObservable<T> Dump<T>(this IObservable<T> source, IWriter writer, string tag)
		{
			writer.EnsureNotNull("writer");
			tag.EnsureNotNull("tag");

			return source
				.Materialize()
				.Do(notification => writer.WriteLine(tag, notification.ToString()))
				.Dematerialize();
		}

		/// <summary>
		/// Dumps the sequence data into a IWriter. Calls with empty string to Dump<T>(writer, tag)
		/// </summary>
		/// <param name="source">The source observable.</param>
		/// <param name="writer">The destination IWriter</param>
		/// <returns></returns>
		public static IObservable<T> Dump<T>(this IObservable<T> source, IWriter writer)
		{
			return source.Dump(writer, "");
		}

		/// <summary>
		/// An exponential behaviour which starts with 1 second and then 4, 9, 16...
		/// </summary>
		public static readonly Func<int, TimeSpan> ExponentialBehaviour = n => TimeSpan.FromSeconds(Math.Pow(n, 2));

		/// <summary>
		/// An continuous back off behaviour of 1 second.
		/// </summary>
		public static readonly Func<int, TimeSpan> ContinuousBehaviour = n => TimeSpan.FromSeconds(1);

		/// <summary>
		/// Retries the subcription when OnError based on some behaviour and exception selection 
		/// </summary>
		/// <param name="source">The source observable.</param>
		/// <param name="retryCount">The number of attempts of running the source observable before failing.</param>
		/// <param name="behaviour">The behaviour to use, exponential by default.</param>
		/// <param name="retryOnError">A predicate determining if retry with certain exception</param>
		/// <param name="scheduler">An scheduler where execute this action</param>
		/// <returns>The forwarded sequence (no disconnects)</returns>
		public static IObservable<T> RetryWithBehaviour<T>(
			this IObservable<T> source,
			int retryCount,
			Func<int, TimeSpan> behaviour,
			Func<Exception, bool> retryOnError,
			IScheduler scheduler)
		{
			source.EnsureNotNull("source");
			behaviour.EnsureNotNull("behaviour");
			scheduler.EnsureNotNull("scheduler");
			retryOnError.EnsureNotNull("retryOnError");

			return Observable.Create<T>(
				o =>
				{
					Func<IScheduler, int, IDisposable> action = null;
					Func<IObservable<T>, int, int, IObservable<T>> applyCatch = null;
                    applyCatch = (s, i, max) =>
					{
						return i < max ? applyCatch(s.Catch<T, Exception>(ex => retryOnError(ex) ? source.Repeat(1) : Observable.Throw<T>(ex)), ++i, max) : s;
					};

					action = (sch, i) => i > retryCount ? source.Subscribe(o) : applyCatch(source, i, retryCount).Subscribe(o);

					return scheduler.Schedule(
						1, action);
				});
		}

		/// <summary>
		/// Resubscribe the source when onComplete arrives (the as Repeat()) with a behaviour on the default Scheduler
		/// </summary>
		/// <param name="source">The main source to resubscribe</param>
		/// <param name="behaviour">a func specifying the repeat behaviour</param>
		/// <returns>The forwarded sequence (no disconnects) </returns>
		public static IObservable<T> RepeatWithBehaviour<T>(
			this IObservable<T> source,
			Func<int, TimeSpan> behaviour)
		{
			return RepeatWithBehaviour(source, behaviour, Scheduler.Default);
		}

		/// <summary>
		/// Resubscribe the source when onComplete arrives (the as Repeat()) with a behaviour
		/// </summary>
		/// <param name="source">The main source to resubscribe</param>
		/// <param name="behaviour">a func specifying the repeat behaviour</param>
		/// <param name="scheduler">An scheduler where execute this action</param>
		/// <returns>The forwarded sequence (no disconnects) </returns>
		public static IObservable<T> RepeatWithBehaviour<T>(
			this IObservable<T> source,
			Func<int, TimeSpan> behaviour,
			IScheduler scheduler)
		{
			source.EnsureNotNull("source");
			behaviour.EnsureNotNull("behaviour");
			scheduler.EnsureNotNull("scheduler");

			return Observable.Create<T>(o =>
			{
				Func<IScheduler, int, IDisposable> action = null;
				action = (schd, state) =>
				{
					var d = source.Subscribe(o.OnNext, o.OnError);
					return new CompositeDisposable(d,
								schd.Schedule(state + 1, behaviour(state), action)
							);
				};
				return scheduler.Schedule(0, action);
			});
		}

		/// <summary>
		/// Executes a finally at any case when subscribing the source with the default scheduler
		/// </summary>
		/// <param name="source">The main source to wrap</param>
		/// <param name="finallyAction">The finally action</param>
		/// <returns>The modified source</returns>
		public static IObservable<T> FinallyAlways<T>(this IObservable<T> source, Action finallyAction)
		{
			return Observable.Create<T>(o =>
                {
                var finallyOnce = Disposable.Create(finallyAction);
                var subscription = source.Subscribe(
                    o.OnNext,
                    ex =>
                    {
                        try { o.OnError(ex); }
                        finally { finallyOnce.Dispose(); }
                    },
                    () =>
                    {
                        try { o.OnCompleted(); }
                        finally { finallyOnce.Dispose(); }
                    });
                return new CompositeDisposable(subscription, finallyOnce);
            });
		}

//		/// <summary>
//		/// Executes a finally at any case when subscribing the source
//		/// </summary>
//		/// <param name="source">The main source to wrap</param>
//		/// <param name="finallyAction">The finally action</param>
//		/// <param name="scheduler">An scheduler where execute this action</param>
//		/// <returns>The modified source</returns>
//		public static IObservable<T> FinallyAlways<T>(this IObservable<T> source, Action finallyAction, IScheduler scheduler)
//        {
//			source.EnsureNotNull("source");
//			scheduler.EnsureNotNull("scheduler");
//
//			return Observable.Create<T>(o =>
//			{
//				return scheduler.Schedule(0,
//					(s, st) =>
//					{
//						try { return source.Subscribe(o); }
//						finally { finallyAction(); }
//					});
//			});
//		}

		/// <summary>
		/// Extension for start, load (charge) and release a trigger based on a given observable source.
		/// </summary>  
		/// <param name="source">The main source for the trigger</param>
		/// <param name="triggerStartSelector">Pass the source itself to the lambda -> OnNext opens a new trigger charging</param>
		/// <param name="triggerCancelSelector">Pass the trigger observed data since started to the lambda -> OnNext cancels (all) the previous started charges</param>
		/// <param name="releaseTimeSelector">Max timeSpan for incomming data, it not releases untils the source channel is empty for x seconds.</param>
		/// <param name="scheduler">An scheduler where execute this action</param>
		/// <param name="cancelPreviousOnTriggerStart">if true, closes (all) the previous started triggers based on triggerStartSelect.OnNext passing the trigger observed data since started</param>
		/// <returns>Returns an Unit in the sequence each time the trigger is released</returns>
		public static IObservable<Unit> Trigger<T, TDontCare1>(
			this IObservable<T> source,
			Func<T, bool> triggerStartSelector,
			Func<IObservable<T>, IObservable<TDontCare1>> triggerCancelSelector,
			Func<TimeSpan> releaseTimeSelector,
			IScheduler scheduler,
			bool cancelPreviousOnTriggerStart = true)
		{
			releaseTimeSelector.EnsureNotNull("releaseTimeSelector");
			scheduler.EnsureNotNull("scheduler");

            return source
				.Trigger(
					triggerStartSelector, triggerCancelSelector,
					s => s.Throttle(releaseTimeSelector(), scheduler),
					scheduler,
					cancelPreviousOnTriggerStart
                );
		}

		/// <summary>
		/// Extension for start, load (charge) and release a trigger based on a given observable source.
		/// </summary>
		/// <param name="source">The main source for the trigger</param>
		/// <param name="triggerStartSelector">Pass the source itself to the lambda -> OnNext opens a new trigger charging</param>
		/// <param name="triggerCancelSelector">Pass the trigger observed data since started to the lambda -> OnNext cancels (all) the previous started charges</param>
		/// <param name="triggerReleaseSelector">Pass the source itself to the lambda -> OnNext release the charged trigger</param>
		/// <param name="scheduler">An scheduler where execute this action</param>
		/// <param name="cancelPreviousOnTriggerStart">if true, closes (all) the previous started triggers based on triggerStartSelect.OnNext passing the trigger observed data since started</param>
		/// <returns>Returns an Unit in the sequence each time the trigger is released</returns>
		[Obsolete("Please use the Trigger with a TriggerOptions enum")]
		public static IObservable<Unit> Trigger<T, TDontCare1, TDontCare2>(this IObservable<T> source,
			Func<T, bool> triggerStartSelector, 
			Func<IObservable<T>, IObservable<TDontCare1>> triggerCancelSelector,
			Func<IObservable<T>, IObservable<TDontCare2>> triggerReleaseSelector,
			IScheduler scheduler,
			bool cancelPreviousOnTriggerStart = true)
		{
			source.EnsureNotNull("source");
			triggerCancelSelector.EnsureNotNull("triggerCancelSelector");
			triggerStartSelector.EnsureNotNull("triggerStartSelector");
			triggerReleaseSelector.EnsureNotNull("triggerReleaseSelector");
			scheduler.EnsureNotNull("scheduler");

		    return Observable.Create<Unit>(o =>
		    {
		        return scheduler.Schedule(
		            0, (s, st) =>
		            {
		                var refCountedSource = source.Publish().RefCount();
		                return refCountedSource
		                    .Window(refCountedSource.Where(triggerStartSelector),
		                        t => triggerReleaseSelector(refCountedSource.Merge(Observable.Return(t))))
		                    .Subscribe(trigger =>
		                    {
		                        var triggerDisposable = new CompositeDisposable();

		                        triggerDisposable.Add(trigger
		                            .Subscribe(_ => { }, () =>
		                            {
		                                o.OnNext(Unit.Default);
		                                triggerDisposable.Dispose();
		                            }));
		                        triggerDisposable.Add(triggerCancelSelector(trigger)
		                            .FirstOrDefaultAsync()
		                            .Subscribe(_ =>
		                            {
		                                triggerDisposable.Dispose();
		                            }));
		                        if (cancelPreviousOnTriggerStart)
		                            triggerDisposable.Add(trigger.Where(triggerStartSelector)
		                                .Skip(1)
		                                .Subscribe(_ =>
		                                {
		                                    triggerDisposable.Dispose();
		                                }));
		                    });
		            });
		    });

		}

	    public enum TriggerOptions
	    {
	        CancelPreviousOnTriggerStart,
            IndependentTriggers,
            DiscardTriggerIfAlreadyStarted
	    }

        /// <summary>
		/// Extension for start, load (charge) and release a trigger based on a given observable source.
		/// </summary>
		/// <param name="source">The main source for the trigger</param>
		/// <param name="triggerStartSelector">Pass the source itself to the lambda -> OnNext opens a new trigger charging</param>
		/// <param name="triggerCancelSelector">Pass the trigger observed data since started to the lambda -> OnNext cancels (all) the previous started charges</param>
		/// <param name="triggerReleaseSelector">Pass the source itself to the lambda -> OnNext release the charged trigger</param>
		/// <param name="scheduler">An scheduler where execute this action</param>
		/// <param name="cancelPreviousOnTriggerStart">if true, closes (all) the previous started triggers based on triggerStartSelect.OnNext passing the trigger observed data since started</param>
		/// <returns>Returns an Unit in the sequence each time the trigger is released</returns>
		public static IObservable<Unit> Trigger<T, TDontCare1, TDontCare2>(this IObservable<T> source,
            Func<T, bool> triggerStartSelector,
            Func<IObservable<T>, IObservable<TDontCare1>> triggerCancelSelector,
            Func<IObservable<T>, IObservable<TDontCare2>> triggerReleaseSelector,
            IScheduler scheduler,
            TriggerOptions triggerOptions)
        {
            source.EnsureNotNull("source");
            triggerCancelSelector.EnsureNotNull("triggerCancelSelector");
            triggerStartSelector.EnsureNotNull("triggerStartSelector");
            triggerReleaseSelector.EnsureNotNull("triggerReleaseSelector");
            scheduler.EnsureNotNull("scheduler");

            return Observable.Create<Unit>(o =>
            {
                return scheduler.Schedule(
                    0, (s, st) =>
                    {
                        var isStarted = 1; // 0: true __ 1: false
                        var refCountedSource = source.Publish().RefCount();
                        return refCountedSource
                            .Window(refCountedSource.Where(el => triggerStartSelector(el)
                                // No start if it is already started
                                && (triggerOptions == TriggerOptions.DiscardTriggerIfAlreadyStarted ? Interlocked.Exchange(ref isStarted, 0) == 0 : true)),
                                t => triggerReleaseSelector(refCountedSource.Merge(Observable.Return(t))))
                            .Subscribe(trigger =>
                            {
                                var triggerDisposable = new CompositeDisposable();

                                triggerDisposable.Add(trigger
                                    .Subscribe(_ => { }, () =>
                                    {
                                        Interlocked.Exchange(ref isStarted, 1);
                                        o.OnNext(Unit.Default);
                                        triggerDisposable.Dispose();
                                    }));
                                triggerDisposable.Add(triggerCancelSelector(trigger)
                                    .FirstOrDefaultAsync()
                                    .Subscribe(_ =>
                                    {
                                        Interlocked.Exchange(ref isStarted, 1);
                                        triggerDisposable.Dispose();
                                    }));
                                if (triggerOptions == TriggerOptions.CancelPreviousOnTriggerStart)
                                    triggerDisposable.Add(trigger.Where(triggerStartSelector)
                                        .Skip(1)
                                        .Subscribe(_ =>
                                        {
                                            triggerDisposable.Dispose();
                                        }));
                            });
                    });
            });

        }

        private class GroupedObservable<TKey, TElement> : IGroupedObservable<TKey, TElement>
        {
            private readonly IObservable<TElement> _o;
            private readonly TKey _key;

            public TKey Key { get { return _key; } }

            public GroupedObservable(TKey key, IObservable<TElement> o)
            {
                _key = key;
                _o = o;
            }

            public IDisposable Subscribe(IObserver<TElement> observer) { return _o.Subscribe(observer); }
        }

        public static IGroupedObservable<TKey, TSource> ConvertToGroup<TKey, TSource>(this IObservable<TSource> source, TKey key)
        {
            return new GroupedObservable<TKey, TSource>(key, source);
        }
	}
}
