using System;
using System.Diagnostics.Contracts;
using System.Reactive;

namespace RxTools
{
    public static class ObserverEx
    {
        /// <summary>
        /// Creates an observer that is capable of observing an observable with two notification channels using the specified actions.
        /// </summary>
        /// <typeparam name="TLeft">Type of the left notification channel.</typeparam>
        /// <typeparam name="TRight">Type of the right notification channel.</typeparam>
        /// <param name="onNextLeft">Handler for notifications from the left channel.</param>
        /// <param name="onNextRight">Handler for notifications from the right channel.</param>
        /// <returns>An observer capable of observing an observable with two notification channels.</returns>
        public static IObserver<Either<TLeft, TRight>> CreateEither<TLeft, TRight>(
            Action<TLeft> onNextLeft,
            Action<TRight> onNextRight)
        {
            Contract.Requires(onNextLeft != null);
            Contract.Requires(onNextRight != null);
            Contract.Ensures(Contract.Result<IObserver<Either<TLeft, TRight>>>() != null);

            return CreateEither(
                onNextLeft,
                onNextRight,
                ex =>
                {
                    throw ex; /*.PrepareForRethrow(); changed to internal in Rx 1.1.10425 */
                },
                () => { });
        }

        /// <summary>
        /// Creates an observer that is capable of observing an observable with two notification channels using the specified actions.
        /// </summary>
        /// <typeparam name="TLeft">Type of the left notification channel.</typeparam>
        /// <typeparam name="TRight">Type of the right notification channel.</typeparam>
        /// <param name="onNextLeft">Handler for notifications from the left channel.</param>
        /// <param name="onNextRight">Handler for notifications from the right channel.</param>
        /// <param name="onError">Handler for an error notification.</param>
        /// <returns>An observer capable of observing an observable with two notification channels.</returns>
        public static IObserver<Either<TLeft, TRight>> CreateEither<TLeft, TRight>(
            Action<TLeft> onNextLeft,
            Action<TRight> onNextRight,
            Action<Exception> onError)
        {
            Contract.Requires(onNextLeft != null);
            Contract.Requires(onNextRight != null);
            Contract.Requires(onError != null);
            Contract.Ensures(Contract.Result<IObserver<Either<TLeft, TRight>>>() != null);

            return CreateEither(
                onNextLeft,
                onNextRight,
                onError,
                () => { });
        }

        /// <summary>
        /// Creates an observer that is capable of observing an observable with two notification channels using the specified actions.
        /// </summary>
        /// <typeparam name="TLeft">Type of the left notification channel.</typeparam>
        /// <typeparam name="TRight">Type of the right notification channel.</typeparam>
        /// <param name="onNextLeft">Handler for notifications from the left channel.</param>
        /// <param name="onNextRight">Handler for notifications from the right channel.</param>
        /// <param name="onCompleted">Handler for a completed notification.</param>
        /// <returns>An observer capable of observing an observable with two notification channels.</returns>
        public static IObserver<Either<TLeft, TRight>> CreateEither<TLeft, TRight>(
            Action<TLeft> onNextLeft,
            Action<TRight> onNextRight,
            Action onCompleted)
        {
            Contract.Requires(onNextLeft != null);
            Contract.Requires(onNextRight != null);
            Contract.Requires(onCompleted != null);
            Contract.Ensures(Contract.Result<IObserver<Either<TLeft, TRight>>>() != null);

            return CreateEither(
                onNextLeft,
                onNextRight,
                ex =>
                {
                    throw ex; /*.PrepareForRethrow(); changed to internal in Rx 1.1.10425 */
                },
                onCompleted);
        }

        /// <summary>
        /// Creates an observer that is capable of observing an observable with two notification channels using the specified actions.
        /// </summary>
        /// <typeparam name="TLeft">Type of the left notification channel.</typeparam>
        /// <typeparam name="TRight">Type of the right notification channel.</typeparam>
        /// <param name="onNextLeft">Handler for notifications from the left channel.</param>
        /// <param name="onNextRight">Handler for notifications from the right channel.</param>
        /// <param name="onError">Handler for an error notification.</param>
        /// <param name="onCompleted">Handler for a completed notification.</param>
        /// <returns>An observer capable of observing an observable with two notification channels.</returns>
        public static IObserver<Either<TLeft, TRight>> CreateEither<TLeft, TRight>(
            Action<TLeft> onNextLeft,
            Action<TRight> onNextRight,
            Action<Exception> onError,
            Action onCompleted)
        {
            Contract.Requires(onNextLeft != null);
            Contract.Requires(onNextRight != null);
            Contract.Requires(onError != null);
            Contract.Requires(onCompleted != null);
            Contract.Ensures(Contract.Result<IObserver<Either<TLeft, TRight>>>() != null);

            return Observer.Create<Either<TLeft, TRight>>(
                value => value.Switch(onNextLeft, onNextRight),
                onError,
                onCompleted);
        }

        /// <summary>
        /// Provides the observer with new data in the left notification channel.
        /// </summary>
        /// <typeparam name="TLeft">Type of the left notification channel.</typeparam>
        /// <typeparam name="TRight">Type of the right notification channel.</typeparam>
        /// <param name="observer">The object to be notified.</param>
        /// <param name="left">The current left notification information.</param>
        public static void OnNextLeft<TLeft, TRight>(this IObserver<Either<TLeft, TRight>> observer, TLeft left)
        {
            Contract.Requires(observer != null);

            observer.OnNext(Either.Left<TLeft, TRight>(left));
        }

