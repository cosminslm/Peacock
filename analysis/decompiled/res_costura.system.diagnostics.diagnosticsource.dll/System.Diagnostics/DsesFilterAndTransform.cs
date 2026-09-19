using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics;

internal sealed class DsesFilterAndTransform : IDisposable
{
	private sealed class ParsedFilterAndPayloadSpecs : IDisposable
	{
		private DsesFilterAndTransform _specList;

		private DsesActivitySourceListener _activitySourceListener;

		public ParsedFilterAndPayloadSpecs(DsesFilterAndTransform specList, DsesActivitySourceListener activitySourceListener)
		{
			_specList = specList;
			_activitySourceListener = activitySourceListener;
		}

		public void Dispose()
		{
			_activitySourceListener?.Dispose();
			_activitySourceListener = null;
			DsesFilterAndTransform dsesFilterAndTransform = _specList;
			_specList = null;
			while (dsesFilterAndTransform != null)
			{
				dsesFilterAndTransform.Dispose();
				dsesFilterAndTransform = dsesFilterAndTransform.Next;
			}
		}
	}

	private sealed class ImplicitTransformEntry
	{
		public Type Type;

		public TransformSpec Transforms;
	}

	private sealed class TransformSpec
	{
		private sealed class PropertySpec
		{
			private class PropertyFetch
			{
				private sealed class RefTypedFetchProperty<TObject, TProperty> : PropertyFetch
				{
					private readonly Func<TObject, TProperty> _propertyFetch;

					public RefTypedFetchProperty(Type type, PropertyInfo property)
						: base(type)
					{
						_propertyFetch = (Func<TObject, TProperty>)property.GetMethod.CreateDelegate(typeof(Func<TObject, TProperty>));
					}

					public override object Fetch(object obj)
					{
						return _propertyFetch((TObject)obj);
					}
				}

				private delegate TProperty StructFunc<TStruct, TProperty>(ref TStruct thisArg);

				private sealed class ValueTypedFetchProperty<TStruct, TProperty> : PropertyFetch
				{
					private readonly StructFunc<TStruct, TProperty> _propertyFetch;

					public ValueTypedFetchProperty(Type type, PropertyInfo property)
						: base(type)
					{
						_propertyFetch = (StructFunc<TStruct, TProperty>)property.GetMethod.CreateDelegate(typeof(StructFunc<TStruct, TProperty>));
					}

					public override object Fetch(object obj)
					{
						TStruct thisArg = (TStruct)obj;
						return _propertyFetch(ref thisArg);
					}
				}

				private sealed class CurrentActivityPropertyFetch : PropertyFetch
				{
					public CurrentActivityPropertyFetch()
						: base(null)
					{
					}

					public override object Fetch(object obj)
					{
						return Activity.Current;
					}
				}

				private sealed class EnumeratePropertyFetch<ElementType> : PropertyFetch
				{
					public EnumeratePropertyFetch(Type type)
						: base(type)
					{
					}

					public override object Fetch(object obj)
					{
						return string.Join(",", (IEnumerable<ElementType>)obj);
					}
				}

				internal Type Type { get; }

				public PropertyFetch(Type type)
				{
					Type = type;
				}

				[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
				public static PropertyFetch FetcherForProperty(DiagnosticSourceEventSource eventSource, Type type, string propertyName)
				{
					if (propertyName == null)
					{
						return new PropertyFetch(type);
					}
					if (propertyName == "*Activity")
					{
						return new CurrentActivityPropertyFetch();
					}
					TypeInfo typeInfo = type.GetTypeInfo();
					if (propertyName == "*Enumerate")
					{
						Type[] interfaces = typeInfo.GetInterfaces();
						for (int i = 0; i < interfaces.Length; i++)
						{
							TypeInfo typeInfo2 = interfaces[i].GetTypeInfo();
							if (typeInfo2.IsGenericType && !(typeInfo2.GetGenericTypeDefinition() != typeof(IEnumerable<>)))
							{
								return CreateEnumeratePropertyFetch(type, typeInfo2);
							}
						}
						eventSource.Message($"*Enumerate applied to non-enumerable type {type}");
						return new PropertyFetch(type);
					}
					PropertyInfo propertyInfo = typeInfo.GetDeclaredProperty(propertyName);
					if (propertyInfo == null)
					{
						PropertyInfo[] properties = typeInfo.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
						foreach (PropertyInfo propertyInfo2 in properties)
						{
							if (propertyInfo2.Name == propertyName)
							{
								propertyInfo = propertyInfo2;
								break;
							}
						}
					}
					if (propertyInfo == null)
					{
						eventSource.Message($"Property {propertyName} not found on {type}. Ensure the name is spelled correctly. If you published the application with PublishTrimmed=true, ensure the property was not trimmed away.");
						return new PropertyFetch(type);
					}
					MethodInfo? getMethod = propertyInfo.GetMethod;
					if ((object)getMethod == null || !getMethod.IsStatic)
					{
						MethodInfo? setMethod = propertyInfo.SetMethod;
						if ((object)setMethod == null || !setMethod.IsStatic)
						{
							return CreatePropertyFetch(typeInfo, propertyInfo);
						}
					}
					eventSource.Message("Property " + propertyName + " is static.");
					return new PropertyFetch(type);
				}

				[UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "MakeGenericType is only called when IsDynamicCodeSupported is true or only with ref types.")]
				private static PropertyFetch CreateEnumeratePropertyFetch(Type type, TypeInfo enumerableOfTType)
				{
					Type type2 = enumerableOfTType.GetGenericArguments()[0];
					return (PropertyFetch)Activator.CreateInstance(typeof(EnumeratePropertyFetch<>).GetTypeInfo().MakeGenericType(type2), type);
				}

