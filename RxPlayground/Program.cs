using System;
using RxTools.IO;

namespace RxPlayground
{
	class Program
	{
		static void Main(string[] args)
		{
			G.Writer = ConsoleWriter.Default;
            Application.MainAsync().Wait();
			Console.ReadLine();
		}

		public static void a(string s)
		{ }

		public static class ConsoleWriter
		{
			public static readonly IWriter Default = new AnonymousWriter(Console.Write);
		}
	}
}
