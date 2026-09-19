namespace HitmanPatcher.PatchDefinitions;

internal static class v3_50
{
	private static readonly HitmanVersion v3_50_0_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(16404013, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(12998285, "0F85B5000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(12998515, "0F84B8000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(61967832, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(31914912, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(61970264, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	private static readonly HitmanVersion v3_50_0_h1_dx12 = new HitmanVersion
	{
		certpin = v3_50_0_dx12.certpin,
		authheader = v3_50_0_dx12.authheader,
		configdomain = v3_50_0_dx12.configdomain,
		protocol = new Patch[1]
		{
			new Patch(31914848, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = v3_50_0_dx12.dynres_noforceoffline
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("3.50.0.0_dx12", 1626941840u, v3_50_0_dx12);
		HitmanVersion.AddVersion("3.50.0.0-h1_dx12", 1629373474u, v3_50_0_h1_dx12);
	}
}
