using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Diagnostics;

[EventSource(Name = "Microsoft-Diagnostics-DiagnosticSource")]
internal sealed class DiagnosticSourceEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords Messages = (EventKeywords)1L;

		public const EventKeywords Events = (EventKeywords)2L;

		public const EventKeywords IgnoreShortCutKeywords = (EventKeywords)2048L;

		public const EventKeywords AspNetCoreHosting = (EventKeywords)4096L;

		public const EventKeywords EntityFrameworkCoreCommands = (EventKeywords)8192L;
	}

	public static readonly DiagnosticSourceEventSource Log = new DiagnosticSourceEventSource();

	private readonly string AspNetCoreHostingKeywordValue = "Microsoft.AspNetCore/Microsoft.AspNetCore.Hosting.BeginRequest@Activity1Start:-httpContext.Request.Method;httpContext.Request.Host;httpContext.Request.Path;httpContext.Request.QueryString\nMicrosoft.AspNetCore/Microsoft.AspNetCore.Hosting.EndRequest@Activity1Stop:-httpContext.TraceIdentifier;httpContext.Response.StatusCode";

	private readonly string EntityFrameworkCoreCommandsKeywordValue = "Microsoft.EntityFrameworkCore/Microsoft.EntityFrameworkCore.BeforeExecuteCommand@Activity2Start:-Command.Connection.DataSource;Command.Connection.Database;Command.CommandText\nMicrosoft.EntityFrameworkCore/Microsoft.EntityFrameworkCore.AfterExecuteCommand@Activity2Stop:-";

	private IDisposable _listener;

	private volatile bool _false;

	[Event(1, Keywords = (EventKeywords)1L)]
	public void Message(string Message)
	{
		WriteEvent(1, Message);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is preserved by DynamicDependency")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<, >))]
	[Event(2, Keywords = (EventKeywords)2L)]
	public void Event(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(2, SourceName, EventName, Arguments);
	}

	[Event(3, Keywords = (EventKeywords)2L)]
	public void EventJson(string SourceName, string EventName, string ArgmentsJson)
	{
		WriteEvent(3, SourceName, EventName, ArgmentsJson);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is preserved by DynamicDependency")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<, >))]
	[Event(4, Keywords = (EventKeywords)2L)]
	public void Activity1Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(4, SourceName, EventName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is preserved by DynamicDependency")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<, >))]
	[Event(5, Keywords = (EventKeywords)2L)]
	public void Activity1Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(5, SourceName, EventName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is preserved by DynamicDependency")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<, >))]
	[Event(6, Keywords = (EventKeywords)2L)]
	public void Activity2Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(6, SourceName, EventName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is preserved by DynamicDependency")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<, >))]
	[Event(7, Keywords = (EventKeywords)2L)]
	public void Activity2Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(7, SourceName, EventName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is preserved by DynamicDependency")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<, >))]
	[Event(8, Keywords = (EventKeywords)2L, ActivityOptions = EventActivityOptions.Recursive)]
	public void RecursiveActivity1Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(8, SourceName, EventName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is preserved by DynamicDependency")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<, >))]
	[Event(9, Keywords = (EventKeywords)2L, ActivityOptions = EventActivityOptions.Recursive)]
	public void RecursiveActivity1Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(9, SourceName, EventName, Arguments);
	}

	[Event(10, Keywords = (EventKeywords)2L)]
	public void NewDiagnosticListener(string SourceName)
	{
		WriteEvent(10, SourceName);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is preserved by DynamicDependency")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<, >))]
	[Event(11, Keywords = (EventKeywords)2L, ActivityOptions = EventActivityOptions.Recursive)]
	public void ActivityStart(string SourceName, string ActivityName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(11, SourceName, ActivityName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is preserved by DynamicDependency")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<, >))]
	[Event(12, Keywords = (EventKeywords)2L, ActivityOptions = EventActivityOptions.Recursive)]
	public void ActivityStop(string SourceName, string ActivityName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(12, SourceName, ActivityName, Arguments);
	}

	[Event(13, Keywords = (EventKeywords)1L)]
	public void Version(int Major, int Minor, int Patch)
	{
		WriteEvent(13, Major, Minor, Patch);
	}

	[NonEvent]
	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		if (command.Command == EventCommand.Enable)
		{
			Version(ThisAssembly.AssemblyFileVersion.Major, ThisAssembly.AssemblyFileVersion.Minor, ThisAssembly.AssemblyFileVersion.Build);
		}
		BreakPointWithDebuggerFuncEval();
		lock (this)
		{
			if ((command.Command == EventCommand.Update || command.Command == EventCommand.Enable) && IsEnabled(EventLevel.Informational, (EventKeywords)2L))
			{
				string value = null;
				command.Arguments.TryGetValue("FilterAndPayloadSpecs", out value);
				if (!IsEnabled(EventLevel.Informational, (EventKeywords)2048L))
				{
					if (IsEnabled(EventLevel.Informational, (EventKeywords)4096L))
					{
						value = NewLineSeparate(value, AspNetCoreHostingKeywordValue);
					}
					if (IsEnabled(EventLevel.Informational, (EventKeywords)8192L))
					{
						value = NewLineSeparate(value, EntityFrameworkCoreCommandsKeywordValue);
					}
				}
				_listener?.Dispose();
				_listener = DsesFilterAndTransform.ParseFilterAndPayloadSpecs(this, value);
			}
			else if (command.Command == EventCommand.Update || command.Command == EventCommand.Disable)
			{
				_listener?.Dispose();
			}
		}
	}

	private DiagnosticSourceEventSource()
		: base(EventSourceSettings.EtwSelfDescribingEventFormat)
	{
	}

	private static string NewLineSeparate(string str1, string str2)
	{
		if (string.IsNullOrEmpty(str1))
		{
			return str2;
		}
		return str1 + "\n" + str2;
	}

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
	[NonEvent]
	private void BreakPointWithDebuggerFuncEval()
	{
		new object();
		while (_false)
		{
			_false = false;
		}
	}
}
