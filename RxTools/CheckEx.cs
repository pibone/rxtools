using System;

namespace RxTools
{
	public static class CheckEx
	{
		public static void EnsureNotNull<T>(this T item, string paramName)
		{
			if (item == null)
				throw new ArgumentNullException(paramName);
		}
	}
}
