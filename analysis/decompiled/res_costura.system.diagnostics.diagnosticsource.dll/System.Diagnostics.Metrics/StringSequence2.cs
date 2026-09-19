namespace System.Diagnostics.Metrics;

internal struct StringSequence2(string value1, string value2) : IEquatable<StringSequence2>, IStringSequence
{
	public string Value1 = value1;

	public string Value2 = value2;

	public string this[int i]
	{
		get
		{
			return i switch
			{
				0 => Value1, 
				1 => Value2, 
				_ => throw new IndexOutOfRangeException(), 
			};
		}
		set
		{
			switch (i)
			{
			case 0:
				Value1 = value;
				break;
			case 1:
				Value2 = value;
				break;
			default:
				throw new IndexOutOfRangeException();
			}
		}
	}

	public int Length => 2;

	public bool Equals(StringSequence2 other)
	{
		if (Value1 == other.Value1)
		{
			return Value2 == other.Value2;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is StringSequence2 other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Value1?.GetHashCode() ?? 0) ^ (Value2?.GetHashCode() ?? 0);
	}
}
