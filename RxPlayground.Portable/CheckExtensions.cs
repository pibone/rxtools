using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxTools
{
	public static class CheckExtensions
	{
		public static void EnsureNotNull<T>(this T item, string paramName)
		{
			if (item == null)
				throw new ArgumentNullException(paramName);
		}
	}
}