        /// <summary>
        /// Provides the observer with new data in the right notification channel.
        /// </summary>
        /// <typeparam name="TLeft">Type of the left notification channel.</typeparam>
        /// <typeparam name="TRight">Type of the right notification channel.</typeparam>
        /// <param name="observer">The object to be notified.</param>
        /// <param name="right">The current right notification information.</param>
        public static void OnNextRight<TLeft, TRight>(this IObserver<Either<TLeft, TRight>> observer, TRight right)
        {
            Contract.Requires(observer != null);

            observer.OnNext(Either.Right<TLeft, TRight>(right));
        }
    }


    public static class ObservableEx
    {
        /// <summary>
        /// Notifies the observable that an observer is to receive notifications.
        /// </summary>
        /// <typeparam name="TLeft">Type of the left notification channel.</typeparam>
        /// <typeparam name="TRight">Type of the right notification channel.</typeparam>
        /// <param name="source">The observable for which a subscription is created.</param>
        /// <param name="onNextLeft">The handler of notifications in the left channel.</param>
        /// <param name="onNextRight">The handler of notifications in the right channel.</param>
        /// <returns>The observer's interface that enables cancelation of the subscription so that it stops receiving notifications.</returns>
        public static IDisposable SubscribeEither<TLeft, TRight>(
            this IObservable<Either<TLeft, TRight>> source,
            Action<TLeft> onNextLeft,
            Action<TRight> onNextRight)
        {
            Contract.Requires(source != null);
            Contract.Requires(onNextLeft != null);
            Contract.Requires(onNextRight != null);
            Contract.Ensures(Contract.Result<IDisposable>() != null);

            return source.Subscribe(ObserverEx.CreateEither(
                onNextLeft,
                onNextRight));
        }

        /// <summary>
        /// Notifies the observable that an observer is to receive notifications.
        /// </summary>
        /// <typeparam name="TLeft">Type of the left notification channel.</typeparam>
        /// <typeparam name="TRight">Type of the right notification channel.</typeparam>
        /// <param name="source">The observable for which a subscription is created.</param>
        /// <param name="onNextLeft">The handler of notifications in the left channel.</param>
        /// <param name="onNextRight">The handler of notifications in the right channel.</param>
        /// <param name="onError">The handler of an error notification.</param>
        /// <returns>The observer's interface that enables cancelation of the subscription so that it stops receiving notifications.</returns>
        public static IDisposable SubscribeEither<TLeft, TRight>(
            this IObservable<Either<TLeft, TRight>> source,
            Action<TLeft> onNextLeft,
            Action<TRight> onNextRight,
            Action<Exception> onError)
        {
            Contract.Requires(source != null);
            Contract.Requires(onNextLeft != null);
            Contract.Requires(onNextRight != null);
            Contract.Requires(onError != null);
            Contract.Ensures(Contract.Result<IDisposable>() != null);

            return source.Subscribe(ObserverEx.CreateEither(
                onNextLeft,
                onNextRight,
                onError));
        }

        /// <summary>
        /// Notifies the observable that an observer is to receive notifications.
        /// </summary>
        /// <typeparam name="TLeft">Type of the left notification channel.</typeparam>
        /// <typeparam name="TRight">Type of the right notification channel.</typeparam>
        /// <param name="source">The observable for which a subscription is created.</param>
        /// <param name="onNextLeft">The handler of notifications in the left channel.</param>
        /// <param name="onNextRight">The handler of notifications in the right channel.</param>
        /// <param name="onCompleted">The handler of a completion notification.</param>
        /// <returns>The observer's interface that enables cancelation of the subscription so that it stops receiving notifications.</returns>
        public static IDisposable SubscribeEither<TLeft, TRight>(
            this IObservable<Either<TLeft, TRight>> source,
            Action<TLeft> onNextLeft,
            Action<TRight> onNextRight,
            Action onCompleted)
        {
            Contract.Requires(source != null);
            Contract.Requires(onNextLeft != null);
            Contract.Requires(onNextRight != null);
            Contract.Requires(onCompleted != null);
            Contract.Ensures(Contract.Result<IDisposable>() != null);

            return source.Subscribe(ObserverEx.CreateEither(
                onNextLeft,
                onNextRight,
                onCompleted));
        }

        /// <summary>
        /// Notifies the observable that an observer is to receive notifications.
        /// </summary>
        /// <typeparam name="TLeft">Type of the left notification channel.</typeparam>
        /// <typeparam name="TRight">Type of the right notification channel.</typeparam>
        /// <param name="source">The observable for which a subscription is created.</param>
        /// <param name="onNextLeft">The handler of notifications in the left channel.</param>
        /// <param name="onNextRight">The handler of notifications in the right channel.</param>
        /// <param name="onError">The handler of an error notification.</param>
        /// <param name="onCompleted">The handler of a completion notification.</param>
        /// <returns>The observer's interface that enables cancelation of the subscription so that it stops receiving notifications.</returns>
        public static IDisposable SubscribeEither<TLeft, TRight>(
            this IObservable<Either<TLeft, TRight>> source,
            Action<TLeft> onNextLeft,
            Action<TRight> onNextRight,
            Action<Exception> onError,
            Action onCompleted)
        {
            Contract.Requires(source != null);
            Contract.Requires(onNextLeft != null);
            Contract.Requires(onNextRight != null);
            Contract.Requires(onError != null);
            Contract.Requires(onCompleted != null);
            Contract.Ensures(Contract.Result<IDisposable>() != null);

            return source.Subscribe(ObserverEx.CreateEither(
                onNextLeft,
                onNextRight,
                onError,
                onCompleted));
        }
    }
}
