using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace System.Diagnostics.Metrics;

[EventSource(Name = "System.Diagnostics.Metrics")]
internal sealed class MetricsEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords Messages = (EventKeywords)1L;

		public const EventKeywords TimeSeriesValues = (EventKeywords)2L;

		public const EventKeywords InstrumentPublishing = (EventKeywords)4L;
	}

	private sealed class CommandHandler
	{
		private AggregationManager _aggregationManager;

		private string _sessionId = "";

		private HashSet<string> _sharedSessionClientIds = new HashSet<string>();

		private int _sharedSessionRefCount;

		private bool _disabledRefCount;

		private static readonly char[] s_instrumentSeparators = new char[4] { '\r', '\n', ',', ';' };

		public MetricsEventSource Parent { get; }

		public CommandHandler(MetricsEventSource parent)
		{
			Parent = parent;
		}

		public bool IsSharedSession(string commandSessionId)
		{
			if (_sessionId.Equals("SHARED"))
			{
				if (!string.IsNullOrEmpty(commandSessionId))
				{
					return commandSessionId.Equals("SHARED");
				}
				return true;
			}
			return false;
		}

		public void OnEventCommand(EventCommandEventArgs command)
		{
			try
			{
				string sessionId = GetSessionId(command);
				if ((command.Command == EventCommand.Update || command.Command == EventCommand.Disable || command.Command == EventCommand.Enable) && _aggregationManager != null)
				{
					if (command.Command == EventCommand.Update || command.Command == EventCommand.Enable)
					{
						IncrementRefCount(sessionId, command);
					}
					if (IsSharedSession(sessionId))
					{
						if (ShouldDisable(command.Command))
						{
							Parent.Message("Previous session with id " + _sessionId + " is stopped");
							_aggregationManager.Dispose();
							_aggregationManager = null;
							_sessionId = string.Empty;
							_sharedSessionClientIds.Clear();
							return;
						}
						bool flag = true;
						double refreshIntervalSeconds;
						lock (_aggregationManager)
						{
							flag = SetSharedRefreshIntervalSecs(command.Arguments, _aggregationManager.CollectionPeriod.TotalSeconds, out refreshIntervalSeconds) && flag;
						}
						flag = SetSharedMaxHistograms(command.Arguments, _aggregationManager.MaxHistograms, out var maxHistograms) && flag;
						flag = SetSharedMaxTimeSeries(command.Arguments, _aggregationManager.MaxTimeSeries, out var maxTimeSeries) && flag;
						if (command.Command != EventCommand.Disable)
						{
							string value;
							if (flag)
							{
								if (ParseMetrics(command.Arguments, out var metricsSpecs))
								{
									ParseSpecs(metricsSpecs);
									_aggregationManager.Update();
								}
							}
							else if (command.Arguments.TryGetValue("ClientId", out value))
							{
								lock (_aggregationManager)
								{
									Parent.MultipleSessionsConfiguredIncorrectlyError(value, _aggregationManager.MaxHistograms.ToString(), maxHistograms.ToString(), _aggregationManager.MaxTimeSeries.ToString(), maxTimeSeries.ToString(), _aggregationManager.CollectionPeriod.TotalSeconds.ToString(), refreshIntervalSeconds.ToString());
									return;
								}
							}
							return;
						}
					}
					else
					{
						if (command.Command == EventCommand.Enable || command.Command == EventCommand.Update)
						{
							Parent.MultipleSessionsNotSupportedError(_sessionId);
							return;
						}
						if (ShouldDisable(command.Command))
						{
							Parent.Message("Previous session with id " + _sessionId + " is stopped");
							_aggregationManager.Dispose();
							_aggregationManager = null;
							_sessionId = string.Empty;
							_sharedSessionClientIds.Clear();
							return;
						}
					}
				}
				if ((command.Command == EventCommand.Update || command.Command == EventCommand.Enable) && command.Arguments != null)
				{
					IncrementRefCount(sessionId, command);
					_sessionId = sessionId;
					double defaultValue = 1.0;
					SetRefreshIntervalSecs(command.Arguments, 0.1, defaultValue, out var refreshIntervalSeconds2);
					SetUniqueMaxTimeSeries(command.Arguments, 1000, out var maxTimeSeries2);
					SetUniqueMaxHistograms(command.Arguments, 20, out var maxHistograms2);
					string sessionId2 = _sessionId;
					_aggregationManager = new AggregationManager(maxTimeSeries2, maxHistograms2, delegate(Instrument i, LabeledAggregationStatistics s, InstrumentState state)
					{
						TransmitMetricValue(i, s, sessionId2, state);
					}, delegate(DateTime startIntervalTime, DateTime endIntervalTime)
					{
						Parent.CollectionStart(sessionId2, startIntervalTime, endIntervalTime);
					}, delegate(DateTime startIntervalTime, DateTime endIntervalTime)
					{
						Parent.CollectionStop(sessionId2, startIntervalTime, endIntervalTime);
					}, delegate(Instrument i, InstrumentState state)
					{
						Parent.BeginInstrumentReporting(sessionId2, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description, Helpers.FormatTags(i.Tags), Helpers.FormatTags(i.Meter.Tags), Helpers.FormatObjectHash(i.Meter.Scope), state.ID);
					}, delegate(Instrument i, InstrumentState state)
					{
						Parent.EndInstrumentReporting(sessionId2, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description, Helpers.FormatTags(i.Tags), Helpers.FormatTags(i.Meter.Tags), Helpers.FormatObjectHash(i.Meter.Scope), state.ID);
					}, delegate(Instrument i, InstrumentState state)
					{
						Parent.InstrumentPublished(sessionId2, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description, Helpers.FormatTags(i.Tags), Helpers.FormatTags(i.Meter.Tags), Helpers.FormatObjectHash(i.Meter.Scope), state?.ID ?? 0);
					}, delegate
					{
						Parent.InitialInstrumentEnumerationComplete(sessionId2);
					}, delegate(Exception ex)
					{
						Parent.Error(sessionId2, ex.ToString());
					}, delegate
					{
						Parent.TimeSeriesLimitReached(sessionId2);
					}, delegate
					{
						Parent.HistogramLimitReached(sessionId2);
					}, delegate(Exception ex)
					{
						Parent.ObservableInstrumentCallbackError(sessionId2, ex.ToString());
					});
					_aggregationManager.SetCollectionPeriod(TimeSpan.FromSeconds(refreshIntervalSeconds2));
					if (ParseMetrics(command.Arguments, out var metricsSpecs2))
					{
						ParseSpecs(metricsSpecs2);
					}
					_aggregationManager.Start();
				}
			}
			catch (Exception e) when (LogError(e))
			{
			}
		}

		private bool ShouldDisable(EventCommand command)
		{
			if (command == EventCommand.Disable)
			{
				if (_disabledRefCount || Interlocked.Decrement(ref _sharedSessionRefCount) != 0)
				{
					return !Parent.IsEnabled();
				}
				return true;
			}
			return false;
		}

		private bool ParseMetrics(IDictionary<string, string> arguments, out string metricsSpecs)
		{
			if (arguments.TryGetValue("Metrics", out metricsSpecs))
			{
				Parent.Message("Metrics argument received: " + metricsSpecs);
				return true;
			}
			Parent.Message("No Metrics argument received");
			return false;
		}

		private void InvalidateRefCounting()
		{
			_disabledRefCount = true;
			Parent.Message("ClientId not provided; session will remain active indefinitely.");
		}

		private void IncrementRefCount(string clientId, EventCommandEventArgs command)
		{
			if (clientId.Equals("SHARED"))
			{
				if (command.Arguments.TryGetValue("ClientId", out string value) && !string.IsNullOrEmpty(value))
				{
					clientId = value;
				}
				else
				{
					InvalidateRefCounting();
				}
			}
			if (_sharedSessionClientIds.Add(clientId))
			{
				Interlocked.Increment(ref _sharedSessionRefCount);
			}
		}

		private bool SetSharedMaxTimeSeries(IDictionary<string, string> arguments, int sharedValue, out int maxTimeSeries)
		{
			return SetMaxValue(arguments, "MaxTimeSeries", "shared value", sharedValue, out maxTimeSeries);
		}

		private void SetUniqueMaxTimeSeries(IDictionary<string, string> arguments, int defaultValue, out int maxTimeSeries)
		{
			SetMaxValue(arguments, "MaxTimeSeries", "default", defaultValue, out maxTimeSeries);
		}

		private bool SetSharedMaxHistograms(IDictionary<string, string> arguments, int sharedValue, out int maxHistograms)
		{
			return SetMaxValue(arguments, "MaxHistograms", "shared value", sharedValue, out maxHistograms);
		}

		private void SetUniqueMaxHistograms(IDictionary<string, string> arguments, int defaultValue, out int maxHistograms)
		{
			SetMaxValue(arguments, "MaxHistograms", "default", defaultValue, out maxHistograms);
		}

		private bool SetMaxValue(IDictionary<string, string> arguments, string argumentsKey, string valueDescriptor, int defaultValue, out int maxValue)
		{
			if (arguments.TryGetValue(argumentsKey, out var value))
			{
				Parent.Message(argumentsKey + " argument received: " + value);
				if (!int.TryParse(value, out maxValue))
				{
					Parent.Message($"Failed to parse {argumentsKey}. Using {valueDescriptor} {defaultValue}");
					maxValue = defaultValue;
				}
				else if (maxValue != defaultValue)
				{
					return false;
				}
			}
			else
			{
				Parent.Message($"No {argumentsKey} argument received. Using {valueDescriptor} {defaultValue}");
				maxValue = defaultValue;
			}
			return true;
		}

		private void SetRefreshIntervalSecs(IDictionary<string, string> arguments, double minValue, double defaultValue, out double refreshIntervalSeconds)
		{
			if (GetRefreshIntervalSecs(arguments, "default", defaultValue, out refreshIntervalSeconds) && refreshIntervalSeconds < minValue)
			{
				Parent.Message(string.Format("{0} too small. Using minimum interval {1} seconds.", "RefreshInterval", minValue));
				refreshIntervalSeconds = minValue;
			}
		}

		private bool SetSharedRefreshIntervalSecs(IDictionary<string, string> arguments, double sharedValue, out double refreshIntervalSeconds)
		{
			if (GetRefreshIntervalSecs(arguments, "shared value", sharedValue, out refreshIntervalSeconds) && refreshIntervalSeconds != sharedValue)
			{
				return false;
			}
			return true;
		}

		private bool GetRefreshIntervalSecs(IDictionary<string, string> arguments, string valueDescriptor, double defaultValue, out double refreshIntervalSeconds)
		{
			if (arguments.TryGetValue("RefreshInterval", out var value))
			{
				Parent.Message("RefreshInterval argument received: " + value);
				if (!double.TryParse(value, out refreshIntervalSeconds))
				{
					Parent.Message(string.Format("Failed to parse {0}. Using {1} {2}s.", "RefreshInterval", valueDescriptor, defaultValue));
					refreshIntervalSeconds = defaultValue;
					return false;
				}
				return true;
			}
			Parent.Message(string.Format("No {0} argument received. Using {1} {2}s.", "RefreshInterval", valueDescriptor, defaultValue));
			refreshIntervalSeconds = defaultValue;
			return false;
		}

		private string GetSessionId(EventCommandEventArgs command)
		{
			if (command.Arguments.TryGetValue("SessionId", out string value))
			{
				Parent.Message("SessionId argument received: " + value);
				return value;
			}
			string text = string.Empty;
			if (command.Command != EventCommand.Disable)
			{
				text = Guid.NewGuid().ToString();
				Parent.Message("New session started. SessionId auto-generated: " + text);
			}
			return text;
		}

		private bool LogError(Exception e)
		{
			Parent.Error(_sessionId, e.ToString());
			return false;
		}

		[UnsupportedOSPlatform("browser")]
		private void ParseSpecs(string metricsSpecs)
		{
			if (metricsSpecs == null)
			{
				return;
			}
			string[] array = metricsSpecs.Split(s_instrumentSeparators, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				MetricSpec metricSpec = MetricSpec.Parse(array[i]);
				Parent.Message($"Parsed metric: {metricSpec}");
				if (metricSpec.InstrumentName != null)
				{
					_aggregationManager.Include(metricSpec.MeterName, metricSpec.InstrumentName);
				}
				else if (metricSpec.MeterName.Length > 0 && metricSpec.MeterName[metricSpec.MeterName.Length - 1] == '*')
				{
					if (metricSpec.MeterName.Length == 1)
					{
						_aggregationManager.IncludeAll();
					}
					else
					{
						_aggregationManager.IncludePrefix(metricSpec.MeterName.Substring(0, metricSpec.MeterName.Length - 1));
					}
				}
				else
				{
					_aggregationManager.Include(metricSpec.MeterName);
				}
			}
		}

		private static void TransmitMetricValue(Instrument instrument, LabeledAggregationStatistics stats, string sessionId, InstrumentState instrumentState)
		{
			int instrumentId = instrumentState?.ID ?? 0;
			if (stats.AggregationStatistics is CounterStatistics counterStatistics)
			{
				if (counterStatistics.IsMonotonic)
				{
					Log.CounterRateValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, Helpers.FormatTags(stats.Labels), counterStatistics.Delta.HasValue ? counterStatistics.Delta.Value.ToString(CultureInfo.InvariantCulture) : "", counterStatistics.Value.ToString(CultureInfo.InvariantCulture), instrumentId);
				}
				else
				{
					Log.UpDownCounterRateValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, Helpers.FormatTags(stats.Labels), counterStatistics.Delta.HasValue ? counterStatistics.Delta.Value.ToString(CultureInfo.InvariantCulture) : "", counterStatistics.Value.ToString(CultureInfo.InvariantCulture), instrumentId);
				}
			}
			else if (stats.AggregationStatistics is LastValueStatistics lastValueStatistics)
			{
				Log.GaugeValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, Helpers.FormatTags(stats.Labels), lastValueStatistics.LastValue.HasValue ? lastValueStatistics.LastValue.Value.ToString(CultureInfo.InvariantCulture) : "", instrumentId);
			}
			else if (stats.AggregationStatistics is SynchronousLastValueStatistics synchronousLastValueStatistics)
			{
				Log.GaugeValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, Helpers.FormatTags(stats.Labels), synchronousLastValueStatistics.LastValue.ToString(CultureInfo.InvariantCulture), instrumentId);
			}
			else if (stats.AggregationStatistics is HistogramStatistics histogramStatistics)
			{
				Log.HistogramValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, Helpers.FormatTags(stats.Labels), FormatQuantiles(histogramStatistics.Quantiles), histogramStatistics.Count, histogramStatistics.Sum, instrumentId);
			}
		}

		private static string FormatQuantiles(QuantileValue[] quantiles)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < quantiles.Length; i++)
			{
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", quantiles[i].Quantile, quantiles[i].Value);
				if (i != quantiles.Length - 1)
				{
					stringBuilder.Append(';');
				}
			}
			return stringBuilder.ToString();
		}
	}

	private sealed class MetricSpec
	{
		private const char MeterInstrumentSeparator = '\\';

		public string MeterName { get; }

		public string InstrumentName { get; }

		public MetricSpec(string meterName, string instrumentName)
		{
			MeterName = meterName;
			InstrumentName = instrumentName;
		}

		public static MetricSpec Parse(string text)
		{
			int num = text.IndexOf('\\');
			if (num < 0)
			{
				return new MetricSpec(text.Trim(), null);
			}
			string meterName = text.AsSpan(0, num).Trim().ToString();
			string instrumentName = text.AsSpan(num + 1).Trim().ToString();
			return new MetricSpec(meterName, instrumentName);
		}

		public override string ToString()
		{
			if (InstrumentName == null)
			{
				return MeterName;
			}
			return MeterName + "\\" + InstrumentName;
		}
	}

	public static readonly MetricsEventSource Log = new MetricsEventSource();

	private const string SharedSessionId = "SHARED";

	private const string ClientIdKey = "ClientId";

	private const string MaxHistogramsKey = "MaxHistograms";

	private const string MaxTimeSeriesKey = "MaxTimeSeries";

	private const string RefreshIntervalKey = "RefreshInterval";

	private const string DefaultValueDescription = "default";

	private const string SharedValueDescription = "shared value";

	private CommandHandler _handler;

	private CommandHandler Handler
	{
		get
		{
			if (_handler == null)
			{
				Interlocked.CompareExchange(ref _handler, new CommandHandler(this), null);
			}
			return _handler;
		}
	}

	public static MetricsEventSource GetInstance()
	{
		return Log;
	}

	private MetricsEventSource()
	{
	}

	[Event(1, Keywords = (EventKeywords)1L)]
	public void Message(string Message)
	{
		WriteEvent(1, Message);
	}

	[Event(2, Keywords = (EventKeywords)2L)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void CollectionStart(string sessionId, DateTime intervalStartTime, DateTime intervalEndTime)
	{
		WriteEvent(2, new object[3] { sessionId, intervalStartTime, intervalEndTime });
	}

	[Event(3, Keywords = (EventKeywords)2L)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void CollectionStop(string sessionId, DateTime intervalStartTime, DateTime intervalEndTime)
	{
		WriteEvent(3, new object[3] { sessionId, intervalStartTime, intervalEndTime });
	}

	[Event(4, Keywords = (EventKeywords)2L, Version = 2)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void CounterRateValuePublished(string sessionId, string meterName, string meterVersion, string instrumentName, string unit, string tags, string rate, string value, int instrumentId)
	{
		WriteEvent(4, new object[9]
		{
			sessionId,
			meterName,
			meterVersion ?? "",
			instrumentName,
			unit ?? "",
			tags,
			rate,
			value,
			instrumentId
		});
	}

	[Event(5, Keywords = (EventKeywords)2L, Version = 2)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void GaugeValuePublished(string sessionId, string meterName, string meterVersion, string instrumentName, string unit, string tags, string lastValue, int instrumentId)
	{
		WriteEvent(5, new object[8]
		{
			sessionId,
			meterName,
			meterVersion ?? "",
			instrumentName,
			unit ?? "",
			tags,
			lastValue,
			instrumentId
		});
	}

	[Event(6, Keywords = (EventKeywords)2L, Version = 2)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void HistogramValuePublished(string sessionId, string meterName, string meterVersion, string instrumentName, string unit, string tags, string quantiles, int count, double sum, int instrumentId)
	{
		WriteEvent(6, new object[10]
		{
			sessionId,
			meterName,
			meterVersion ?? "",
			instrumentName,
			unit ?? "",
			tags,
			quantiles,
			count,
			sum,
			instrumentId
		});
	}

	[Event(7, Keywords = (EventKeywords)2L, Version = 2)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void BeginInstrumentReporting(string sessionId, string meterName, string meterVersion, string instrumentName, string instrumentType, string unit, string description, string instrumentTags, string meterTags, string meterScopeHash, int instrumentId)
	{
		WriteEvent(7, new object[11]
		{
			sessionId,
			meterName,
			meterVersion ?? "",
			instrumentName,
			instrumentType,
			unit ?? "",
			description ?? "",
			instrumentTags,
			meterTags,
			meterScopeHash,
			instrumentId
		});
	}

	[Event(8, Keywords = (EventKeywords)2L, Version = 2)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void EndInstrumentReporting(string sessionId, string meterName, string meterVersion, string instrumentName, string instrumentType, string unit, string description, string instrumentTags, string meterTags, string meterScopeHash, int instrumentId)
	{
		WriteEvent(8, new object[11]
		{
			sessionId,
			meterName,
			meterVersion ?? "",
			instrumentName,
			instrumentType,
			unit ?? "",
			description ?? "",
			instrumentTags,
			meterTags,
			meterScopeHash,
			instrumentId
		});
	}

	[Event(9, Keywords = (EventKeywords)7L)]
	public void Error(string sessionId, string errorMessage)
	{
		WriteEvent(9, sessionId, errorMessage);
	}

	[Event(10, Keywords = (EventKeywords)6L)]
	public void InitialInstrumentEnumerationComplete(string sessionId)
	{
		WriteEvent(10, sessionId);
	}

	[Event(11, Keywords = (EventKeywords)4L, Version = 2)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void InstrumentPublished(string sessionId, string meterName, string meterVersion, string instrumentName, string instrumentType, string unit, string description, string instrumentTags, string meterTags, string meterScopeHash, int instrumentId)
	{
		WriteEvent(11, new object[11]
		{
			sessionId,
			meterName,
			meterVersion ?? "",
			instrumentName,
			instrumentType,
			unit ?? "",
			description ?? "",
			instrumentTags,
			meterTags,
			meterScopeHash,
			instrumentId
		});
	}

	[Event(12, Keywords = (EventKeywords)2L)]
	public void TimeSeriesLimitReached(string sessionId)
	{
		WriteEvent(12, sessionId);
	}

	[Event(13, Keywords = (EventKeywords)2L)]
	public void HistogramLimitReached(string sessionId)
	{
		WriteEvent(13, sessionId);
	}

	[Event(14, Keywords = (EventKeywords)2L)]
	public void ObservableInstrumentCallbackError(string sessionId, string errorMessage)
	{
		WriteEvent(14, sessionId, errorMessage);
	}

	[Event(15, Keywords = (EventKeywords)7L)]
	public void MultipleSessionsNotSupportedError(string runningSessionId)
	{
		WriteEvent(15, runningSessionId);
	}

	[Event(16, Keywords = (EventKeywords)2L, Version = 2)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void UpDownCounterRateValuePublished(string sessionId, string meterName, string meterVersion, string instrumentName, string unit, string tags, string rate, string value, int instrumentId)
	{
		WriteEvent(16, new object[9]
		{
			sessionId,
			meterName,
			meterVersion ?? "",
			instrumentName,
			unit ?? "",
			tags,
			rate,
			value,
			instrumentId
		});
	}

	[Event(17, Keywords = (EventKeywords)2L)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void MultipleSessionsConfiguredIncorrectlyError(string clientId, string expectedMaxHistograms, string actualMaxHistograms, string expectedMaxTimeSeries, string actualMaxTimeSeries, string expectedRefreshInterval, string actualRefreshInterval)
	{
		WriteEvent(17, new object[7] { clientId, expectedMaxHistograms, actualMaxHistograms, expectedMaxTimeSeries, actualMaxTimeSeries, expectedRefreshInterval, actualRefreshInterval });
	}

	[Event(18, Keywords = (EventKeywords)1L)]
	public void Version(int Major, int Minor, int Patch)
	{
		WriteEvent(18, Major, Minor, Patch);
	}

	[NonEvent]
	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		if (command.Command == EventCommand.Enable)
		{
			Version(ThisAssembly.AssemblyFileVersion.Major, ThisAssembly.AssemblyFileVersion.Minor, ThisAssembly.AssemblyFileVersion.Build);
		}
		lock (this)
		{
			Handler.OnEventCommand(command);
		}
	}
}
