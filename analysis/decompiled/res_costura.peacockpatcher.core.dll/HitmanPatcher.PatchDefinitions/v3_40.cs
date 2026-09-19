namespace HitmanPatcher.PatchDefinitions;

internal static class v3_40
{
	private static readonly HitmanVersion v3_40_0_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(16385261, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(12982349, "0F85B5000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(12982579, "0F84B8000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(61742552, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(31873304, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(61744984, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	private static readonly HitmanVersion v3_40_1_dx12 = new HitmanVersion
	{
		certpin = v3_40_0_dx12.certpin,
		authheader = v3_40_0_dx12.authheader,
		configdomain = new Patch[1]
		{
			new Patch(61742360, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(31873384, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(61744792, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("3.40.0.0_dx12", 1623315825u, v3_40_0_dx12);
		HitmanVersion.AddVersion("3.40.1.0_dx12", 1624365008u, v3_40_1_dx12);
	}
}
