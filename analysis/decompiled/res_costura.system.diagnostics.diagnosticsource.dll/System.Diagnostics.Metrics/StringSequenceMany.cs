namespace System.Diagnostics.Metrics;

internal struct StringSequenceMany(string[] values) : IEquatable<StringSequenceMany>, IStringSequence
{
	private readonly string[] _values = values;

	public string this[int i]
	{
		get
		{
			return _values[i];
		}
		set
		{
			_values[i] = value;
		}
	}

	public int Length => _values.Length;

	public Span<string> AsSpan()
	{
		return _values.AsSpan();
	}

	public bool Equals(StringSequenceMany other)
	{
		return _values.AsSpan().SequenceEqual(other._values.AsSpan());
	}

	public override bool Equals(object obj)
	{
		if (obj is StringSequenceMany other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = 0;
		for (int i = 0; i < _values.Length; i++)
		{
			num <<= 3;
			num ^= _values[i].GetHashCode();
		}
		return num;
	}
}