				[UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "MakeGenericType is only called when IsDynamicCodeSupported is true or only with ref types.")]
				private static PropertyFetch CreatePropertyFetch(Type type, PropertyInfo propertyInfo)
				{
					return (PropertyFetch)Activator.CreateInstance((type.IsValueType ? typeof(ValueTypedFetchProperty<, >) : typeof(RefTypedFetchProperty<, >)).GetTypeInfo().MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType), type, propertyInfo);
				}

				public virtual object Fetch(object obj)
				{
					return null;
				}
			}

			private const string CurrentActivityPropertyName = "*Activity";

			private const string EnumeratePropertyName = "*Enumerate";

			private readonly DiagnosticSourceEventSource _eventSource;

			private readonly string _propertyName;

			private volatile PropertyFetch _fetchForExpectedType;

			public bool IsStatic { get; }

			public PropertySpec Next { get; }

			public PropertySpec(DiagnosticSourceEventSource eventSource, string propertyName, PropertySpec next)
			{
				_eventSource = eventSource;
				_propertyName = propertyName;
				Next = next;
				if (_propertyName == "*Activity")
				{
					IsStatic = true;
				}
			}

			[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
			public object Fetch(object obj)
			{
				PropertyFetch propertyFetch = _fetchForExpectedType;
				Type type = obj?.GetType();
				if (propertyFetch == null || propertyFetch.Type != type)
				{
					propertyFetch = (_fetchForExpectedType = PropertyFetch.FetcherForProperty(_eventSource, type, _propertyName));
				}
				object result = null;
				try
				{
					result = propertyFetch.Fetch(obj);
				}
				catch (Exception arg)
				{
					_eventSource.Message($"Property {type}.{_propertyName} threw the exception {arg}");
				}
				return result;
			}
		}

		public TransformSpec Next;

		private readonly string _outputName;

		private readonly PropertySpec _fetches;

		public TransformSpec(DiagnosticSourceEventSource eventSource, string transformSpec, int startIdx, int endIdx, TransformSpec next = null)
		{
			Next = next;
			int num = transformSpec.IndexOf('=', startIdx, endIdx - startIdx);
			if (0 <= num)
			{
				_outputName = transformSpec.Substring(startIdx, num - startIdx);
				startIdx = num + 1;
			}
			while (startIdx < endIdx)
			{
				int num2 = transformSpec.LastIndexOf('.', endIdx - 1, endIdx - startIdx);
				int num3 = startIdx;
				if (0 <= num2)
				{
					num3 = num2 + 1;
				}
				string text = transformSpec.Substring(num3, endIdx - num3);
				_fetches = new PropertySpec(eventSource, text, _fetches);
				if (_outputName == null)
				{
					_outputName = text;
				}
				endIdx = num2;
			}
		}

		[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
		public KeyValuePair<string, string> Morph(object obj)
		{
			for (PropertySpec propertySpec = _fetches; propertySpec != null; propertySpec = propertySpec.Next)
			{
				if (obj != null || propertySpec.IsStatic)
				{
					obj = propertySpec.Fetch(obj);
				}
			}
			return new KeyValuePair<string, string>(_outputName, obj?.ToString());
		}
	}

