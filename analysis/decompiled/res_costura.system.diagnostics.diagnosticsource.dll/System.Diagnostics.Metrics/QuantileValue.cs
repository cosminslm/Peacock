namespace System.Diagnostics.Metrics;

internal readonly struct QuantileValue(double quantile, double value)
{
	public double Quantile { get; } = quantile;

	public double Value { get; } = value;
}
