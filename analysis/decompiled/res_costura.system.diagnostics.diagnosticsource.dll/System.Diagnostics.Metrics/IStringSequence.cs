namespace System.Diagnostics.Metrics;

internal interface IStringSequence
{
	string this[int i] { get; set; }

	int Length { get; }
}
