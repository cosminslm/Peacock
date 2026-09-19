namespace System.Diagnostics;

[Flags]
internal enum DsesActivityEvents
{
	None = 0,
	ActivityStart = 1,
	ActivityStop = 2,
	All = ActivityStart | ActivityStop
}
