namespace HitmanPatcher.PatchDefinitions;

internal static class v1_12
{
	private static readonly HitmanVersion v1_12_2_dx11 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(14124968, "75", "EB", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(11031215, "0F84B2000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(11031231, "0F8599000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(41461576, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(22345344, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(41462728, "01", "00", MemProtection.PAGE_READWRITE)
		}
	};

	private static readonly HitmanVersion v1_12_2_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(14122360, "75", "EB", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(11027151, "0F84B2000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(11027167, "0F8599000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(41495496, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(22374400, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(41496648, "01", "00", MemProtection.PAGE_READWRITE)
		}
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("1.12.2.0_dx11", 1506525738u, v1_12_2_dx11);
		HitmanVersion.AddVersion("1.12.2.0_dx12", 1506525697u, v1_12_2_dx12);
	}
}
