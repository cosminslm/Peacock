namespace HitmanPatcher.PatchDefinitions;

public static class v3_110
{
	private static readonly HitmanVersion v3_110_1_epic_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(16815149, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(13365949, "0F85B5000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(13366179, "0F84B8000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(63447224, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(32285136, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(63447824, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	private static readonly HitmanVersion v3_110_1_steam_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(16813245, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(13354733, "0F85B5000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(13354963, "0F84B8000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(63473848, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(32298816, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(63474448, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	public static void AddVersions()
	{
		HitmanVersion.AddVersion("3.110.1.0_epic_dx12", 1652794232u, v3_110_1_epic_dx12);
		HitmanVersion.AddVersion("3.110.1.0_steam_dx12", 1652795398u, v3_110_1_steam_dx12);
	}
}
