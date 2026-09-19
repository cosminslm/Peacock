using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Diagnostics.Metrics;

public sealed class InstrumentAdvice<T> where T : struct
{
	private readonly ReadOnlyCollection<T> _HistogramBucketBoundaries;

	public IReadOnlyList<T>? HistogramBucketBoundaries
	{
		get
		{
			return _HistogramBucketBoundaries;
		}
		init
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			List<T> list = new List<T>(value);
			if (!IsSortedAndDistinct(list))
			{
				throw new ArgumentException(System.SR.InvalidHistogramExplicitBucketBoundaries, "value");
			}
			_HistogramBucketBoundaries = new ReadOnlyCollection<T>(list);
		}
	}

	public InstrumentAdvice()
	{
		Instrument.ValidateTypeParameter<T>();
	}

	private static bool IsSortedAndDistinct(List<T> values)
	{
		Comparer<T> comparer = Comparer<T>.Default;
		for (int i = 1; i < values.Count; i++)
		{
			if (comparer.Compare(values[i - 1], values[i]) >= 0)
			{
				return false;
			}
		}
		return true;
	}
}
