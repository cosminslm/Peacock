namespace HitmanPatcher.PatchDefinitions;

internal static class v3_70
{
	private static readonly HitmanVersion v3_70_0_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(16424365, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(13018109, "0F85B5000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
			new Patch(13018339, "0F84B8000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(62005656, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[1]
		{
			new Patch(31946064, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(62008088, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	private static readonly HitmanVersion v3_70_0_h1_dx12 = new HitmanVersion
	{
		certpin = v3_70_0_dx12.certpin,
		authheader = v3_70_0_dx12.authheader,
		configdomain = v3_70_0_dx12.configdomain,
		protocol = new Patch[1]
		{
			new Patch(31946112, "68", "61", MemProtection.PAGE_READONLY)
		},
		dynres_noforceoffline = v3_70_0_dx12.dynres_noforceoffline
	};

	private static readonly HitmanVersion v3_70_0_h2_dx12 = v3_70_0_h1_dx12;

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("3.70.0.0_dx12", 1632305719u, v3_70_0_dx12);
		HitmanVersion.AddVersion("3.70.0.0-h1_dx12", 1633096095u, v3_70_0_h1_dx12);
		HitmanVersion.AddVersion("3.70.0.0-h2_dx12", 1633486353u, v3_70_0_h2_dx12);
	}
}
