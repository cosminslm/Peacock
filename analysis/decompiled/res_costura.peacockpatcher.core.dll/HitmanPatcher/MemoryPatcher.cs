using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HitmanPatcher;

public static class MemoryPatcher
{
	public struct Options
	{
		public bool DisableCertPinning;

		public bool AlwaysSendAuthHeader;

		public bool SetCustomConfigDomain;

		public string CustomConfigDomain;

		public bool UseHttp;

		public bool EnableDynamicResources;

		public bool DisableForceOfflineOnFailedDynamicResources;
	}

	public static HashSet<int> patchedprocesses = new HashSet<int>();

	private static List<Process> GetProcessesByName(params string[] names)
	{
		Process[] processes = Process.GetProcesses();
		List<Process> list = new List<Process>();
		Process[] array = processes;
		foreach (Process process in array)
		{
			try
			{
				if (Enumerable.Contains<string>(names, process.ProcessName, StringComparer.OrdinalIgnoreCase))
				{
					list.Add(process);
				}
				else
				{
					process.Dispose();
				}
			}
			catch (InvalidOperationException)
			{
				process.Dispose();
			}
		}
		return list;
	}

	public static void PatchAllProcesses(ILoggingProvider logger, Options patchOptions)
	{
		foreach (Process item in (IEnumerable<Process>)GetProcessesByName("HITMAN", "HITMAN2", "HITMAN3"))
		{
			if (!patchedprocesses.Contains(item.Id))
			{
				patchedprocesses.Add(item.Id);
				try
				{
					bool flag = false;
					try
					{
						flag = patchedprocesses.Contains(Pinvoke.GetProcessParentPid(item));
					}
					catch (Win32Exception ex)
					{
						if (ex.NativeErrorCode == 5 && !Compositions.HasAdmin)
						{
							logger.log(string.Format("Access denied, try running the patcher as admin.", Array.Empty<object>()));
							item.Dispose();
							continue;
						}
						flag = true;
					}
					if (flag)
					{
						logger.log($"Skipping PID {item.Id}...");
						item.Dispose();
						continue;
					}
					if (Patch(item, patchOptions))
					{
						logger.log($"Successfully patched processid {item.Id}");
						if (patchOptions.SetCustomConfigDomain)
						{
							logger.log($"Injected server: {patchOptions.CustomConfigDomain}");
						}
					}
					else
					{
						patchedprocesses.Remove(item.Id);
					}
					goto IL_0176;
				}
				catch (Win32Exception ex2)
				{
					logger.log($"Failed to patch processid {item.Id}: error code {ex2.NativeErrorCode}");
					logger.log(ex2.Message);
					goto IL_0176;
				}
				catch (NotImplementedException)
				{
					logger.log($"Failed to patch processid {item.Id}: unknown version");
					goto IL_0176;
				}
			}
			goto IL_0176;
			IL_0176:
			item.Dispose();
		}
	}

