namespace HitmanPatcher.PatchDefinitions;

internal static class v3_11
{
	private static readonly HitmanVersion v3_11_0_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(13262654, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(10575111, "75", "EB", MemProtection.PAGE_EXECUTE_READ),
			new Patch(10575147, "0F8482000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(44795112, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[3]
		{
			new Patch(26567328, Patch.https, Patch.http, MemProtection.PAGE_READONLY),
			new Patch(10529492, "0C", "0B", MemProtection.PAGE_EXECUTE_READ),
			new Patch(10723616, "0C", "0B", MemProtection.PAGE_EXECUTE_READ)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(44797544, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("3.11.0.0_dx12", 1613991054u, v3_11_0_dx12);
	}
}
