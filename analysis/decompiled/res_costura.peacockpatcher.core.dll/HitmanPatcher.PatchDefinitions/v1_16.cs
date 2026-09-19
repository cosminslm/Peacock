namespace HitmanPatcher.PatchDefinitions;

internal static class v1_16
{
	private static readonly HitmanVersion v1_16_0_epic_dx11 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(13475788, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(10254357, "0F84B3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(10254373, "0F84A3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(41133384, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(21870200, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(41134536, "01", "00", MemProtection.PAGE_READWRITE)
		}
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("1.16.0.0_epic_dx11", 1603101381u, v1_16_0_epic_dx11);
	}
}
