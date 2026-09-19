using System;

namespace HitmanPatcher;

[Flags]
public enum ProcessAccess : uint
{
	PROCESS_QUERY_INFORMATION = 0x400u,
	PROCESS_VM_READ = 0x10u,
	PROCESS_VM_WRITE = 0x20u,
	PROCESS_VM_OPERATION = 8u
}
