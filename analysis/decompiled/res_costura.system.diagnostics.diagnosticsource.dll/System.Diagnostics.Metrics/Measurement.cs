using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics;

public readonly struct Measurement<T> where T : struct
{
	private readonly KeyValuePair<string, object>[] _tags;

	public ReadOnlySpan<KeyValuePair<string, object?>> Tags => _tags.AsSpan();

	public T Value { get; }

	public Measurement(T value)
	{
		_tags = Instrument.EmptyTags;
		Value = value;
	}

	public Measurement(T value, IEnumerable<KeyValuePair<string, object?>>? tags)
	{
		_tags = ToArray(tags);
		Value = value;
	}

	public Measurement(T value, params KeyValuePair<string, object?>[]? tags)
	{
		if (tags != null)
		{
			_tags = new KeyValuePair<string, object>[tags.Length];
			tags.CopyTo(_tags, 0);
		}
		else
		{
			_tags = Instrument.EmptyTags;
		}
		Value = value;
	}

	public Measurement(T value, params ReadOnlySpan<KeyValuePair<string, object?>> tags)
	{
		_tags = tags.ToArray();
		Value = value;
	}

	public Measurement(T value, in TagList tags)
	{
		if (tags.Count > 0)
		{
			_tags = new KeyValuePair<string, object>[tags.Count];
			tags.CopyTo(_tags.AsSpan());
		}
		else
		{
			_tags = Instrument.EmptyTags;
		}
		Value = value;
	}

	private static KeyValuePair<string, object>[] ToArray(IEnumerable<KeyValuePair<string, object>> tags)
	{
		if (tags == null)
		{
			return Instrument.EmptyTags;
		}
		KeyValuePair<string, object>[] array;
		if (tags is ICollection<KeyValuePair<string, object>> { Count: var count } collection)
		{
			if (count == 0)
			{
				return Instrument.EmptyTags;
			}
			array = new KeyValuePair<string, object>[count];
			collection.CopyTo(array, 0);
			return array;
		}
		KeyValuePair<string, object>[] array2 = ArrayPool<KeyValuePair<string, object>>.Shared.Rent(32);
		int num = 0;
		int length = array2.Length;
		foreach (KeyValuePair<string, object> tag in tags)
		{
			if (num == length)
			{
				Grow(ref array2, ref length);
			}
			array2[num++] = tag;
		}
		if (num == 0)
		{
			ArrayPool<KeyValuePair<string, object>>.Shared.Return(array2);
			return Instrument.EmptyTags;
		}
		array = new KeyValuePair<string, object>[num];
		Span<KeyValuePair<string, object>> span = array2.AsSpan();
		span = span.Slice(0, num);
		span.CopyTo(array.AsSpan());
		ArrayPool<KeyValuePair<string, object>>.Shared.Return(array2);
		return array;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void Grow(ref KeyValuePair<string, object>[] reference2, ref int reference)
		{
			KeyValuePair<string, object>[] array3 = ArrayPool<KeyValuePair<string, object>>.Shared.Rent(reference * 2);
			reference2.CopyTo(array3, 0);
			ArrayPool<KeyValuePair<string, object>>.Shared.Return(reference2);
			reference2 = array3;
			reference = reference2.Length;
		}
	}
}
