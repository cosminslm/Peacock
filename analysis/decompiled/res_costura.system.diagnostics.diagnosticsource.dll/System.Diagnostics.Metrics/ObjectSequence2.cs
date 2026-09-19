namespace System.Diagnostics.Metrics;

internal struct ObjectSequence2(object value1, object value2) : IEquatable<ObjectSequence2>, IObjectSequence
{
	public object Value1 = value1;

	public object Value2 = value2;

	public object this[int i]
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

	public bool Equals(ObjectSequence2 other)
	{
		if ((Value1 == null) ? (other.Value1 == null) : Value1.Equals(other.Value1))
		{
			if (Value2 != null)
			{
				return Value2.Equals(other.Value2);
			}
			return other.Value2 == null;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ObjectSequence2 other)
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
