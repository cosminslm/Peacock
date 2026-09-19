namespace System.Diagnostics;

internal sealed class SynchronizedList<T>
{
	private readonly object _writeLock;

	private T[] _volatileArray;

	public int Count => _volatileArray.Length;

	public SynchronizedList()
	{
		_volatileArray = Array.Empty<T>();
		_writeLock = new object();
	}

	public void Add(T item)
	{
		lock (_writeLock)
		{
			T[] array = new T[_volatileArray.Length + 1];
			Array.Copy(_volatileArray, array, _volatileArray.Length);
			array[_volatileArray.Length] = item;
			_volatileArray = array;
		}
	}

	public bool AddIfNotExist(T item)
	{
		lock (_writeLock)
		{
			if (Array.IndexOf(_volatileArray, item) >= 0)
			{
				return false;
			}
			T[] array = new T[_volatileArray.Length + 1];
			Array.Copy(_volatileArray, array, _volatileArray.Length);
			array[_volatileArray.Length] = item;
			_volatileArray = array;
			return true;
		}
	}

	public bool Remove(T item)
	{
		lock (_writeLock)
		{
			int num = Array.IndexOf(_volatileArray, item);
			if (num < 0)
			{
				return false;
			}
			T[] array = new T[_volatileArray.Length - 1];
			Array.Copy(_volatileArray, array, num);
			Array.Copy(_volatileArray, num + 1, array, num, _volatileArray.Length - num - 1);
			_volatileArray = array;
			return true;
		}
	}

	public void EnumWithFunc<TParent>(ActivitySource.Function<T, TParent> func, ref ActivityCreationOptions<TParent> data, ref ActivitySamplingResult samplingResult, ref ActivityCreationOptions<ActivityContext> dataWithContext)
	{
		T[] volatileArray = _volatileArray;
		foreach (T item in volatileArray)
		{
			func(item, ref data, ref samplingResult, ref dataWithContext);
		}
	}

	public void EnumWithAction(Action<T, object> action, object arg)
	{
		T[] volatileArray = _volatileArray;
		foreach (T arg2 in volatileArray)
		{
			action(arg2, arg);
		}
	}

	public void EnumWithExceptionNotification(Activity activity, Exception exception, ref TagList tags)
	{
		if (!(typeof(T) != typeof(ActivityListener)))
		{
			T[] volatileArray = _volatileArray;
			for (int i = 0; i < volatileArray.Length; i++)
			{
				(volatileArray[i] as ActivityListener).ExceptionRecorder?.Invoke(activity, exception, ref tags);
			}
		}
	}
}
