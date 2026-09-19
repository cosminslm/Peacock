using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace System.Diagnostics;

internal sealed class HttpHandlerDiagnosticListener : DiagnosticListener
{
	private class HashtableWrapper : Hashtable, IEnumerable
	{
		protected Hashtable _table;

		public override int Count => _table.Count;

		public override bool IsReadOnly => _table.IsReadOnly;

		public override bool IsFixedSize => _table.IsFixedSize;

		public override bool IsSynchronized => _table.IsSynchronized;

		public override object this[object key]
		{
			get
			{
				return _table[key];
			}
			set
			{
				_table[key] = value;
			}
		}

		public override object SyncRoot => _table.SyncRoot;

		public override ICollection Keys => _table.Keys;

		public override ICollection Values => _table.Values;

		internal HashtableWrapper(Hashtable table)
		{
			_table = table;
		}

		public override void Add(object key, object value)
		{
			_table.Add(key, value);
		}

		public override void Clear()
		{
			_table.Clear();
		}

		public override bool Contains(object key)
		{
			return _table.Contains(key);
		}

		public override bool ContainsKey(object key)
		{
			return _table.ContainsKey(key);
		}

		public override bool ContainsValue(object key)
		{
			return _table.ContainsValue(key);
		}

		public override void CopyTo(Array array, int arrayIndex)
		{
			_table.CopyTo(array, arrayIndex);
		}

		public override object Clone()
		{
			return new HashtableWrapper((Hashtable)_table.Clone());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _table.GetEnumerator();
		}

		public override IDictionaryEnumerator GetEnumerator()
		{
			return _table.GetEnumerator();
		}

		public override void Remove(object key)
		{
			_table.Remove(key);
		}
	}

	private sealed class ServicePointHashtable : HashtableWrapper
	{
		public override object this[object key]
		{
			get
			{
				return base[key];
			}
			set
			{
				if (value is WeakReference { IsAlive: not false, Target: ServicePoint target })
				{
					ConnectionGroupHashtable value2 = new ConnectionGroupHashtable((s_connectionGroupListField.GetValue(target) as Hashtable) ?? new Hashtable());
					s_connectionGroupListField.SetValue(target, value2);
				}
				base[key] = value;
			}
		}

		public ServicePointHashtable(Hashtable table)
			: base(table)
		{
		}
	}

	private sealed class ConnectionGroupHashtable : HashtableWrapper
	{
		public override object this[object key]
		{
			get
			{
				return base[key];
			}
			set
			{
				if (s_connectionGroupType.IsInstanceOfType(value))
				{
					ConnectionArrayList value2 = new ConnectionArrayList((s_connectionListField.GetValue(value) as ArrayList) ?? new ArrayList());
					s_connectionListField.SetValue(value, value2);
				}
				base[key] = value;
			}
		}

		public ConnectionGroupHashtable(Hashtable table)
			: base(table)
		{
		}
	}

	private class ArrayListWrapper : ArrayList
	{
		private ArrayList _list;

		public override int Capacity
		{
			get
			{
				return _list.Capacity;
			}
			set
			{
				_list.Capacity = value;
			}
		}

		public override int Count => _list.Count;

		public override bool IsReadOnly => _list.IsReadOnly;

		public override bool IsFixedSize => _list.IsFixedSize;

		public override bool IsSynchronized => _list.IsSynchronized;

		public override object this[int index]
		{
			get
			{
				return _list[index];
			}
			set
			{
				_list[index] = value;
			}
		}

		public override object SyncRoot => _list.SyncRoot;

		internal ArrayListWrapper(ArrayList list)
		{
			_list = list;
		}

		public override int Add(object value)
		{
			return _list.Add(value);
		}

		public override void AddRange(ICollection c)
		{
			_list.AddRange(c);
		}

		public override int BinarySearch(object value)
		{
			return _list.BinarySearch(value);
		}

		public override int BinarySearch(object value, IComparer comparer)
		{
			return _list.BinarySearch(value, comparer);
		}

		public override int BinarySearch(int index, int count, object value, IComparer comparer)
		{
			return _list.BinarySearch(index, count, value, comparer);
		}

		public override void Clear()
		{
			_list.Clear();
		}

		public override object Clone()
		{
			return new ArrayListWrapper((ArrayList)_list.Clone());
		}

		public override bool Contains(object item)
		{
			return _list.Contains(item);
		}

		public override void CopyTo(Array array)
		{
			_list.CopyTo(array);
		}

		public override void CopyTo(Array array, int index)
		{
			_list.CopyTo(array, index);
		}

