namespace HitmanPatcher.PatchDefinitions;

internal static class vScpc
{
	private static readonly HitmanVersion v1_0_1_h1_dx11_scpc = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(14887750, "75", "EB", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(11101429, "0F84B3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(11101445, "0F84A3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(44714312, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(23745120, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(44715592, "01", "00", MemProtection.PAGE_READWRITE)
		}
	};

	private static readonly HitmanVersion v1_0_1_h1_dx12_scpc = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(14881926, "75", "EB", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(11097733, "0F84B3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(11097749, "0F84A3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(44785416, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(23814768, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(44786696, "01", "00", MemProtection.PAGE_READWRITE)
		}
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("1.0.1.0-h1_dx11_sniper", 1529942141u, v1_0_1_h1_dx11_scpc);
		HitmanVersion.AddVersion("1.0.1.0-h1_dx12_sniper", 1529942409u, v1_0_1_h1_dx12_scpc);
	}
}
