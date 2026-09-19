using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;

namespace HitmanPatcher;

public class Patch
{
	public static readonly byte[] http = Encoding.ASCII.GetBytes("http://{0}\0").ToArray();

	public static readonly byte[] https = Encoding.ASCII.GetBytes("https://{0}\0").ToArray();

	public readonly int offset;

	public readonly byte[] original;

	public readonly byte[] patch;

	public readonly string customPatch;

	public readonly MemProtection defaultProtection;

	public Patch(int offset, byte[] original, byte[] patch, MemProtection defaultProtection, string customPatch = "")
	{
		this.offset = offset;
		this.original = original;
		this.patch = patch;
		this.defaultProtection = defaultProtection;
		this.customPatch = customPatch;
	}

	public Patch(int offset, string original, string patch, MemProtection defaultProtection, string customPatch = "")
		: this(offset, SoapHexBinary.Parse(original).Value, SoapHexBinary.Parse(patch).Value, defaultProtection, customPatch)
	{
	}
}
