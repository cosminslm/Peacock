namespace HitmanPatcher.PatchDefinitions;

internal static class v1_15
{
	private static readonly HitmanVersion v1_15_0_dx11 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(13464652, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(10238245, "0F84B3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(10238261, "0F84A3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(41141800, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(21877848, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(41142952, "01", "00", MemProtection.PAGE_READWRITE)
		}
	};

	private static readonly HitmanVersion v1_15_0_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(13466732, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(10243141, "0F84B3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(10243157, "0F84A3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(41175848, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(21906616, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(41177000, "01", "00", MemProtection.PAGE_READWRITE)
		}
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("1.15.0.0_dx11", 1603197113u, v1_15_0_dx11);
		HitmanVersion.AddVersion("1.15.0.0_dx12", 1603197136u, v1_15_0_dx12);
	}
}
