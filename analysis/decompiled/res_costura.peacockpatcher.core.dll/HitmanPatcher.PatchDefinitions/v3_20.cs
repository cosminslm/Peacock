namespace HitmanPatcher.PatchDefinitions;

internal static class v3_20
{
	private static readonly HitmanVersion v3_20_0_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(13289262, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(10596215, "75", "EB", MemProtection.PAGE_EXECUTE_READ),
			new Patch(10596251, "0F8482000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(44844680, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[3]
		{
			new Patch(26606040, Patch.https, Patch.http, MemProtection.PAGE_READONLY),
			new Patch(10550244, "0C", "0B", MemProtection.PAGE_EXECUTE_READ),
			new Patch(10745056, "0C", "0B", MemProtection.PAGE_EXECUTE_READ)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(44847112, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	private static readonly HitmanVersion v3_20_0_h1_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(13289278, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = v3_20_0_dx12.authheader,
		configdomain = v3_20_0_dx12.configdomain,
		protocol = v3_20_0_dx12.protocol,
		dynres_noforceoffline = v3_20_0_dx12.dynres_noforceoffline
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("3.20.0.0_dx12", 1615836263u, v3_20_0_dx12);
		HitmanVersion.AddVersion("3.20.0.0-h1_dx12", 1616556374u, v3_20_0_h1_dx12);
	}
}
