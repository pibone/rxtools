using System;
using System.Reactive.Disposables;

namespace gcperu.Term.Utils
{
	public static class DisposableEx
	{
		public static void AddToComposite(this IDisposable This, CompositeDisposable disposable)
		{
			disposable.Add(This);
		}

        public static void ThrowIfDisposed<T>(this T This, Func<T, bool> selector) where T : class
        {
            lock (This)
            {
                if (selector(This))
                {
                    throw new ObjectDisposedException(This.GetType().Name);
                }
            }
        }
	}
}