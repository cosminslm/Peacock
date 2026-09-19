namespace System.Diagnostics;

internal static class DsesSamplerBuilder
{
	public static DsesSampleActivityFunc CreateParentRatioSampler(double ratio)
	{
		long idUpperBound = ((ratio <= 0.0) ? long.MinValue : ((ratio >= 1.0) ? long.MaxValue : ((long)(ratio * 9.223372036854776E+18))));
		return delegate(bool hasActivityContext, ref ActivityCreationOptions<ActivityContext> options)
		{
			if (hasActivityContext && options.TraceId != default(ActivityTraceId))
			{
				ActivityContext parentContext = options.Parent;
				ActivitySamplingResult activitySamplingResult = ParentRatioSampler(idUpperBound, in parentContext, options.TraceId);
				if (activitySamplingResult != ActivitySamplingResult.None || (!(parentContext == default(ActivityContext)) && !parentContext.IsRemote))
				{
					return activitySamplingResult;
				}
				return ActivitySamplingResult.PropagationData;
			}
			return ActivitySamplingResult.None;
		};
	}

	public static ActivitySamplingResult ParentRatioSampler(long idUpperBound, in ActivityContext parentContext, ActivityTraceId traceId)
	{
		if (parentContext.TraceId != default(ActivityTraceId))
		{
			if (!parentContext.TraceFlags.HasFlag(ActivityTraceFlags.Recorded))
			{
				return ActivitySamplingResult.None;
			}
			return ActivitySamplingResult.AllDataAndRecorded;
		}
		Span<byte> span = stackalloc byte[16];
		traceId.CopyTo(span);
		if (Math.Abs(GetLowerLong(span)) >= idUpperBound)
		{
			return ActivitySamplingResult.None;
		}
		return ActivitySamplingResult.AllDataAndRecorded;
		static long GetLowerLong(ReadOnlySpan<byte> bytes)
		{
			long num = 0L;
			for (int i = 0; i < 8; i++)
			{
				num <<= 8;
				num |= bytes[i] & 0xFF;
			}
			return num;
		}
	}
}
