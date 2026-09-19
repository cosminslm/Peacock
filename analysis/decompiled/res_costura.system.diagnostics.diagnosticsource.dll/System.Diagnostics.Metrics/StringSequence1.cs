namespace System.Diagnostics.Metrics;

internal struct StringSequence1(string value1) : IEquatable<StringSequence1>, IStringSequence
{
	public string Value1 = value1;

	public string this[int i]
	{
		get
		{
			if (i == 0)
			{
				return Value1;
			}
			throw new IndexOutOfRangeException();
		}
		set
		{
			if (i == 0)
			{
				Value1 = value;
				return;
			}
			throw new IndexOutOfRangeException();
		}
	}

	public int Length => 1;

	public override int GetHashCode()
	{
		return Value1.GetHashCode();
	}

	public bool Equals(StringSequence1 other)
	{
		return Value1 == other.Value1;
	}

	public override bool Equals(object obj)
	{
		if (obj is StringSequence1 other)
		{
			return Equals(other);
		}
		return false;
	}
}
