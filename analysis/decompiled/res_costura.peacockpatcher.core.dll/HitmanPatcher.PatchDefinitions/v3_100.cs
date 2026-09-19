namespace HitmanPatcher.PatchDefinitions;

internal static class v3_100
{
	private static readonly HitmanVersion v3_100_0_epic_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(16682349, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(13248669, "0F85B5000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(13248899, "0F84B8000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(62885480, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(31892968, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(62886080, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	private static readonly HitmanVersion v3_100_0_steam_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(16680429, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(13237437, "0F85B5000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(13237667, "0F84B8000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(62912456, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(31913872, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(62913056, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	private static readonly HitmanVersion v3_100_0_h1_epic_dx12 = new HitmanVersion
	{
		certpin = v3_100_0_epic_dx12.certpin,
		authheader = v3_100_0_epic_dx12.authheader,
		configdomain = new Patch[1]
		{
			new Patch(62885416, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(31892920, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(62886016, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	private static readonly HitmanVersion v3_100_0_h1_steam_dx12 = new HitmanVersion
	{
		certpin = v3_100_0_steam_dx12.certpin,
		authheader = v3_100_0_steam_dx12.authheader,
		configdomain = new Patch[1]
		{
			new Patch(62912520, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(31913888, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(62913120, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("3.100.0.0_epic_dx12", 1642131073u, v3_100_0_epic_dx12);
		HitmanVersion.AddVersion("3.100.0.0_steam_dx12", 1642094071u, v3_100_0_steam_dx12);
		HitmanVersion.AddVersion("3.100.0.0-h1_epic_dx12", 1642688063u, v3_100_0_h1_epic_dx12);
		HitmanVersion.AddVersion("3.100.0.0-h1_steam_dx12", 1642688893u, v3_100_0_h1_steam_dx12);
	}
}
