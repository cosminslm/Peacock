namespace System.Diagnostics.Metrics;

internal struct ObjectSequence3(object value1, object value2, object value3) : IEquatable<ObjectSequence3>, IObjectSequence
{
	public object Value1 = value1;

	public object Value2 = value2;

	public object Value3 = value3;

	public object this[int i]
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

	public bool Equals(ObjectSequence3 other)
	{
		if (((Value1 == null) ? (other.Value1 == null) : Value1.Equals(other.Value1)) && ((Value2 == null) ? (other.Value2 == null) : Value2.Equals(other.Value2)))
		{
			if (Value3 != null)
			{
				return Value3.Equals(other.Value3);
			}
			return other.Value3 == null;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ObjectSequence3 other)
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
