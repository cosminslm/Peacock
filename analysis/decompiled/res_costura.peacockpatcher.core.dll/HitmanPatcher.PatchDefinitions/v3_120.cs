namespace HitmanPatcher.PatchDefinitions;

public static class v3_120
{
	private static readonly HitmanVersion v3_120_0_epic_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(16578509, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(13126285, "0F85B5000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(13126515, "0F84B8000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(63460792, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[3]
		{
			new Patch(32298600, Patch.https, Patch.http, MemProtection.PAGE_READONLY),
			new Patch(13195449, "0B", "0A", MemProtection.PAGE_EXECUTE_READ),
			new Patch(13195679, "0B", "0A", MemProtection.PAGE_EXECUTE_READ)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(63461392, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	private static readonly HitmanVersion v3_120_0_steam_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(16576621, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(13115101, "0F85B5000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(13115331, "0F84B8000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(63490936, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(32312872, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(63491536, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	public static void AddVersions()
	{
		HitmanVersion.AddVersion("3.120.0.0_epic_dx12", 1657160180u, v3_120_0_epic_dx12);
		HitmanVersion.AddVersion("3.120.0.0_steam_dx12", 1657160075u, v3_120_0_steam_dx12);
	}
}