	private sealed class CallbackObserver<T> : IObserver<T>
	{
		private readonly Action<T> _callback;

		public CallbackObserver(Action<T> callback)
		{
			_callback = callback;
		}

		public void OnCompleted()
		{
		}

		public void OnError(Exception error)
		{
		}

		public void OnNext(T value)
		{
			_callback(value);
		}
	}

	private sealed class Subscriptions
	{
		public IDisposable Subscription;

		public Subscriptions Next;

		public Subscriptions(IDisposable subscription, Subscriptions next)
		{
			Subscription = subscription;
			Next = next;
		}
	}

	private const string c_ActivitySourcePrefix = "[AS]";

	private const string c_ParentRatioSamplerPrefix = "ParentRatioSampler(";

	private readonly DiagnosticSourceEventSource _eventSource;

	private IDisposable _diagnosticsListenersSubscription;

	private Subscriptions _liveSubscriptions;

	private readonly bool _noImplicitTransforms;

	private ImplicitTransformEntry _firstImplicitTransformsEntry;

	private ConcurrentDictionary<Type, TransformSpec> _implicitTransformsTable;

	private readonly TransformSpec _explicitTransforms;

	public DsesFilterAndTransform Next { get; }

	internal string SourceName { get; }

	internal string ActivityName { get; }

	internal DsesActivityEvents Events { get; }

	internal DsesSampleActivityFunc SampleFunc { get; }

