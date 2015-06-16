using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using ReactiveUI.Testing;
using RxTools;
using RxTools.IO;

namespace RxPlayground
{
	public static class G
	{
		public static IWriter Writer = NullWriter.Default;
	}

	public class Application
	{
		public static async Task MainAsync()
		{
			var sched = new TestScheduler();
			var test = sched.CreateColdObservable(
					sched.OnNextAt(200, 1),
					sched.OnNextAt(400, 1),
					sched.OnNextAt(600, 2),
					sched.OnNextAt(800, 3),
					sched.OnNextAt(1000, 1),
					sched.OnNextAt(1200, 1),
					sched.OnNextAt(1400, 4),
					sched.OnNextAt(1600, 1),
					sched.OnNextAt(1800, 4),
					sched.OnNextAt(2000, 2),
					sched.OnNextAt(2200, 1),
					sched.OnNextAt(2400, 3),
					sched.OnNextAt(2600, 1),
					sched.OnNextAt(3000, 1),
					sched.OnNextAt(6000, 4)
				);
				test
				.Dump(G.Writer)
				.Trigger(i => i == 1, o => o.Where(i => i > 1 && i < 4), () => TimeSpan.FromSeconds(2), sched)
				.Subscribe(
				    _ =>
				    {
						G.Writer.WriteLine("launched");
				    });
			var count = 0;
			do
			{
				count += 100;
				sched.AdvanceToMs(count);
				Console.WriteLine("Time " + count);
			}
			while (string.IsNullOrWhiteSpace(Console.ReadLine()));

		}
	}
}
