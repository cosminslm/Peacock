using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace System.Diagnostics;

internal sealed class DsesActivitySourceListener : IDisposable
{
	private readonly struct SpecLookupKey(string activitySourceName, string activityName)
	{
		public readonly string activitySourceName = activitySourceName;

		public readonly string activityName = activityName;
	}

	private sealed class SpecLookupKeyComparer : IEqualityComparer<SpecLookupKey>
	{
		public static readonly SpecLookupKeyComparer Instance = new SpecLookupKeyComparer();

		public bool Equals(SpecLookupKey x, SpecLookupKey y)
		{
			if (string.Equals(x.activitySourceName, y.activitySourceName, StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(x.activityName, y.activityName, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		public int GetHashCode(SpecLookupKey obj)
		{
			int num = 5381;
			num = (num << 5) + num + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.activitySourceName);
			return (num << 5) + num + ((obj.activityName != null) ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.activityName) : 0);
		}
	}

	private readonly DiagnosticSourceEventSource _eventSource;

	private DsesFilterAndTransform _wildcardSpec;

	private Dictionary<SpecLookupKey, DsesFilterAndTransform> _specsBySourceNameAndActivityName;

	private HashSet<string> _listenToActivitySourceNames;

	private bool _hasActivityNameSpecDefined;

	private ActivityListener _activityListener;

	public static DsesActivitySourceListener Create(DiagnosticSourceEventSource eventSource, DsesFilterAndTransform activitySourceSpecs)
	{
		DsesActivitySourceListener dsesActivitySourceListener = new DsesActivitySourceListener(eventSource);
		dsesActivitySourceListener.NormalizeActivitySourceSpecsList(activitySourceSpecs);
		dsesActivitySourceListener.CreateActivityListener();
		return dsesActivitySourceListener;
	}

	private DsesActivitySourceListener(DiagnosticSourceEventSource eventSource)
	{
		_eventSource = eventSource;
	}

	public void Dispose()
	{
		_activityListener?.Dispose();
		_activityListener = null;
		_wildcardSpec = null;
		_specsBySourceNameAndActivityName = null;
		_listenToActivitySourceNames = null;
	}

	private void NormalizeActivitySourceSpecsList(DsesFilterAndTransform activitySourceSpecs)
	{
		while (activitySourceSpecs != null)
		{
			DsesFilterAndTransform dsesFilterAndTransform = activitySourceSpecs;
			activitySourceSpecs = activitySourceSpecs.Next;
			if (dsesFilterAndTransform.SourceName == "*")
			{
				if (_wildcardSpec != null)
				{
					if (_eventSource.IsEnabled(EventLevel.Warning, (EventKeywords)1L))
					{
						_eventSource.Message("DiagnosticSource: Ignoring wildcard activity source filterAndPayloadSpec rule because a previous rule was defined");
					}
				}
				else
				{
					_wildcardSpec = dsesFilterAndTransform;
				}
				continue;
			}
			Dictionary<SpecLookupKey, DsesFilterAndTransform> dictionary = _specsBySourceNameAndActivityName ?? (_specsBySourceNameAndActivityName = new Dictionary<SpecLookupKey, DsesFilterAndTransform>(SpecLookupKeyComparer.Instance));
			HashSet<string> hashSet = _listenToActivitySourceNames ?? (_listenToActivitySourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase));
			SpecLookupKey key = new SpecLookupKey(dsesFilterAndTransform.SourceName, dsesFilterAndTransform.ActivityName);
			if (dictionary.ContainsKey(key))
			{
				LogIgnoredSpecRule(dsesFilterAndTransform.SourceName, dsesFilterAndTransform.ActivityName);
				continue;
			}
			dictionary[key] = dsesFilterAndTransform;
			hashSet.Add(key.activitySourceName);
			if (key.activityName != null)
			{
				_hasActivityNameSpecDefined = true;
			}
		}
		void LogIgnoredSpecRule(string activitySourceName, string activityName)
		{
			if (_eventSource.IsEnabled(EventLevel.Warning, (EventKeywords)1L))
			{
				if (activityName == null)
				{
					_eventSource.Message("DiagnosticSource: Ignoring filterAndPayloadSpec rule for '[AS]" + activitySourceName + "' because a previous rule was defined");
				}
				else
				{
					_eventSource.Message("DiagnosticSource: Ignoring filterAndPayloadSpec rule for '[AS]" + activitySourceName + "+" + activityName + "' because a previous rule was defined");
				}
			}
		}
	}

	private void CreateActivityListener()
	{
		_activityListener = new ActivityListener();
		_activityListener.SampleUsingParentId = OnSampleUsingParentId;
		_activityListener.Sample = OnSample;
		_activityListener.ShouldListenTo = (ActivitySource activitySource) => _wildcardSpec != null || (_listenToActivitySourceNames != null && _listenToActivitySourceNames.Contains(activitySource.Name));
		_activityListener.ActivityStarted = OnActivityStarted;
		_activityListener.ActivityStopped = OnActivityStopped;
		ActivitySource.AddActivityListener(_activityListener);
	}

	private bool TryFindSpecForActivity(string activitySourceName, string activityName, [NotNullWhen(true)] out DsesFilterAndTransform spec)
	{
		if (_specsBySourceNameAndActivityName != null)
		{
			if (_hasActivityNameSpecDefined && _specsBySourceNameAndActivityName.TryGetValue(new SpecLookupKey(activitySourceName, activityName), out spec))
			{
				return true;
			}
			if (_specsBySourceNameAndActivityName.TryGetValue(new SpecLookupKey(activitySourceName, null), out spec))
			{
				return true;
			}
		}
		return (spec = _wildcardSpec) != null;
	}

	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Activity))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityContext))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityEvent))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityLink))]
	[DynamicDependency("Ticks", typeof(DateTime))]
	[DynamicDependency("Ticks", typeof(TimeSpan))]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Activity's properties are being preserved with the DynamicDependencies on OnActivityStarted.")]
	private void OnActivityStarted(Activity activity)
	{
		if (TryFindSpecForActivity(activity.Source.Name, activity.OperationName, out var spec) && (spec.Events & DsesActivityEvents.ActivityStart) != DsesActivityEvents.None)
		{
			_eventSource.ActivityStart(activity.Source.Name, activity.OperationName, spec.Morph(activity));
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Activity's properties are being preserved with the DynamicDependencies on OnActivityStarted.")]
	private void OnActivityStopped(Activity activity)
	{
		if (TryFindSpecForActivity(activity.Source.Name, activity.OperationName, out var spec) && (spec.Events & DsesActivityEvents.ActivityStop) != DsesActivityEvents.None)
		{
			_eventSource.ActivityStop(activity.Source.Name, activity.OperationName, spec.Morph(activity));
		}
	}

	private ActivitySamplingResult OnSampleUsingParentId(ref ActivityCreationOptions<string> options)
	{
		ActivityCreationOptions<ActivityContext> options2 = default(ActivityCreationOptions<ActivityContext>);
		return OnSample(options.Source.Name, options.Name, hasActivityContext: false, ref options2);
	}

	private ActivitySamplingResult OnSample(ref ActivityCreationOptions<ActivityContext> options)
	{
		return OnSample(options.Source.Name, options.Name, hasActivityContext: true, ref options);
	}

	private ActivitySamplingResult OnSample(string activitySourceName, string activityName, bool hasActivityContext, ref ActivityCreationOptions<ActivityContext> options)
	{
		if (!TryFindSpecForActivity(activitySourceName, activityName, out var spec))
		{
			return ActivitySamplingResult.None;
		}
		return spec.SampleFunc(hasActivityContext, ref options);
	}
}
