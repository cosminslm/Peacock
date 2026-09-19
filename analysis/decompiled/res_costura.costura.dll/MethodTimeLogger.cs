using System;
using System.Reflection;

internal static class MethodTimeLogger
{
	public static void Log(MethodBase methodBase, long milliseconds, string message)
	{
		Log(methodBase.DeclaringType ?? typeof(object), methodBase.Name, milliseconds, message);
	}

	public static void Log(Type type, string methodName, long milliseconds, string message)
	{
	}
}
