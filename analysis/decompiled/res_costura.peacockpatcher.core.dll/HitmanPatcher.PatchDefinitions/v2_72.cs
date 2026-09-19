namespace HitmanPatcher.PatchDefinitions;

internal static class v2_72
{
	private static readonly HitmanVersion v2_72_0_h4_dx11 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(15938403, "75", "EB", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(11903480, "75", "EB", MemProtection.PAGE_EXECUTE_READ),
			new Patch(11903516, "0F8486000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(45857800, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[2]
		{
			new Patch(25351576, Patch.https, Patch.http, MemProtection.PAGE_READONLY),
			new Patch(11857252, "0C", "0B", MemProtection.PAGE_EXECUTE_READ)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(45860168, "01", "00", MemProtection.PAGE_READWRITE)
		}
	};

	private static readonly HitmanVersion v2_72_0_h4_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(15937219, "75", "EB", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = new Patch[2]
		{
			new Patch(11902296, "75", "EB", MemProtection.PAGE_EXECUTE_READ),
			new Patch(11902332, "0F8486000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
		},
		configdomain = new Patch[1]
		{
			new Patch(45982216, "", "", MemProtection.PAGE_READWRITE, "configdomain")
		},
		protocol = new Patch[2]
		{
			new Patch(25462456, Patch.https, Patch.http, MemProtection.PAGE_READONLY),
			new Patch(11856068, "0C", "0B", MemProtection.PAGE_EXECUTE_READ)
		},
		dynres_noforceoffline = new Patch[1]
		{
			new Patch(45984584, "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
		}
	};

	private static readonly HitmanVersion v2_72_0_h5_dx11 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(15937454, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = v2_72_0_h4_dx11.authheader,
		configdomain = v2_72_0_h4_dx11.configdomain,
		protocol = v2_72_0_h4_dx11.protocol,
		dynres_noforceoffline = v2_72_0_h4_dx11.dynres_noforceoffline
	};

	private static readonly HitmanVersion v2_72_0_h5_dx12 = new HitmanVersion
	{
		certpin = new Patch[1]
		{
			new Patch(15936270, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		},
		authheader = v2_72_0_h4_dx12.authheader,
		configdomain = v2_72_0_h4_dx12.configdomain,
		protocol = v2_72_0_h4_dx12.protocol,
		dynres_noforceoffline = v2_72_0_h4_dx12.dynres_noforceoffline
	};

	internal static void AddVersions()
	{
		HitmanVersion.AddVersion("2.72.0.0-h4_dx11", 1592381541u, v2_72_0_h4_dx11);
		HitmanVersion.AddVersion("2.72.0.0-h4_dx12", 1592381589u, v2_72_0_h4_dx12);
		HitmanVersion.AddVersion("2.72.0.0-h5_dx11", 1603098570u, v2_72_0_h5_dx11);
		HitmanVersion.AddVersion("2.72.0.0-h5_dx12", 1603098323u, v2_72_0_h5_dx12);
	}
}
