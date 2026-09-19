namespace HitmanPatcher.PatchDefinitions;

internal static class v2_71
{
	private static readonly HitmanVersion v2_71_0_h1_dx11 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(15938195, "75", "EB", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(11903544, "75", "EB", MemProtection.PAGE_EXECUTE_READ),
			new Patch(11903580, "0F8486000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(45856232, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[2]
		{
			new Patch(25351576, Patch.https, Patch.http, MemProtection.PAGE_READONLY),
			new Patch(11857316, "0C", "0B", MemProtection.PAGE_EXECUTE_READ)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(45858600, "01", "00", MemProtection.PAGE_READWRITE)
		}
	};

	private static readonly HitmanVersion v2_71_0_h1_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(15937011, "75", "EB", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(11902360, "75", "EB", MemProtection.PAGE_EXECUTE_READ),
			new Patch(11902396, "0F8486000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(45980840, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[2]
		{
			new Patch(25462456, Patch.https, Patch.http, MemProtection.PAGE_READONLY),
			new Patch(11856132, "0C", "0B", MemProtection.PAGE_EXECUTE_READ)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(45983208, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("2.71.0.0-h1_dx11", 1570630206u, v2_71_0_h1_dx11);
		HitmanVersion.AddVersion("2.71.0.0-h1_dx12", 1570630227u, v2_71_0_h1_dx12);
	}
}
