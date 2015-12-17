using System;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using ReactiveUI.Testing;
using RxTools.IO;

namespace RxPlayground
{
	class Program
	{
		static void Main(string[] args)
		{
            var sched = new TestScheduler();
		    var newConnOb = sched.CreateHotObservable(
                sched.OnNextAt(100, "1"),
                sched.OnNextAt(200, "2"),
                sched.OnNextAt(600, "3")
            );

            var remConnOb = sched.CreateHotObservable(
                sched.OnNextAt(500, "2"),
                sched.OnNextAt(900, "1"),
                sched.OnNextAt(1000, "3")
            );

		    var packageSimulator = sched.CreateHotObservable(
		        sched.OnNextAt(101, Tuple.Create("1", "p1")),
                sched.OnNextAt(120, Tuple.Create("1", "p2")),
                sched.OnNextAt(250, Tuple.Create("2", "p1")),
                sched.OnNextAt(450, Tuple.Create("2", "p2")),
                sched.OnNextAt(700, Tuple.Create("3", "p3")),
                sched.OnNextAt(950, Tuple.Create("1", "p1"))
                );


		    var ob = Observable.Interval(TimeSpan.FromMilliseconds(50), sched).Publish().RefCount();
		    packageSimulator
                .Window(newConnOb, s => remConnOb.Where(c => c == s))
                .Subscribe(window =>
                {
                    string connection = null;
                    var windowPublish = window.Publish();

                    windowPublish.FirstOrDefaultAsync().Subscribe(p =>
                    {
                        if (p == null)
                        connection = p.Item1;
                        Console.WriteLine("Created {1} at {0}", sched.Now.Millisecond, connection);
                    });

                    windowPublish.Where(p => p.Item1 == connection).Subscribe(_ =>
                    {
                        Console.WriteLine("New item in {2} [{0}] at {1}", _, sched.Now.Millisecond, connection);
                    }, () =>
                    {
                        Console.WriteLine("Completed {1} at {0}", sched.Now.Millisecond, connection);
                    });
                    windowPublish.Connect();
                });

            sched.AdvanceToMs(1001);
		    Console.ReadLine();



//			G.Writer = ConsoleWriter.Default;
//            Application.MainAsync().Wait();
//			Console.ReadLine();
		}

		public static void a(string s)
		{ }

		public static class ConsoleWriter
		{
			public static readonly IWriter Default = new AnonymousWriter(Console.Write);
		}
	}
}
