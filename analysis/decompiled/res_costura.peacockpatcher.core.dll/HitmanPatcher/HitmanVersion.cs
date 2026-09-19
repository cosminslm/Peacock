using System.Collections.Generic;
using HitmanPatcher.PatchDefinitions;

namespace HitmanPatcher;

public class HitmanVersion
{
	public Patch[] certpin;

	public Patch[] authheader;

	public Patch[] configdomain;

	public Patch[] protocol;

	public Patch[] dynres_noforceoffline;

	public Patch[] dynres_enable;

	private static Dictionary<uint, string> timestampMap;

	private static Dictionary<string, HitmanVersion> versionMap;

	public static readonly HitmanVersion NotFound;

	public static void AddVersion(string name, uint timestamp, HitmanVersion patchVersions)
	{
		timestampMap.Add(timestamp, name);
		versionMap.Add(name, patchVersions);
	}

	private static string VersionStringFromTimestamp(uint timestamp)
	{
		if (!timestampMap.TryGetValue(timestamp, out var value))
		{
			return "unknown";
		}
		return value;
	}

	public static HitmanVersion GetVersion(uint timestamp)
	{
		string key = VersionStringFromTimestamp(timestamp);
		if (versionMap.TryGetValue(key, out var value))
		{
			return value;
		}
		return NotFound;
	}

	static HitmanVersion()
	{
		timestampMap = new Dictionary<uint, string>();
		versionMap = new Dictionary<string, HitmanVersion>();
		NotFound = new HitmanVersion();
		v1_12.AddVersions();
		v1_15.AddVersions();
		v1_16.AddVersions();
		v2_13.AddVersions();
		v2_71.AddVersions();
		v2_72.AddVersions();
		v3_10.AddVersions();
		v3_11.AddVersions();
		v3_20.AddVersions();
		v3_30.AddVersions();
		v3_40.AddVersions();
		v3_50.AddVersions();
		v3_70.AddVersions();
		v3_100.AddVersions();
		v3_110.AddVersions();
		v3_120.AddVersions();
		vScpc.AddVersions();
	}
}
