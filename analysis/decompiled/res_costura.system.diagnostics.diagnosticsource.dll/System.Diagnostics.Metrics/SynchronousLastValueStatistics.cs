namespace System.Diagnostics.Metrics;

internal sealed class SynchronousLastValueStatistics : IAggregationStatistics
{
	public double LastValue { get; }

	internal SynchronousLastValueStatistics(double lastValue)
	{
		LastValue = lastValue;
	}
}