		public override void CopyTo(int index, Array array, int arrayIndex, int count)
		{
			_list.CopyTo(index, array, arrayIndex, count);
		}

		public override IEnumerator GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		public override IEnumerator GetEnumerator(int index, int count)
		{
			return _list.GetEnumerator(index, count);
		}

		public override int IndexOf(object value)
		{
			return _list.IndexOf(value);
		}

		public override int IndexOf(object value, int startIndex)
		{
			return _list.IndexOf(value, startIndex);
		}

		public override int IndexOf(object value, int startIndex, int count)
		{
			return _list.IndexOf(value, startIndex, count);
		}

		public override void Insert(int index, object value)
		{
			_list.Insert(index, value);
		}

		public override void InsertRange(int index, ICollection c)
		{
			_list.InsertRange(index, c);
		}

		public override int LastIndexOf(object value)
		{
			return _list.LastIndexOf(value);
		}

		public override int LastIndexOf(object value, int startIndex)
		{
			return _list.LastIndexOf(value, startIndex);
		}

		public override int LastIndexOf(object value, int startIndex, int count)
		{
			return _list.LastIndexOf(value, startIndex, count);
		}

		public override void Remove(object value)
		{
			_list.Remove(value);
		}

		public override void RemoveAt(int index)
		{
			_list.RemoveAt(index);
		}

		public override void RemoveRange(int index, int count)
		{
			_list.RemoveRange(index, count);
		}

		public override void Reverse(int index, int count)
		{
			_list.Reverse(index, count);
		}

		public override void SetRange(int index, ICollection c)
		{
			_list.SetRange(index, c);
		}

		public override ArrayList GetRange(int index, int count)
		{
			return _list.GetRange(index, count);
		}

		public override void Sort()
		{
			_list.Sort();
		}

		public override void Sort(IComparer comparer)
		{
			_list.Sort(comparer);
		}

		public override void Sort(int index, int count, IComparer comparer)
		{
			_list.Sort(index, count, comparer);
		}

		public override object[] ToArray()
		{
			return _list.ToArray();
		}

		public override Array ToArray(Type type)
		{
			return _list.ToArray(type);
		}

		public override void TrimToSize()
		{
			_list.TrimToSize();
		}
	}

	private sealed class ConnectionArrayList : ArrayListWrapper
	{
		public ConnectionArrayList(ArrayList list)
			: base(list)
		{
		}

		public override int Add(object value)
		{
			if (s_connectionType.IsInstanceOfType(value))
			{
				HttpWebRequestArrayList value2 = new HttpWebRequestArrayList((s_writeListField.GetValue(value) as ArrayList) ?? new ArrayList());
				s_writeListField.SetValue(value, value2);
			}
			return base.Add(value);
		}
	}

	private sealed class HttpWebRequestArrayList : ArrayListWrapper
	{
		public HttpWebRequestArrayList(ArrayList list)
			: base(list)
		{
		}

		public override int Add(object value)
		{
			if (value is HttpWebRequest request)
			{
				s_instance.RaiseRequestEvent(request);
			}
			return base.Add(value);
		}

		public override void RemoveAt(int index)
		{
			if (base[index] is HttpWebRequest httpWebRequest)
			{
				HttpWebResponse httpWebResponse = s_httpResponseAccessor(httpWebRequest);
				if (httpWebResponse != null)
				{
					s_instance.RaiseResponseEvent(httpWebRequest, httpWebResponse);
				}
				else
				{
					object obj = s_coreResponseAccessor(httpWebRequest);
					if (obj != null && s_coreResponseDataType.IsInstanceOfType(obj))
					{
						HttpStatusCode statusCode = s_coreStatusCodeAccessor(obj);
						WebHeaderCollection headers = s_coreHeadersAccessor(obj);
						s_instance.RaiseResponseEvent(httpWebRequest, statusCode, headers);
					}
				}
			}
			base.RemoveAt(index);
		}
	}

	internal static readonly HttpHandlerDiagnosticListener s_instance = new HttpHandlerDiagnosticListener();

	private const string DiagnosticListenerName = "System.Net.Http.Desktop";

	private const string ActivityName = "System.Net.Http.Desktop.HttpRequestOut";

	private const string RequestStartName = "System.Net.Http.Desktop.HttpRequestOut.Start";

	private const string RequestStopName = "System.Net.Http.Desktop.HttpRequestOut.Stop";

	private const string RequestStopExName = "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop";

	private const string InitializationFailed = "System.Net.Http.InitializationFailed";

	private const string RequestIdHeaderName = "Request-Id";

	private const string CorrelationContextHeaderName = "Correlation-Context";

