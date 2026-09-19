using System.Collections.Generic;
using System.ComponentModel;

namespace System.Diagnostics.Metrics;

public abstract class Instrument
{
	internal readonly DiagLinkedList<ListenerSubscription> _subscriptions = new DiagLinkedList<ListenerSubscription>();

	internal static KeyValuePair<string, object?>[] EmptyTags => Array.Empty<KeyValuePair<string, object>>();

	internal static object SyncObject { get; } = new object();

	public Meter Meter { get; }

	public string Name { get; }

	public string? Description { get; }

	public string? Unit { get; }

	public IEnumerable<KeyValuePair<string, object?>>? Tags { get; }

	public bool Enabled => _subscriptions.First != null;

	public virtual bool IsObservable => false;

	protected Instrument(Meter meter, string name)
		: this(meter, name, null, null, null)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected Instrument(Meter meter, string name, string? unit, string? description)
		: this(meter, name, unit, description, null)
	{
	}

	protected Instrument(Meter meter, string name, string? unit = null, string? description = null, IEnumerable<KeyValuePair<string, object?>>? tags = null)
	{
		Meter = meter ?? throw new ArgumentNullException("meter");
		Name = name ?? throw new ArgumentNullException("name");
		Description = description;
		Unit = unit;
		if (tags != null)
		{
			List<KeyValuePair<string, object>> list = new List<KeyValuePair<string, object>>(tags);
			list.Sort((KeyValuePair<string, object> left, KeyValuePair<string, object> right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
			Tags = list;
		}
	}

	protected void Publish()
	{
		if (!System.Diagnostics.Metrics.Meter.IsSupported)
		{
			return;
		}
		List<MeterListener> list = null;
		lock (SyncObject)
		{
			if (Meter.Disposed || !Meter.AddInstrument(this))
			{
				return;
			}
			list = MeterListener.GetAllListeners();
		}
		if (list == null)
		{
			return;
		}
		foreach (MeterListener item in list)
		{
			item.InstrumentPublished?.Invoke(this, item);
		}
	}

	internal void NotifyForUnpublishedInstrument()
	{
		for (DiagNode<ListenerSubscription> diagNode = _subscriptions.First; diagNode != null; diagNode = diagNode.Next)
		{
			diagNode.Value.Listener.DisableMeasurementEvents(this);
		}
		_subscriptions.Clear();
	}

	internal static void ValidateTypeParameter<T>()
	{
		Type typeFromHandle = typeof(T);
		if (typeFromHandle != typeof(byte) && typeFromHandle != typeof(short) && typeFromHandle != typeof(int) && typeFromHandle != typeof(long) && typeFromHandle != typeof(double) && typeFromHandle != typeof(float) && typeFromHandle != typeof(decimal))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.UnsupportedType, typeFromHandle));
		}
	}

	internal object EnableMeasurement(ListenerSubscription subscription, out bool oldStateStored)
	{
		oldStateStored = false;
		if (!_subscriptions.AddIfNotExist(subscription, (ListenerSubscription s1, ListenerSubscription s2) => s1.Listener == s2.Listener))
		{
			ListenerSubscription listenerSubscription = _subscriptions.Remove(subscription, (ListenerSubscription s1, ListenerSubscription s2) => s1.Listener == s2.Listener);
			_subscriptions.AddIfNotExist(subscription, (ListenerSubscription s1, ListenerSubscription s2) => s1.Listener == s2.Listener);
			oldStateStored = listenerSubscription.Listener == subscription.Listener;
			return listenerSubscription.State;
		}
		return false;
	}

	internal object DisableMeasurements(MeterListener listener)
	{
		return _subscriptions.Remove(new ListenerSubscription(listener), (ListenerSubscription s1, ListenerSubscription s2) => s1.Listener == s2.Listener).State;
	}

	internal virtual void Observe(MeterListener listener)
	{
		throw new InvalidOperationException();
	}

	internal object GetSubscriptionState(MeterListener listener)
	{
		for (DiagNode<ListenerSubscription> diagNode = _subscriptions.First; diagNode != null; diagNode = diagNode.Next)
		{
			if (listener == diagNode.Value.Listener)
			{
				return diagNode.Value.State;
			}
		}
		return null;
	}
}
[DebuggerDisplay("Name = {Name}, Meter = {Meter.Name}")]
public abstract class Instrument<T> : Instrument where T : struct
{
	[ThreadStatic]
	private static KeyValuePair<string, object>[] ts_tags;

