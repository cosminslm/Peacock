namespace HitmanPatcher.PatchDefinitions;

internal static class v2_13
{
	private static readonly HitmanVersion v2_13_0_h3_dx11 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(15652450, "75", "EB", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(11772247, "75", "EB", MemProtection.PAGE_EXECUTE_READ),
			new Patch(11772283, "0F8483000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(45788808, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[2]
		{
			new Patch(25159536, Patch.https, Patch.http, MemProtection.PAGE_READONLY),
			new Patch(11728312, "0C", "0B", MemProtection.PAGE_EXECUTE_READ)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(45790728, "01", "00", MemProtection.PAGE_READWRITE)
		}
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("2.13.0.0-h3_dx11", 1548330187u, v2_13_0_h3_dx11);
	}
}
