using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics;

internal static class Helpers
{
	internal static string FormatTags(IEnumerable<KeyValuePair<string, object>> tags)
	{
		if (tags == null)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		foreach (KeyValuePair<string, object> tag in tags)
		{
			if (flag)
			{
				flag = false;
			}
			else
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(tag.Key).Append('=').Append(tag.Value);
		}
		return stringBuilder.ToString();
	}

	internal static string FormatTags(KeyValuePair<string, string>[] labels)
	{
		if (labels == null || labels.Length == 0)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < labels.Length; i++)
		{
			stringBuilder.Append(labels[i].Key).Append('=').Append(labels[i].Value);
			if (i != labels.Length - 1)
			{
				stringBuilder.Append(',');
			}
		}
		return stringBuilder.ToString();
	}

	internal static string FormatObjectHash(object obj)
	{
		if (obj != null)
		{
			return RuntimeHelpers.GetHashCode(obj).ToString(CultureInfo.InvariantCulture);
		}
		return string.Empty;
	}
}
