using System.Resources;
using FxResources.System.Diagnostics.DiagnosticSource;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = GetUsingResourceKeysSwitchValue();

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ActivityIdFormatInvalid => GetResourceString("ActivityIdFormatInvalid");

	internal static string ActivityNotRunning => GetResourceString("ActivityNotRunning");

	internal static string ActivityNotStarted => GetResourceString("ActivityNotStarted");

	internal static string ActivityStartAlreadyStarted => GetResourceString("ActivityStartAlreadyStarted");

	internal static string ActivitySetParentAlreadyStarted => GetResourceString("ActivitySetParentAlreadyStarted");

	internal static string EndTimeNotUtc => GetResourceString("EndTimeNotUtc");

	internal static string OperationNameInvalid => GetResourceString("OperationNameInvalid");

	internal static string ParentIdAlreadySet => GetResourceString("ParentIdAlreadySet");

	internal static string ParentIdInvalid => GetResourceString("ParentIdInvalid");

	internal static string SetFormatOnStartedActivity => GetResourceString("SetFormatOnStartedActivity");

	internal static string SetParentIdOnActivityWithParent => GetResourceString("SetParentIdOnActivityWithParent");

	internal static string StartTimeNotUtc => GetResourceString("StartTimeNotUtc");

	internal static string KeyAlreadyExist => GetResourceString("KeyAlreadyExist");

	internal static string InvalidTraceParent => GetResourceString("InvalidTraceParent");

	internal static string UnableAccessServicePointTable => GetResourceString("UnableAccessServicePointTable");

	internal static string UnableToInitialize => GetResourceString("UnableToInitialize");

	internal static string UnsupportedType => GetResourceString("UnsupportedType");

	internal static string Arg_BufferTooSmall => GetResourceString("Arg_BufferTooSmall");

	internal static string InvalidInstrumentType => GetResourceString("InvalidInstrumentType");

	internal static string InvalidHistogramExplicitBucketBoundaries => GetResourceString("InvalidHistogramExplicitBucketBoundaries");

	private static bool GetUsingResourceKeysSwitchValue()
	{
		if (!AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled))
		{
			return false;
		}
		return isEnabled;
	}

	internal static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	private static string GetResourceString(string resourceKey)
	{
		if (UsingResourceKeys())
		{
			return resourceKey;
		}
		string result = null;
		try
		{
			result = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		return result;
	}

	private static string GetResourceString(string resourceKey, string defaultString)
	{
		string resourceString = GetResourceString(resourceKey);
		if (!(resourceKey == resourceString) && resourceString != null)
		{
			return resourceString;
		}
		return defaultString;
	}

	internal static string Format(string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[2] { resourceFormat, p1 });
		}
		return string.Format(resourceFormat, p1);
	}

	internal static string Format(string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[3] { resourceFormat, p1, p2 });
		}
		return string.Format(resourceFormat, p1, p2);
	}

	internal static string Format(string resourceFormat, object p1, object p2, object p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[4] { resourceFormat, p1, p2, p3 });
		}
		return string.Format(resourceFormat, p1, p2, p3);
	}

	internal static string Format(string resourceFormat, params object[] args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + ", " + string.Join(", ", args);
			}
			return string.Format(resourceFormat, args);
		}
		return resourceFormat;
	}

	internal static string Format(IFormatProvider provider, string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[2] { resourceFormat, p1 });
		}
		return string.Format(provider, resourceFormat, p1);
	}

	internal static string Format(IFormatProvider provider, string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[3] { resourceFormat, p1, p2 });
		}
		return string.Format(provider, resourceFormat, p1, p2);
	}

	internal static string Format(IFormatProvider provider, string resourceFormat, object p1, object p2, object p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[4] { resourceFormat, p1, p2, p3 });
		}
		return string.Format(provider, resourceFormat, p1, p2, p3);
	}

	internal static string Format(IFormatProvider provider, string resourceFormat, params object[] args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + ", " + string.Join(", ", args);
			}
			return string.Format(provider, resourceFormat, args);
		}
		return resourceFormat;
	}
}
