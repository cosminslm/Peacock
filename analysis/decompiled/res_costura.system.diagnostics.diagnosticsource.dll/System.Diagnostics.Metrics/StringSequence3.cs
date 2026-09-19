namespace System.Diagnostics.Metrics;

internal struct StringSequence3(string value1, string value2, string value3) : IEquatable<StringSequence3>, IStringSequence
{
	public string Value1 = value1;

	public string Value2 = value2;

	public string Value3 = value3;

	public string this[int i]
	{
		get
		{
			return i switch
			{
				0 => Value1, 
				1 => Value2, 
				2 => Value3, 
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
			case 2:
				Value3 = value;
				break;
			default:
				throw new IndexOutOfRangeException();
			}
		}
	}

	public int Length => 3;

	public bool Equals(StringSequence3 other)
	{
		if (Value1 == other.Value1 && Value2 == other.Value2)
		{
			return Value3 == other.Value3;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is StringSequence3 other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Value1?.GetHashCode() ?? 0) ^ (Value2?.GetHashCode() ?? 0) ^ (Value3?.GetHashCode() ?? 0);
	}
}
