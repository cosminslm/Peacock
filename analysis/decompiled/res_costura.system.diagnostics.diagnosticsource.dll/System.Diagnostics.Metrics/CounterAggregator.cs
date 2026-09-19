using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics.Metrics;

internal sealed class CounterAggregator : Aggregator
{
	[StructLayout(LayoutKind.Explicit, Size = 64)]
	private struct PaddedDouble
	{
		[FieldOffset(0)]
		public double Value;
	}

	private readonly bool _isMonotonic;

	private double _aggregatedValue;

	private readonly PaddedDouble[] _deltas = new PaddedDouble[Math.Min(Environment.ProcessorCount, 8)];

	public CounterAggregator(bool isMonotonic)
	{
		_isMonotonic = isMonotonic;
	}

	public override void Update(double value)
	{
		PaddedDouble[] deltas = _deltas;
		ref PaddedDouble reference = ref deltas[Environment.CurrentManagedThreadId % deltas.Length];
		double value2;
		do
		{
			value2 = reference.Value;
		}
		while (Interlocked.CompareExchange(ref reference.Value, value2 + value, value2) != value2);
	}

	public override IAggregationStatistics Collect()
	{
		double num;
		double aggregatedValue;
		lock (this)
		{
			num = 0.0;
			Span<PaddedDouble> span = _deltas.AsSpan();
			for (int i = 0; i < span.Length; i++)
			{
				num += Interlocked.Exchange(ref span[i].Value, 0.0);
			}
			_aggregatedValue += num;
			aggregatedValue = _aggregatedValue;
		}
		return new CounterStatistics(num, _isMonotonic, aggregatedValue);
	}
}