	private const string TraceParentHeaderName = "traceparent";

	private const string TraceStateHeaderName = "tracestate";

	private bool initialized;

	private static FieldInfo s_connectionGroupListField;

	private static Type s_connectionGroupType;

	private static FieldInfo s_connectionListField;

	private static Type s_connectionType;

	private static FieldInfo s_writeListField;

	private static Func<HttpWebRequest, HttpWebResponse> s_httpResponseAccessor;

	private static Func<HttpWebRequest, int> s_autoRedirectsAccessor;

	private static Func<HttpWebRequest, object> s_coreResponseAccessor;

	private static Func<object, HttpStatusCode> s_coreStatusCodeAccessor;

	private static Func<object, WebHeaderCollection> s_coreHeadersAccessor;

	private static Type s_coreResponseDataType;

	public override IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer, Predicate<string> isEnabled)
	{
		IDisposable result = base.Subscribe(observer, isEnabled);
		Initialize();
		return result;
	}

	public override IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer, Func<string, object, object, bool> isEnabled)
	{
		IDisposable result = base.Subscribe(observer, isEnabled);
		Initialize();
		return result;
	}

	public override IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer)
	{
		IDisposable result = base.Subscribe(observer);
		Initialize();
		return result;
	}

	private void Initialize()
	{
		lock (this)
		{
			if (!initialized)
			{
				try
				{
					initialized = true;
					PrepareReflectionObjects();
					PerformInjection();
					return;
				}
				catch (Exception exception)
				{
					Write("System.Net.Http.InitializationFailed", new
					{
						Exception = exception
					});
					return;
				}
			}
		}
	}

	private HttpHandlerDiagnosticListener()
		: base("System.Net.Http.Desktop")
	{
	}

	private void RaiseRequestEvent(HttpWebRequest request)
	{
		if (request.Headers.Get("Request-Id") != null || !IsEnabled("System.Net.Http.Desktop.HttpRequestOut", request))
		{
			return;
		}
		Activity activity = new Activity("System.Net.Http.Desktop.HttpRequestOut");
		if (IsEnabled("System.Net.Http.Desktop.HttpRequestOut.Start"))
		{
			StartActivity(activity, new
			{
				Request = request
			});
		}
		else
		{
			activity.Start();
		}
		if (activity.IdFormat == ActivityIdFormat.W3C)
		{
			if (request.Headers.Get("traceparent") == null)
			{
				request.Headers.Add("traceparent", activity.Id);
				string traceStateString = activity.TraceStateString;
				if (traceStateString != null)
				{
					request.Headers.Add("tracestate", traceStateString);
				}
			}
		}
		else if (request.Headers.Get("Request-Id") == null)
		{
			request.Headers.Add("Request-Id", activity.Id);
		}
		if (request.Headers.Get("Correlation-Context") == null)
		{
			using IEnumerator<KeyValuePair<string, string>> enumerator = activity.Baggage.GetEnumerator();
			if (enumerator.MoveNext())
			{
				StringBuilder stringBuilder = new StringBuilder();
				do
				{
					KeyValuePair<string, string> current = enumerator.Current;
					stringBuilder.Append(WebUtility.UrlEncode(current.Key)).Append('=').Append(WebUtility.UrlEncode(current.Value))
						.Append(',');
				}
				while (enumerator.MoveNext());
				stringBuilder.Remove(stringBuilder.Length - 1, 1);
				request.Headers.Add("Correlation-Context", stringBuilder.ToString());
			}
		}
		activity.Stop();
	}

	private void RaiseResponseEvent(HttpWebRequest request, HttpWebResponse response)
	{
		if ((request.Headers.Get("traceparent") != null || request.Headers.Get("Request-Id") != null) && IsLastResponse(request, response.StatusCode))
		{
			Write("System.Net.Http.Desktop.HttpRequestOut.Stop", new
			{
				Request = request,
				Response = response
			});
		}
	}

	private void RaiseResponseEvent(HttpWebRequest request, HttpStatusCode statusCode, WebHeaderCollection headers)
	{
		if (request.Headers.Get("Request-Id") != null && IsLastResponse(request, statusCode))
		{
			Write("System.Net.Http.Desktop.HttpRequestOut.Ex.Stop", new
			{
				Request = request,
				StatusCode = statusCode,
				Headers = headers
			});
		}
	}

	private static bool IsLastResponse(HttpWebRequest request, HttpStatusCode statusCode)
	{
		if (request.AllowAutoRedirect && (statusCode == HttpStatusCode.MultipleChoices || statusCode == HttpStatusCode.MovedPermanently || statusCode == HttpStatusCode.Found || statusCode == HttpStatusCode.SeeOther || statusCode == HttpStatusCode.TemporaryRedirect || statusCode == HttpStatusCode.PermanentRedirect))
		{
			return s_autoRedirectsAccessor(request) >= request.MaximumAutomaticRedirections;
		}
		return true;
	}

	private static void PrepareReflectionObjects()
	{
		Assembly assembly = typeof(ServicePoint).Assembly;
		s_connectionGroupListField = typeof(ServicePoint).GetField("m_ConnectionGroupList", BindingFlags.Instance | BindingFlags.NonPublic);
		s_connectionGroupType = assembly?.GetType("System.Net.ConnectionGroup");
		s_connectionListField = s_connectionGroupType?.GetField("m_ConnectionList", BindingFlags.Instance | BindingFlags.NonPublic);
		s_connectionType = assembly?.GetType("System.Net.Connection");
		s_writeListField = s_connectionType?.GetField("m_WriteList", BindingFlags.Instance | BindingFlags.NonPublic);
		s_httpResponseAccessor = CreateFieldGetter<HttpWebRequest, HttpWebResponse>("_HttpResponse", BindingFlags.Instance | BindingFlags.NonPublic);
		s_autoRedirectsAccessor = CreateFieldGetter<HttpWebRequest, int>("_AutoRedirects", BindingFlags.Instance | BindingFlags.NonPublic);
		s_coreResponseAccessor = CreateFieldGetter<HttpWebRequest, object>("_CoreResponse", BindingFlags.Instance | BindingFlags.NonPublic);
		s_coreResponseDataType = assembly?.GetType("System.Net.CoreResponseData");
		if (s_coreResponseDataType != null)
		{
			s_coreStatusCodeAccessor = CreateFieldGetter<HttpStatusCode>(s_coreResponseDataType, "m_StatusCode", BindingFlags.Instance | BindingFlags.Public);
			s_coreHeadersAccessor = CreateFieldGetter<WebHeaderCollection>(s_coreResponseDataType, "m_ResponseHeaders", BindingFlags.Instance | BindingFlags.Public);
		}
		if (s_connectionGroupListField == null || s_connectionGroupType == null || s_connectionListField == null || s_connectionType == null || s_writeListField == null || s_httpResponseAccessor == null || s_autoRedirectsAccessor == null || s_coreResponseDataType == null || s_coreStatusCodeAccessor == null || s_coreHeadersAccessor == null)
		{
			throw new InvalidOperationException(System.SR.UnableToInitialize);
		}
	}

	private static void PerformInjection()
	{
		FieldInfo? field = typeof(ServicePointManager).GetField("s_ServicePointTable", BindingFlags.Static | BindingFlags.NonPublic);
		if (field == null)
		{
			throw new InvalidOperationException(System.SR.UnableAccessServicePointTable);
		}
		ServicePointHashtable value = new ServicePointHashtable((field.GetValue(null) as Hashtable) ?? new Hashtable());
		field.SetValue(null, value);
	}

	private static Func<TClass, TField> CreateFieldGetter<TClass, TField>(string fieldName, BindingFlags flags) where TClass : class
	{
		FieldInfo field = typeof(TClass).GetField(fieldName, flags);
		if (field != null)
		{
			DynamicMethod dynamicMethod = new DynamicMethod(field.ReflectedType.FullName + ".get_" + field.Name, typeof(TField), new Type[1] { typeof(TClass) }, restrictedSkipVisibility: true);
			ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
			iLGenerator.Emit(OpCodes.Ldarg_0);
			iLGenerator.Emit(OpCodes.Ldfld, field);
			iLGenerator.Emit(OpCodes.Ret);
			return (Func<TClass, TField>)dynamicMethod.CreateDelegate(typeof(Func<TClass, TField>));
		}
		return null;
	}

	private static Func<object, TField> CreateFieldGetter<TField>(Type classType, string fieldName, BindingFlags flags)
	{
		FieldInfo field = classType.GetField(fieldName, flags);
		if (field != null)
		{
			DynamicMethod dynamicMethod = new DynamicMethod(classType.FullName + ".get_" + field.Name, typeof(TField), new Type[1] { typeof(object) }, restrictedSkipVisibility: true);
			ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
			iLGenerator.Emit(OpCodes.Ldarg_0);
			iLGenerator.Emit(OpCodes.Castclass, classType);
			iLGenerator.Emit(OpCodes.Ldfld, field);
			iLGenerator.Emit(OpCodes.Ret);
			return (Func<object, TField>)dynamicMethod.CreateDelegate(typeof(Func<object, TField>));
		}
		return null;
	}
}
