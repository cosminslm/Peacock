using System.Threading;

namespace System.Diagnostics.Metrics;

internal sealed class ObservableCounterAggregator : Aggregator
{
	private readonly bool _isMonotonic;

	private double? _prevValue;

	private double _currValue;

	public ObservableCounterAggregator(bool isMonotonic)
	{
		_isMonotonic = isMonotonic;
	}

	public override void Update(double value)
	{
		Volatile.Write(ref _currValue, value);
	}

	public override IAggregationStatistics Collect()
	{
		double? delta = null;
		double num;
		lock (this)
		{
			num = Volatile.Read(in _currValue);
			if (_prevValue.HasValue)
			{
				delta = num - _prevValue.Value;
			}
			_prevValue = num;
		}
		return new CounterStatistics(delta, _isMonotonic, num);
	}
}
