using System.Threading;

namespace System.Diagnostics.Metrics;

internal sealed class SynchronousLastValue : Aggregator
{
	private double _lastValue;

	public override void Update(double value)
	{
		Volatile.Write(ref _lastValue, value);
	}

	public override IAggregationStatistics Collect()
	{
		return new SynchronousLastValueStatistics(Volatile.Read(in _lastValue));
	}
}
