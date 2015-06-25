using System;
using System.Collections.Generic;

namespace RxTools.IO
{
	public interface IWriter
	{
		void Write(string tag, string text);
	}

	public static class WriterExtensions
	{
		public static void Write(this IWriter writer, string text)
		{
			writer.Write("", text);
		}

		public static void WriteLine(this IWriter writer, string text)
		{
			writer.Write(text + "\n");
		}

		public static void WriteLineIf(this IWriter writer, bool condition, string text)
		{
			if (condition)
				writer.Write(text + "\n");
		}

		public static void WriteIf(this IWriter writer, bool condition, string text)
		{
			if (condition)
				writer.Write(text);
		}

		public static void WriteLine(this IWriter writer, string tag, string text)
		{
			writer.Write(tag, text + "\n");
		}

		public static void WriteLineIf(this IWriter writer, bool condition, string tag, string text)
		{
			if (condition)
				writer.Write(tag, text + "\n");
		}

		public static void WriteIf(this IWriter writer, bool condition, string tag, string text)
		{
			if (condition)
				writer.Write(tag, text);
		}

		public static IWriter ToWriter(this Action<string> action)
		{
			return new AnonymousWriter(action);
		}

		public static IWriter ToWriter(this IEnumerable<Action<string>> actions)
		{
			return new AnonymousWriter(actions);
		}
	}

	public sealed class NullWriter : IWriter
	{
		public static readonly IWriter Default = new NullWriter();
		private NullWriter() { }
		public void Write(string tag, string text) { }
	}

	public sealed class AnonymousWriter : IWriter
	{
		private readonly IEnumerable<Action<string>> _writerActions;

		public IWriter Create(Action<string> writerAction)
		{
			return new AnonymousWriter(writerAction);
		}

		public IWriter Create(IEnumerable<Action<string>> writerActions)
		{
			return new AnonymousWriter(writerActions);
		}

		public AnonymousWriter(Action<string> writerAction)
		{
			_writerActions = writerAction != null ? new [] {writerAction} : new Action<string>[] {};
		}

		public AnonymousWriter(IEnumerable<Action<string>> writerActions)
		{
			_writerActions = writerActions ?? new Action<string>[] { };
		}

		public void Write(string tag, string text)
		{
			tag.EnsureNotNull("tag");
			text.EnsureNotNull("text");

			tag = tag.Trim();
			if (!string.IsNullOrWhiteSpace(tag)) tag = tag + " ";

			foreach (var action in _writerActions) { action(tag + text); };
		}
	}
}
