using System.Resources;
using FxResources.System.Memory;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(FxResources.System.Memory.SR)));

	internal static string NotSupported_CannotCallEqualsOnSpan => GetResourceString("NotSupported_CannotCallEqualsOnSpan");

	internal static string NotSupported_CannotCallGetHashCodeOnSpan => GetResourceString("NotSupported_CannotCallGetHashCodeOnSpan");

	internal static string Argument_InvalidTypeWithPointersNotSupported => GetResourceString("Argument_InvalidTypeWithPointersNotSupported");

	internal static string Argument_DestinationTooShort => GetResourceString("Argument_DestinationTooShort");

	internal static string MemoryDisposed => GetResourceString("MemoryDisposed");

	internal static string OutstandingReferences => GetResourceString("OutstandingReferences");

	internal static string Argument_BadFormatSpecifier => GetResourceString("Argument_BadFormatSpecifier");

	internal static string Argument_GWithPrecisionNotSupported => GetResourceString("Argument_GWithPrecisionNotSupported");

	internal static string Argument_CannotParsePrecision => GetResourceString("Argument_CannotParsePrecision");

	internal static string Argument_PrecisionTooLarge => GetResourceString("Argument_PrecisionTooLarge");

	internal static string Argument_OverlapAlignmentMismatch => GetResourceString("Argument_OverlapAlignmentMismatch");

	internal static string EndPositionNotReached => GetResourceString("EndPositionNotReached");

	internal static string UnexpectedSegmentType => GetResourceString("UnexpectedSegmentType");

	private static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	internal static string GetResourceString(string resourceKey)
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

	internal static string GetResourceString(string resourceKey, string defaultString)
	{
		string resourceString = GetResourceString(resourceKey);
		if (!(resourceKey == resourceString) && resourceString != null)
		{
			return resourceString;
		}
		return defaultString;
	}

	internal static string Format(string resourceFormat, object? p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[2] { resourceFormat, p1 });
		}
		return string.Format(resourceFormat, p1);
	}

	internal static string Format(string resourceFormat, object? p1, object? p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[3] { resourceFormat, p1, p2 });
		}
		return string.Format(resourceFormat, p1, p2);
	}

	internal static string Format(string resourceFormat, object? p1, object? p2, object? p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[4] { resourceFormat, p1, p2, p3 });
		}
		return string.Format(resourceFormat, p1, p2, p3);
	}

	internal static string Format(string resourceFormat, params object?[]? args)
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

	internal static string Format(IFormatProvider? provider, string resourceFormat, object? p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[2] { resourceFormat, p1 });
		}
		return string.Format(provider, resourceFormat, p1);
	}

	internal static string Format(IFormatProvider? provider, string resourceFormat, object? p1, object? p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[3] { resourceFormat, p1, p2 });
		}
		return string.Format(provider, resourceFormat, p1, p2);
	}

	internal static string Format(IFormatProvider? provider, string resourceFormat, object? p1, object? p2, object? p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", new object[4] { resourceFormat, p1, p2, p3 });
		}
		return string.Format(provider, resourceFormat, p1, p2, p3);
	}

	internal static string Format(IFormatProvider? provider, string resourceFormat, params object?[]? args)
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
