namespace HitmanPatcher.PatchDefinitions;

internal static class v3_10
{
	private static readonly HitmanVersion v3_10_0_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(13231454, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(10553399, "75", "EB", MemProtection.PAGE_EXECUTE_READ),
			new Patch(10553435, "0F8482000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(44742280, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[3]
		{
			new Patch(26524448, Patch.https, Patch.http, MemProtection.PAGE_READONLY),
			new Patch(10507924, "0C", "0B", MemProtection.PAGE_EXECUTE_READ),
			new Patch(10700880, "0C", "0B", MemProtection.PAGE_EXECUTE_READ)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(44744712, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	private static readonly HitmanVersion v3_10_0_h1_dx12 = v3_10_0_dx12;

	private static readonly HitmanVersion v3_10_1_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(13231646, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = v3_10_0_dx12.authheader,
		configdomain = v3_10_0_dx12.configdomain,
		protocol = v3_10_0_dx12.protocol,
		dynres_noforceoffline = v3_10_0_dx12.dynres_noforceoffline
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("3.10.0.0_dx12", 1610534424u, v3_10_0_dx12);
		HitmanVersion.AddVersion("3.10.0.0-h1_dx12", 1611154961u, v3_10_0_h1_dx12);
		HitmanVersion.AddVersion("3.10.1.0_dx12", 1611313398u, v3_10_1_dx12);
	}
}