	private const int MaxTagsCount = 8;

	public InstrumentAdvice<T>? Advice { get; }

	protected Instrument(Meter meter, string name)
		: this(meter, name, (string?)null, (string?)null, (IEnumerable<KeyValuePair<string, object?>>?)null, (InstrumentAdvice<T>?)null)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected Instrument(Meter meter, string name, string? unit, string? description)
		: this(meter, name, unit, description, (IEnumerable<KeyValuePair<string, object?>>?)null, (InstrumentAdvice<T>?)null)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected Instrument(Meter meter, string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags)
		: this(meter, name, unit, description, tags, (InstrumentAdvice<T>?)null)
	{
	}

	protected Instrument(Meter meter, string name, string? unit = null, string? description = null, IEnumerable<KeyValuePair<string, object?>>? tags = null, InstrumentAdvice<T>? advice = null)
		: base(meter, name, unit, description, tags)
	{
		Advice = advice;
		Instrument.ValidateTypeParameter<T>();
	}

	protected void RecordMeasurement(T measurement)
	{
		RecordMeasurement(measurement, Instrument.EmptyTags.AsSpan());
	}

	protected void RecordMeasurement(T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags)
	{
		for (DiagNode<ListenerSubscription> diagNode = _subscriptions.First; diagNode != null; diagNode = diagNode.Next)
		{
			diagNode.Value.Listener.NotifyMeasurement(this, measurement, tags, diagNode.Value.State);
		}
	}

	protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag)
	{
		KeyValuePair<string, object>[] array = ts_tags ?? new KeyValuePair<string, object>[8];
		ts_tags = null;
		array[0] = tag;
		RecordMeasurement(measurement, array.AsSpan(0, 1));
		ts_tags = array;
	}

	protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
	{
		KeyValuePair<string, object>[] array = ts_tags ?? new KeyValuePair<string, object>[8];
		ts_tags = null;
		array[0] = tag1;
		array[1] = tag2;
		RecordMeasurement(measurement, array.AsSpan(0, 2));
		ts_tags = array;
	}

	protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
	{
		KeyValuePair<string, object>[] array = ts_tags ?? new KeyValuePair<string, object>[8];
		ts_tags = null;
		array[0] = tag1;
		array[1] = tag2;
		array[2] = tag3;
		RecordMeasurement(measurement, array.AsSpan(0, 3));
		ts_tags = array;
	}

	protected void RecordMeasurement(T measurement, in TagList tagList)
	{
		KeyValuePair<string, object>[] tags = tagList.Tags;
		if (tags != null)
		{
			RecordMeasurement(measurement, tags.AsSpan(0, tagList.Count));
			return;
		}
		tags = ts_tags ?? new KeyValuePair<string, object>[8];
		switch (tagList.Count)
		{
		default:
			return;
		case 8:
			tags[7] = tagList.Tag8;
			goto case 7;
		case 7:
			tags[6] = tagList.Tag7;
			goto case 6;
		case 6:
			tags[5] = tagList.Tag6;
			goto case 5;
		case 5:
			tags[4] = tagList.Tag5;
			goto case 4;
		case 4:
			tags[3] = tagList.Tag4;
			goto case 3;
		case 3:
			tags[2] = tagList.Tag3;
			goto case 2;
		case 2:
			tags[1] = tagList.Tag2;
			goto case 1;
		case 1:
			tags[0] = tagList.Tag1;
			break;
		case 0:
			break;
		}
		ts_tags = null;
		RecordMeasurement(measurement, tags.AsSpan(0, tagList.Count));
		ts_tags = tags;
	}
}
