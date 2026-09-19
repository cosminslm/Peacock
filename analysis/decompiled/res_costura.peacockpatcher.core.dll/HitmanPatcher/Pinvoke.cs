using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HitmanPatcher;

public static class Pinvoke
{
	[DllImport("kernel32", SetLastError = true)]
	public static extern IntPtr OpenProcess(ProcessAccess dwDesiredAccess, bool bInheritHandle, int dwProcessId);

	[DllImport("kernel32", SetLastError = true)]
	public static extern bool CloseHandle(IntPtr hObject);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool WriteProcessMemory([In] IntPtr hProcess, [In] IntPtr address, [In][MarshalAs(UnmanagedType.LPArray)] byte[] buffer, [In] UIntPtr size, out UIntPtr byteswritten);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool ReadProcessMemory([In] IntPtr hProcess, [In] IntPtr address, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] buffer, [In] UIntPtr size, out UIntPtr numberOfBytesRead);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool VirtualProtectEx([In] IntPtr hProcess, [In] IntPtr lpAddress, [In] UIntPtr dwSize, [In] MemProtection flNewProtect, out MemProtection lpflOldProtect);

	[DllImport("ntdll.dll")]
	public static extern int NtQueryInformationProcess(IntPtr hProcess, PROCESSINFOCLASS processInformationClass, out PROCESS_BASIC_INFORMATION processInformation, uint processInformationLength, out uint returnLength);

	public static int GetProcessParentPid(Process process)
	{
		IntPtr intPtr = OpenProcess(ProcessAccess.PROCESS_QUERY_INFORMATION | ProcessAccess.PROCESS_VM_READ, bInheritHandle: false, process.Id);
		if (intPtr == IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get a process handle.");
		}
		PROCESS_BASIC_INFORMATION processInformation = default(PROCESS_BASIC_INFORMATION);
		int num = NtQueryInformationProcess(intPtr, PROCESSINFOCLASS.ProcessBasicInformation, out processInformation, (uint)Marshal.SizeOf(processInformation), out var _);
		CloseHandle(intPtr);
		if (num != 0)
		{
			throw new Win32Exception(num, "(NTSTATUS)");
		}
		return processInformation.Reserved3.ToInt32();
	}
}
