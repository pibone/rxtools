using System;
using System.Diagnostics.Contracts;
using System.Reactive.Linq;

namespace RxTools
{
    public static  partial class EitherEx
    {
        /// <summary>
        /// Returns an observable that contains only the values from the left notification channel.
        /// </summary>
        /// <typeparam name="TLeft">Type of the left notification channel.</typeparam>
        /// <typeparam name="TRight">Type of the right notification channel.</typeparam>
        /// <param name="source">The observable from which values are taken.</param>
        /// <returns>An observable of values from the left notification channel.</returns>
        public static IObservable<TLeft> TakeLeft<TLeft, TRight>(
            this IObservable<Either<TLeft, TRight>> source)
        {
            Contract.Requires(source != null);
            Contract.Ensures(Contract.Result<IObservable<TLeft>>() != null);

            return Observable.Create<TLeft>(
                observer =>
                {
                    return source.SubscribeEither(
                        observer.OnNext,
                        right => { },
                        observer.OnError,
                        observer.OnCompleted);
                });
        }
    }
}
