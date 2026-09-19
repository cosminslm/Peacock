using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

public sealed class Histogram<T> : Instrument<T> where T : struct
{
	internal Histogram(Meter meter, string name, string unit, string description)
		: this(meter, name, unit, description, (IEnumerable<KeyValuePair<string, object>>)null, (InstrumentAdvice<T>)null)
	{
	}

	internal Histogram(Meter meter, string name, string unit, string description, IEnumerable<KeyValuePair<string, object>> tags, InstrumentAdvice<T> advice)
		: base(meter, name, unit, description, tags, advice)
	{
		Publish();
	}

	public void Record(T value)
	{
		RecordMeasurement(value);
	}

	public void Record(T value, KeyValuePair<string, object?> tag)
	{
		RecordMeasurement(value, tag);
	}

	public void Record(T value, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
	{
		RecordMeasurement(value, tag1, tag2);
	}

	public void Record(T value, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
	{
		RecordMeasurement(value, tag1, tag2, tag3);
	}

	public void Record(T value, params ReadOnlySpan<KeyValuePair<string, object?>> tags)
	{
		RecordMeasurement(value, tags);
	}

	public void Record(T value, params KeyValuePair<string, object?>[] tags)
	{
		RecordMeasurement(value, tags.AsSpan());
	}

	public void Record(T value, in TagList tagList)
	{
		RecordMeasurement(value, in tagList);
	}
}
