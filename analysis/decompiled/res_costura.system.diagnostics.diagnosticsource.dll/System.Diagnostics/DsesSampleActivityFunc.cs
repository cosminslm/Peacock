namespace System.Diagnostics;

internal delegate ActivitySamplingResult DsesSampleActivityFunc(bool hasActivityContext, ref ActivityCreationOptions<ActivityContext> options);