	public static bool Patch(Process process, Options patchOptions)
	{
		IntPtr intPtr = Pinvoke.OpenProcess(ProcessAccess.PROCESS_VM_READ | ProcessAccess.PROCESS_VM_WRITE | ProcessAccess.PROCESS_VM_OPERATION, bInheritHandle: false, process.Id);
		if (intPtr == IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get a process handle.");
		}
		try
		{
			IntPtr zero = IntPtr.Zero;
			try
			{
				zero = process.MainModule.BaseAddress;
			}
			catch (NullReferenceException)
			{
				return false;
			}
			uint timestamp = getTimestamp(intPtr, zero);
			HitmanVersion result = HitmanVersion.GetVersion(timestamp);
			if (result == HitmanVersion.NotFound)
			{
				if (!AOBScanner.TryGetHitmanVersionByScanning(process, intPtr, out result))
				{
					throw new NotImplementedException();
				}
				HitmanVersion.AddVersion(timestamp.ToString("X8"), timestamp, result);
			}
			MemProtection lpflOldProtect = (MemProtection)0u;
			byte[] array = Encoding.ASCII.GetBytes(patchOptions.CustomConfigDomain).Concat(new byte[1]).ToArray();
			List<Patch> list = new List<Patch>();
			if (!IsReadyForPatching(intPtr, zero, result))
			{
				Pinvoke.CloseHandle(intPtr);
				return false;
			}
			if (patchOptions.DisableCertPinning)
			{
				list.AddRange(result.certpin);
			}
			if (patchOptions.AlwaysSendAuthHeader)
			{
				list.AddRange(result.authheader);
			}
			if (patchOptions.SetCustomConfigDomain)
			{
				list.AddRange(result.configdomain);
			}
			if (patchOptions.UseHttp)
			{
				list.AddRange(result.protocol);
			}
			if (patchOptions.EnableDynamicResources && result.dynres_enable != null)
			{
				list.AddRange(result.dynres_enable);
			}
			if (patchOptions.DisableForceOfflineOnFailedDynamicResources)
			{
				list.AddRange(result.dynres_noforceoffline);
			}
			foreach (Patch item in list)
			{
				byte[] array2 = item.patch;
				if (item.customPatch == "configdomain")
				{
					array2 = array;
				}
				MemProtection flNewProtect;
				switch (item.defaultProtection)
				{
				case MemProtection.PAGE_EXECUTE_READ:
				case MemProtection.PAGE_EXECUTE_READWRITE:
					flNewProtect = MemProtection.PAGE_EXECUTE_READWRITE;
					break;
				case MemProtection.PAGE_READONLY:
				case MemProtection.PAGE_READWRITE:
					flNewProtect = MemProtection.PAGE_READWRITE;
					break;
				default:
					throw new Exception("This shouldn't be able to happen.");
				}
				if (!Pinvoke.VirtualProtectEx(intPtr, zero + item.offset, (UIntPtr)(ulong)array2.Length, flNewProtect, out lpflOldProtect))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), string.Format("error at {0} for offset {1:X}", "vpe1", item.offset));
				}
				if (!Pinvoke.WriteProcessMemory(intPtr, zero + item.offset, array2, (UIntPtr)(ulong)array2.Length, out var byteswritten))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), string.Format("error at {0} for offset {1:X}\nBytes written: {2}", "wpm", item.offset, byteswritten));
				}
				MemProtection flNewProtect2 = lpflOldProtect;
				if (!Pinvoke.VirtualProtectEx(intPtr, zero + item.offset, (UIntPtr)(ulong)array2.Length, flNewProtect2, out lpflOldProtect))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), string.Format("error at {0} for offset {1:X}", "vpe2", item.offset));
				}
			}
		}
		finally
		{
			Pinvoke.CloseHandle(intPtr);
		}
		return true;
	}

	private static bool IsReadyForPatching(IntPtr hProcess, IntPtr baseAddress, HitmanVersion version)
	{
		byte[] array = new byte[1];
		bool flag = true;
		MemProtection flNewProtect = MemProtection.PAGE_READWRITE;
		foreach (Patch item in version.configdomain.Where((Patch p) => p.customPatch == "configdomain"))
		{
			if (!Pinvoke.VirtualProtectEx(hProcess, baseAddress + item.offset, (UIntPtr)1uL, flNewProtect, out var lpflOldProtect))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"error at vpe1Check for offset {item.offset:X}");
			}
			if (!Pinvoke.ReadProcessMemory(hProcess, baseAddress + item.offset, array, (UIntPtr)1uL, out var _))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"error at rpmCheck for offset {item.offset:X}");
			}
			if (!Pinvoke.VirtualProtectEx(hProcess, baseAddress + item.offset, (UIntPtr)1uL, lpflOldProtect, out lpflOldProtect))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"error at vpe2Check for offset {item.offset:X}");
			}
			flag &= array[0] != 0;
		}
		return flag;
	}

	public static uint getTimestamp(IntPtr hProcess, IntPtr baseAddress)
	{
		byte[] array = new byte[4];
		Pinvoke.ReadProcessMemory(hProcess, baseAddress + 60, array, (UIntPtr)4uL, out var numberOfBytesRead);
		int num = BitConverter.ToInt32(array, 0);
		Pinvoke.ReadProcessMemory(hProcess, baseAddress + num + 8, array, (UIntPtr)4uL, out numberOfBytesRead);
		return BitConverter.ToUInt32(array, 0);
	}
}
