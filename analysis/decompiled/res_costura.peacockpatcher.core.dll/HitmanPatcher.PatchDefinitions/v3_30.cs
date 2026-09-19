namespace HitmanPatcher.PatchDefinitions;

internal static class v3_30
{
	private static readonly HitmanVersion v3_30_0_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(16345757, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(12954765, "0F85B5000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(12954995, "0F84B8000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(61639064, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(31808064, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(61641496, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("3.30.0.0_dx12", 1619748991u, v3_30_0_dx12);
	}
}
