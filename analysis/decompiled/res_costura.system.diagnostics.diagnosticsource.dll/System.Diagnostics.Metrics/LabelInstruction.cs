namespace System.Diagnostics.Metrics;

internal readonly struct LabelInstruction(int sourceIndex, string labelName)
{
	public int SourceIndex { get; } = sourceIndex;

	public string LabelName { get; } = labelName;
}