	public static IDisposable ParseFilterAndPayloadSpecs(DiagnosticSourceEventSource eventSource, string filterAndPayloadSpecs)
	{
		if (filterAndPayloadSpecs == null)
		{
			filterAndPayloadSpecs = "";
		}
		DsesFilterAndTransform dsesFilterAndTransform = null;
		DsesFilterAndTransform dsesFilterAndTransform2 = null;
		int num = filterAndPayloadSpecs.Length;
		while (true)
		{
			if (0 < num && char.IsWhiteSpace(filterAndPayloadSpecs[num - 1]))
			{
				num--;
				continue;
			}
			int num2 = filterAndPayloadSpecs.LastIndexOf('\n', num - 1, num);
			int i = 0;
			if (0 <= num2)
			{
				i = num2 + 1;
			}
			for (; i < num && char.IsWhiteSpace(filterAndPayloadSpecs[i]); i++)
			{
			}
			if (IsActivitySourceEntry(filterAndPayloadSpecs, i, num))
			{
				dsesFilterAndTransform2 = CreateActivitySourceTransform(eventSource, filterAndPayloadSpecs, i, num, dsesFilterAndTransform2);
			}
			else
			{
				dsesFilterAndTransform = CreateTransform(eventSource, filterAndPayloadSpecs, i, num, dsesFilterAndTransform);
			}
			num = num2;
			if (num < 0)
			{
				break;
			}
		}
		DsesActivitySourceListener activitySourceListener = ((dsesFilterAndTransform2 != null) ? DsesActivitySourceListener.Create(eventSource, dsesFilterAndTransform2) : null);
		return new ParsedFilterAndPayloadSpecs(dsesFilterAndTransform, activitySourceListener);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsActivitySourceEntry(string filterAndPayloadSpec, int startIdx, int endIdx)
	{
		return filterAndPayloadSpec.AsSpan(startIdx, endIdx - startIdx).StartsWith("[AS]".AsSpan(), StringComparison.Ordinal);
	}

	private static DsesFilterAndTransform CreateTransform(DiagnosticSourceEventSource eventSource, string filterAndPayloadSpec, int startIdx, int endIdx, DsesFilterAndTransform next)
	{
		string text = null;
		string text2 = null;
		string activityName = null;
		bool noImplicitTransforms = false;
		TransformSpec transformSpec = null;
		int num = startIdx;
		int num2 = endIdx;
		int num3 = filterAndPayloadSpec.IndexOf(':', startIdx, endIdx - startIdx);
		if (0 <= num3)
		{
			num2 = num3;
			num = num3 + 1;
		}
		int num4 = filterAndPayloadSpec.IndexOf('/', startIdx, num2 - startIdx);
		if (0 <= num4)
		{
			text = filterAndPayloadSpec.Substring(startIdx, num4 - startIdx);
			int num5 = filterAndPayloadSpec.IndexOf('@', num4 + 1, num2 - num4 - 1);
			if (0 <= num5)
			{
				activityName = filterAndPayloadSpec.Substring(num5 + 1, num2 - num5 - 1);
				text2 = filterAndPayloadSpec.Substring(num4 + 1, num5 - num4 - 1);
			}
			else
			{
				text2 = filterAndPayloadSpec.Substring(num4 + 1, num2 - num4 - 1);
			}
		}
		else if (startIdx < num2)
		{
			text = filterAndPayloadSpec.Substring(startIdx, num2 - startIdx);
		}
		eventSource.Message("DiagnosticSource: Enabling '" + (text ?? "*") + "/" + (text2 ?? "*") + "'");
		if (num < endIdx && filterAndPayloadSpec[num] == '-')
		{
			eventSource.Message("DiagnosticSource: suppressing implicit transforms.");
			noImplicitTransforms = true;
			num++;
		}
		if (num < endIdx)
		{
			while (true)
			{
				int num6 = num;
				int num7 = filterAndPayloadSpec.LastIndexOf(';', endIdx - 1, endIdx - num);
				if (0 <= num7)
				{
					num6 = num7 + 1;
				}
				if (num6 < endIdx)
				{
					if (eventSource.IsEnabled(EventLevel.Informational, (EventKeywords)1L))
					{
						eventSource.Message("DiagnosticSource: Parsing Explicit Transform '" + filterAndPayloadSpec.Substring(num6, endIdx - num6) + "'");
					}
					transformSpec = new TransformSpec(eventSource, filterAndPayloadSpec, num6, endIdx, transformSpec);
				}
				if (num == num6)
				{
					break;
				}
				endIdx = num7;
			}
		}
		DsesFilterAndTransform dsesFilterAndTransform = new DsesFilterAndTransform(eventSource, next, noImplicitTransforms, transformSpec, null, null, DsesActivityEvents.None, null);
		dsesFilterAndTransform.SetupDiagnosticListenerSubscription(text, text2, activityName);
		return dsesFilterAndTransform;
	}

	private static DsesFilterAndTransform CreateActivitySourceTransform(DiagnosticSourceEventSource eventSource, string filterAndPayloadSpec, int startIdx, int endIdx, DsesFilterAndTransform next)
	{
		bool noImplicitTransforms = false;
		TransformSpec transformSpec = null;
		DsesActivityEvents activityEvents = DsesActivityEvents.All;
		DsesSampleActivityFunc sampleFunc = delegate
		{
			return ActivitySamplingResult.AllDataAndRecorded;
		};
		int num = filterAndPayloadSpec.IndexOf(':', startIdx + "[AS]".Length, endIdx - startIdx - "[AS]".Length);
		ReadOnlySpan<char> readOnlySpan = filterAndPayloadSpec.AsSpan(startIdx + "[AS]".Length, ((num >= 0) ? num : endIdx) - startIdx - "[AS]".Length).Trim();
		int num2 = readOnlySpan.IndexOf('/');
		ReadOnlySpan<char> span;
		if (num2 >= 0)
		{
			span = readOnlySpan.Slice(0, num2).Trim();
			ReadOnlySpan<char> readOnlySpan2 = readOnlySpan.Slice(num2 + 1).Trim();
			int num3 = readOnlySpan2.IndexOf('-');
			ReadOnlySpan<char> span2;
			if (num3 >= 0)
			{
				span2 = readOnlySpan2.Slice(0, num3).Trim();
				readOnlySpan2 = readOnlySpan2.Slice(num3 + 1).Trim();
				if (readOnlySpan2.Length > 0)
				{
					if (readOnlySpan2.Equals("Propagate".AsSpan(), StringComparison.OrdinalIgnoreCase))
					{
						sampleFunc = delegate
						{
							return ActivitySamplingResult.PropagationData;
						};
					}
					else if (readOnlySpan2.Equals("Record".AsSpan(), StringComparison.OrdinalIgnoreCase))
					{
						sampleFunc = delegate
						{
							return ActivitySamplingResult.AllData;
						};
					}
					else
					{
						if (!readOnlySpan2.StartsWith("ParentRatioSampler(".AsSpan(), StringComparison.OrdinalIgnoreCase))
						{
							if (eventSource.IsEnabled(EventLevel.Warning, (EventKeywords)1L))
							{
								eventSource.Message("DiagnosticSource: Ignoring filterAndPayloadSpec '[AS]" + readOnlySpan.ToString() + "' because sampling method was invalid");
							}
							return next;
						}
						int num4 = readOnlySpan2.IndexOf(')');
						if (num4 < 0 || !double.TryParse(readOnlySpan2.Slice("ParentRatioSampler(".Length, num4 - "ParentRatioSampler(".Length).ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
						{
							if (eventSource.IsEnabled(EventLevel.Warning, (EventKeywords)1L))
							{
								eventSource.Message("DiagnosticSource: Ignoring filterAndPayloadSpec '[AS]" + readOnlySpan.ToString() + "' because sampling ratio was invalid");
							}
							return next;
						}
						sampleFunc = DsesSamplerBuilder.CreateParentRatioSampler(result);
					}
				}
			}
			else
			{
				span2 = readOnlySpan2;
			}
			if (span2.Length > 0)
			{
				if (span2.Equals("Start".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					activityEvents = DsesActivityEvents.ActivityStart;
				}
				else
				{
					if (!span2.Equals("Stop".AsSpan(), StringComparison.OrdinalIgnoreCase))
					{
						if (eventSource.IsEnabled(EventLevel.Warning, (EventKeywords)1L))
						{
							eventSource.Message("DiagnosticSource: Ignoring filterAndPayloadSpec '[AS]" + readOnlySpan.ToString() + "' because event name was invalid");
						}
						return next;
					}
					activityEvents = DsesActivityEvents.ActivityStop;
				}
			}
		}
		else
		{
			span = readOnlySpan;
		}
		string text = null;
		int num5 = span.IndexOf('+');
		if (num5 >= 0)
		{
			text = span.Slice(num5 + 1).Trim().ToString();
			span = span.Slice(0, num5).Trim();
			if (text.Length > 0 && span.Length == 1 && span[0] == '*')
			{
				if (eventSource.IsEnabled(EventLevel.Warning, (EventKeywords)1L))
				{
					eventSource.Message("DiagnosticSource: Ignoring filterAndPayloadSpec '[AS]" + readOnlySpan.ToString() + "' because activity name cannot be specified for wildcard activity sources");
				}
				return next;
			}
		}
		if (num >= 0)
		{
			int num6 = num + 1;
			if (num6 < endIdx && filterAndPayloadSpec[num6] == '-')
			{
				eventSource.Message("DiagnosticSource: suppressing implicit transforms.");
				noImplicitTransforms = true;
				num6++;
			}
			if (num6 < endIdx)
			{
				while (true)
				{
					int num7 = num6;
					int num8 = filterAndPayloadSpec.LastIndexOf(';', endIdx - 1, endIdx - num6);
					if (0 <= num8)
					{
						num7 = num8 + 1;
					}
					if (num7 < endIdx)
					{
						if (eventSource.IsEnabled(EventLevel.Informational, (EventKeywords)1L))
						{
							eventSource.Message("DiagnosticSource: Parsing Explicit Transform '" + filterAndPayloadSpec.Substring(num7, endIdx - num7) + "'");
						}
						transformSpec = new TransformSpec(eventSource, filterAndPayloadSpec, num7, endIdx, transformSpec);
					}
					if (num6 == num7)
					{
						break;
					}
					endIdx = num8;
				}
			}
		}
		return new DsesFilterAndTransform(eventSource, next, noImplicitTransforms, transformSpec, span.ToString(), text, activityEvents, sampleFunc);
	}

	private DsesFilterAndTransform(DiagnosticSourceEventSource eventSource, DsesFilterAndTransform next, bool noImplicitTransforms, TransformSpec explicitTransforms, string sourceName, string activityName, DsesActivityEvents activityEvents, DsesSampleActivityFunc sampleFunc)
	{
		_eventSource = eventSource;
		_noImplicitTransforms = noImplicitTransforms;
		_explicitTransforms = explicitTransforms;
		Next = next;
		SourceName = sourceName;
		ActivityName = activityName;
		Events = activityEvents;
		SampleFunc = sampleFunc;
	}

	public void Dispose()
	{
		if (_diagnosticsListenersSubscription != null)
		{
			_diagnosticsListenersSubscription.Dispose();
			_diagnosticsListenersSubscription = null;
		}
		if (_liveSubscriptions != null)
		{
			Subscriptions subscriptions = _liveSubscriptions;
			_liveSubscriptions = null;
			while (subscriptions != null)
			{
				subscriptions.Subscription.Dispose();
				subscriptions = subscriptions.Next;
			}
		}
	}

	[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
	public List<KeyValuePair<string, string>> Morph(object args)
	{
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		if (args != null)
		{
			if (!_noImplicitTransforms)
			{
				Type type = args.GetType();
				ImplicitTransformEntry firstImplicitTransformsEntry = _firstImplicitTransformsEntry;
				TransformSpec transformSpec;
				if (firstImplicitTransformsEntry != null && firstImplicitTransformsEntry.Type == type)
				{
					transformSpec = firstImplicitTransformsEntry.Transforms;
				}
				else if (firstImplicitTransformsEntry == null)
				{
					transformSpec = MakeImplicitTransforms(_eventSource, type);
					Interlocked.CompareExchange(ref _firstImplicitTransformsEntry, new ImplicitTransformEntry
					{
						Type = type,
						Transforms = transformSpec
					}, null);
				}
				else
				{
					if (_implicitTransformsTable == null)
					{
						Interlocked.CompareExchange(ref _implicitTransformsTable, new ConcurrentDictionary<Type, TransformSpec>(1, 8), null);
					}
					transformSpec = _implicitTransformsTable.GetOrAdd(type, (Type t) => MakeImplicitTransforms(_eventSource, t));
				}
				if (transformSpec != null)
				{
					for (TransformSpec transformSpec2 = transformSpec; transformSpec2 != null; transformSpec2 = transformSpec2.Next)
					{
						list.Add(transformSpec2.Morph(args));
					}
				}
			}
			if (_explicitTransforms != null)
			{
				for (TransformSpec transformSpec3 = _explicitTransforms; transformSpec3 != null; transformSpec3 = transformSpec3.Next)
				{
					KeyValuePair<string, string> item = transformSpec3.Morph(args);
					if (item.Value != null)
					{
						list.Add(item);
					}
				}
			}
		}
		return list;
	}

	private void SetupDiagnosticListenerSubscription(string listenerNameFilter, string eventNameFilter, string activityName)
	{
		Action<string, string, IEnumerable<KeyValuePair<string, string>>> writeEvent = null;
		if (activityName != null && activityName.Contains("Activity"))
		{
			writeEvent = activityName switch
			{
				"Activity1Start" => _eventSource.Activity1Start, 
				"Activity1Stop" => _eventSource.Activity1Stop, 
				"Activity2Start" => _eventSource.Activity2Start, 
				"Activity2Stop" => _eventSource.Activity2Stop, 
				"RecursiveActivity1Start" => _eventSource.RecursiveActivity1Start, 
				"RecursiveActivity1Stop" => _eventSource.RecursiveActivity1Stop, 
				_ => null, 
			};
			if (writeEvent == null)
			{
				_eventSource.Message("DiagnosticSource: Could not find Event to log Activity " + activityName);
			}
		}
		if (writeEvent == null)
		{
			writeEvent = _eventSource.Event;
		}
		_diagnosticsListenersSubscription = DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(delegate(DiagnosticListener newListener)
		{
			if (listenerNameFilter == null || listenerNameFilter == newListener.Name)
			{
				_eventSource.NewDiagnosticListener(newListener.Name);
				Predicate<string> isEnabled = null;
				if (eventNameFilter != null)
				{
					isEnabled = (string eventName) => eventNameFilter == eventName;
				}
				IDisposable subscription = newListener.Subscribe(new CallbackObserver<KeyValuePair<string, object>>(OnEventWritten), isEnabled);
				_liveSubscriptions = new Subscriptions(subscription, _liveSubscriptions);
			}
			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "DiagnosticSource.Write is marked with RequiresUnreferencedCode.")]
			void OnEventWritten(KeyValuePair<string, object> evnt)
			{
				if (eventNameFilter == null || !(eventNameFilter != evnt.Key))
				{
					List<KeyValuePair<string, string>> arg = Morph(evnt.Value);
					string key = evnt.Key;
					writeEvent(newListener.Name, key, arg);
				}
			}
		}));
	}

	[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
	private static TransformSpec MakeImplicitTransforms(DiagnosticSourceEventSource eventSource, Type type)
	{
		TransformSpec transformSpec = null;
		PropertyInfo[] properties = type.GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (!(propertyInfo.GetMethod == null) && propertyInfo.GetMethod.GetParameters().Length == 0)
			{
				transformSpec = new TransformSpec(eventSource, propertyInfo.Name, 0, propertyInfo.Name.Length, transformSpec);
			}
		}
		return Reverse(transformSpec);
	}

	private static TransformSpec Reverse(TransformSpec list)
	{
		TransformSpec transformSpec = null;
		while (list != null)
		{
			TransformSpec next = list.Next;
			list.Next = transformSpec;
			transformSpec = list;
			list = next;
		}
		return transformSpec;
	}
}
