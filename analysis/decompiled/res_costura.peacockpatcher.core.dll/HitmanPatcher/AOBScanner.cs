using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HitmanPatcher;

internal static class AOBScanner
{
	public static bool TryGetHitmanVersionByScanning(Process process, IntPtr hProcess, out HitmanVersion result)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		IntPtr baseAddress = process.MainModule.BaseAddress;
		byte[] array = new byte[process.MainModule.ModuleMemorySize];
		Pinvoke.ReadProcessMemory(hProcess, baseAddress, array, (UIntPtr)(ulong)array.Length, out var _);
		Task<IEnumerable<Patch[]>> task = Task.Factory.ContinueWhenAll(new Task<Patch[]>[3]
		{
			findCertpin_nearjump(array),
			findCertpin_nearjump_new(array),
			findCertpin_shortjump(array)
		}, (Task<Patch[]>[] tasks) => from task8 in tasks
			select task8.Result into x
			where x != null
			select x);
		Task<IEnumerable<Patch[]>> task2 = Task.Factory.ContinueWhenAll(new Task<Patch[]>[4]
		{
			findAuthhead3_210(array),
			findAuthhead3_30(array),
			findAuthhead2_72(array),
			findAuthhead1_15(array)
		}, (Task<Patch[]>[] tasks) => from task8 in tasks
			select task8.Result into x
			where x != null
			select x);
		Task<IEnumerable<Patch[]>> task3 = Task.Factory.ContinueWhenAll(new Task<Patch[]>[1] { findConfigdomain(array) }, (Task<Patch[]>[] tasks) => from task8 in tasks
			select task8.Result into x
			where x != null
			select x);
		Task<IEnumerable<Patch[]>> task4 = Task.Factory.ContinueWhenAll(new Task<Patch[]>[1] { findProtocolCombined(array) }, (Task<Patch[]>[] tasks) => from task8 in tasks
			select task8.Result into x
			where x != null
			select x);
		Task<IEnumerable<Patch[]>> task5 = Task.Factory.ContinueWhenAll(new Task<Patch[]>[1] { findDynresForceoffline(array) }, (Task<Patch[]>[] tasks) => from task8 in tasks
			select task8.Result into x
			where x != null
			select x);
		Task<IEnumerable<Patch[]>> task6 = Task.Factory.ContinueWhenAll(new Task<Patch[]>[1] { findDynresEnable(array) }, (Task<Patch[]>[] tasks) => from task8 in tasks
			select task8.Result into x
			where x != null
			select x);
		Task<IEnumerable<Patch[]>>[] array2 = new Task<IEnumerable<Patch[]>>[5] { task, task2, task3, task4, task5 };
		Task<IEnumerable<Patch[]>>[] array3 = array2;
		int num = 0;
		Task[] array4 = new Task[1 + array3.Length];
		Task<IEnumerable<Patch[]>>[] array5 = array3;
		foreach (Task<IEnumerable<Patch[]>> task7 in array5)
		{
			array4[num] = task7;
			num++;
		}
		array4[num] = task6;
		Task.WaitAll(array4);
		stopwatch.Stop();
		if (array2.Any((Task<IEnumerable<Patch[]>> task8) => task8.Result.Count() != 1))
		{
			result = null;
			return false;
		}
		result = new HitmanVersion
		{
			certpin = task.Result.First(),
			authheader = task2.Result.First(),
			configdomain = task3.Result.First(),
			protocol = task4.Result.First(),
			dynres_noforceoffline = task5.Result.First(),
			dynres_enable = (task6.Result.FirstOrDefault() ?? Array.Empty<Patch>())
		};
		return true;
	}

	private static Task<Patch[]> findCertpin_nearjump(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[2]
		{
			Task.Factory.StartNew(() => findPattern(data, 14, "? ? 9afdffffc747302f000000c7471803000000")),
			Task.Factory.StartNew(() => findPattern(data, 12, "? ? 9afdffffc747302f000000c7471803000000"))
		}, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1) ? new Patch[1]
			{
				new Patch(source.First(), "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
			} : null;
		});
	}

	private static Task<Patch[]> findCertpin_nearjump_new(byte[] data)
	{
		return Task.Factory.StartNew(() => findPattern(data, 4, "0f84 ? fdffff4584f6 ? ? ? fdffff")).ContinueWith((Task<int[]> task) => (task.Result.Length == 1) ? new Patch[1]
		{
			new Patch(task.Result[0] + 9, "0F85", "90E9", MemProtection.PAGE_EXECUTE_READ)
		} : null);
	}

	private static Task<Patch[]> findCertpin_shortjump(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[2]
		{
			Task.Factory.StartNew(() => findPattern(data, 3, "? 0ec746302f000000c7461803000000")),
			Task.Factory.StartNew(() => findPattern(data, 2, "? 0ec746302f000000c7461803000000"))
		}, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1) ? new Patch[1]
			{
				new Patch(source.First(), "75", "EB", MemProtection.PAGE_EXECUTE_READ)
			} : null;
		});
	}

	private static Task<Patch[]> findAuthhead3_210(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[4]
		{
			Task.Factory.StartNew(() => findPattern(data, 12, "75084883f90675eceb22")),
			Task.Factory.StartNew(() => findPattern(data, 12, "90904883f90675eceb22")),
			Task.Factory.StartNew(() => findPattern(data, 2, "0f84c20000004584ed0f85")),
			Task.Factory.StartNew(() => findPattern(data, 2, "9090909090904584ed0f85"))
		}, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.Take(2).SelectMany((Task<int[]> task) => task.Result);
			IEnumerable<int> source2 = tasks.Skip(2).Take(2).SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1 && source2.Count() == 1) ? new Patch[2]
			{
				new Patch(source.First(), "7508", "9090", MemProtection.PAGE_EXECUTE_READ),
				new Patch(source2.First(), "0F84C2000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
			} : null;
		});
	}

	private static Task<Patch[]> findAuthhead3_30(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[4]
		{
			Task.Factory.StartNew(() => findPattern(data, 13, "0f85b50000004883f90675e8")),
			Task.Factory.StartNew(() => findPattern(data, 13, "9090909090904883f90675e8")),
			Task.Factory.StartNew(() => findPattern(data, 3, "0f84b800000084db0f85b0000000")),
			Task.Factory.StartNew(() => findPattern(data, 3, "90909090909084db0f85b0000000"))
		}, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.Take(2).SelectMany((Task<int[]> task) => task.Result);
			IEnumerable<int> source2 = tasks.Skip(2).Take(2).SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1 && source2.Count() == 1) ? new Patch[2]
			{
				new Patch(source.First(), "0F85B5000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
				new Patch(source2.First(), "0F84B8000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
			} : null;
		});
	}

	private static Task<Patch[]> findAuthhead2_72(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[7]
		{
			Task.Factory.StartNew(() => findPattern(data, 8, "? 18488d15ff02cd00")),
			Task.Factory.StartNew(() => findPattern(data, 8, "? 18488d155fd6cc00")),
			Task.Factory.StartNew(() => findPattern(data, 7, "? 18488d152018cc00")),
			Task.Factory.StartNew(() => findPattern(data, 12, "0f84860000004584e4")),
			Task.Factory.StartNew(() => findPattern(data, 12, "9090909090904584e4")),
			Task.Factory.StartNew(() => findPattern(data, 11, "0f84830000004584e4")),
			Task.Factory.StartNew(() => findPattern(data, 11, "9090909090904584e4"))
		}, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.Take(3).SelectMany((Task<int[]> task) => task.Result);
			IEnumerable<int> source2 = tasks.Skip(3).Take(4).SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1 && source2.Count() == 1) ? new Patch[2]
			{
				new Patch(source.First(), "75", "EB", MemProtection.PAGE_EXECUTE_READ),
				new Patch(source2.First(), "0F8486000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
			} : null;
		});
	}

	private static Task<Patch[]> findAuthhead1_15(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[4]
		{
			Task.Factory.StartNew(() => findPattern(data, 5, "0f84b3000000498bcf")),
			Task.Factory.StartNew(() => findPattern(data, 5, "909090909090498bcf")),
			Task.Factory.StartNew(() => findPattern(data, 5, "0f84a30000004584ed")),
			Task.Factory.StartNew(() => findPattern(data, 5, "9090909090904584ed"))
		}, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.Take(2).SelectMany((Task<int[]> task) => task.Result);
			IEnumerable<int> source2 = tasks.Skip(2).Take(2).SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1 && source2.Count() == 1) ? new Patch[2]
			{
				new Patch(source.First(), "0F84B3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ),
				new Patch(source2.First(), "0F84A3000000", "909090909090", MemProtection.PAGE_EXECUTE_READ)
			} : null;
		});
	}

	private static Task<Patch[]> findConfigdomain(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[4]
		{
			Task.Factory.StartNew(() => findPattern(data, 14, "488905 ? ? ? ? 488d0d ? ? ? ? 4883c4205b48ff25 ? ? ? 01")).ContinueWith((Task<int[]> task) => task.Result.Select((int addr) => addr + 14 + BitConverter.ToInt32(data, addr + 10)).ToArray()),
			Task.Factory.StartNew(() => findPattern(data, 13, "83f06e69d093010001e8 ? ? 0400488d05 ? ? ? 01ba000100004c8d05 ? ? ? 01488905 ? ? ? 02488d0d ? ? ? 02")).ContinueWith((Task<int[]> task) => task.Result.Select((int addr) => addr + 47 + BitConverter.ToInt32(data, addr + 43)).ToArray()),
			Task.Factory.StartNew(() => findPattern(data, 13, "83f16e69d193010001488d0d ? ? ? 02e8 ? ? 0400488d05 ? ? ? 01ba000100004c8d05 ? ? ? 01488905 ? ? ? 02488d0d ? ? ? 02")).ContinueWith((Task<int[]> task) => task.Result.Select((int addr) => addr + 54 + BitConverter.ToInt32(data, addr + 50)).ToArray()),
			Task.Factory.StartNew(() => findPattern(data, 0, "4883ec28baa71ace87488d0d ? ? ? 02e8 ? ? 0200488d05 ? ? ? 01ba000100004c8d05 ? ? ? 01488905 ? ? ? 02488d0d ? ? ? 02")).ContinueWith((Task<int[]> task) => task.Result.Select((int addr) => addr + 54 + BitConverter.ToInt32(data, addr + 50)).ToArray())
		}, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1) ? new Patch[1]
			{
				new Patch(source.First(), "", "", MemProtection.PAGE_READWRITE, "configdomain")
			} : null;
		});
	}

	private static Task<Patch[]> findHttpsString(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[4]
		{
			Task.Factory.StartNew(() => findPattern(data, 8, "68747470733a2f2f7b307d00")),
			Task.Factory.StartNew(() => findPattern(data, 8, "687474703a2f2f7b307d00")),
			Task.Factory.StartNew(() => findPattern(data, 0, "68747470733a2f2f7b307d00")),
			Task.Factory.StartNew(() => findPattern(data, 0, "687474703a2f2f7b307d00"))
		}, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1) ? new Patch[1]
			{
				new Patch(source.First(), Patch.https, Patch.http, MemProtection.PAGE_READONLY)
			} : null;
		});
	}

	private static Task<Patch[]> findHttpsLengths1_16(byte[] data)
	{
		return Task.Factory.StartNew(() => findPattern(data, 12, "c745B7 ? 000080488d05 ? ? ? 0148894dd7")).ContinueWith((Task<int[]> task) => (task.Result.Length == 1) ? new Patch[1]
		{
			new Patch(task.Result[0] + 3, "0B", "0A", MemProtection.PAGE_EXECUTE_READ)
		} : null);
	}

	private static Task<Patch[]> findHttpsLengthsPre3_30(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[2]
		{
			Task.Factory.StartNew(() => findPattern(data, 12, "488d55c7488d4d17e8 ? ? ? ff41b8")),
			Task.Factory.StartNew(() => findPattern(data, 5, "488d55c7488d4d17e8 ? ? ? ff41b8"))
		}, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1) ? new Patch[1]
			{
				new Patch(source.First() + 15, "0C", "0B", MemProtection.PAGE_EXECUTE_READ)
			} : null;
		});
	}

	private static Task<Patch[]> findHttpsLengths3_30(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[2]
		{
			Task.Factory.StartNew(() => findPattern(data, 6, "c74547 ? 0000800fbae81f")),
			Task.Factory.StartNew(() => findPattern(data, 9, "85d2750cc703 ? 000080"))
		}, delegate(Task<int[]>[] tasks)
		{
			int[][] array = tasks.Select((Task<int[]> task) => task.Result).ToArray();
			return (array[0].Length == 1 && array[1].Length == 1) ? new Patch[2]
			{
				new Patch(array[0][0] + 3, "0B", "0A", MemProtection.PAGE_EXECUTE_READ),
				new Patch(array[1][0] + 6, "0B", "0A", MemProtection.PAGE_EXECUTE_READ)
			} : null;
		});
	}

	private static Task<Patch[]> findHttpsLength3_210(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[1] { Task.Factory.StartNew(() => findPattern(data, 7, "488945d741b8 ? 000080 448945b7")) }, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1) ? new Patch[1]
			{
				new Patch(source.First() + 6, "0B", "0A", MemProtection.PAGE_EXECUTE_READ)
			} : null;
		});
	}

	private static Task<Patch[]> findProtocolCombined(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<Patch[]>[5]
		{
			findHttpsString(data),
			findHttpsLengths1_16(data),
			findHttpsLengthsPre3_30(data),
			findHttpsLengths3_30(data),
			findHttpsLength3_210(data)
		}, delegate(Task<Patch[]>[] tasks)
		{
			if (tasks[0].Result == null)
			{
				return (Patch[])null;
			}
			IEnumerable<Patch[]> source = from t in tasks.Skip(1)
				select t.Result into result
				where result != null
				select result;
			return (source.Count() != 1) ? null : tasks[0].Result.Concat(source.First()).ToArray();
		});
	}

	private static Task<Patch[]> findDynresForceoffline(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[5]
		{
			Task.Factory.StartNew(() => findPattern(data, 4, "baa7935217488d0d")).ContinueWith((Task<int[]> task) => task.Result.Select((int addr) => addr + 34 + BitConverter.ToInt32(data, addr + 26)).ToArray()),
			Task.Factory.StartNew(() => findPattern(data, 1, "83f17269c193010001488d0d ? ? ? 0383f06569d093010001e8 ? ? 0400488d05 ? ? ? 01c705 ? ? ? 0301000000")).ContinueWith((Task<int[]> task) => task.Result.Select((int addr) => addr + 47 + BitConverter.ToInt32(data, addr + 39)).ToArray()),
			Task.Factory.StartNew(() => findPattern(data, 11, "83f17269c193010001488d0d ? ? ? 0283f06569d093010001e8 ? ? 0400488d05 ? ? ? 01c705 ? ? ? 0201000000")).ContinueWith((Task<int[]> task) => task.Result.Select((int addr) => addr + 47 + BitConverter.ToInt32(data, addr + 39)).ToArray()),
			Task.Factory.StartNew(() => findPattern(data, 7, "83f17269c193010001488d0d ? ? ? 0283f06569d093010001e8 ? ? 0400488d05 ? ? ? 01c705 ? ? ? 0201000000")).ContinueWith((Task<int[]> task) => task.Result.Select((int addr) => addr + 47 + BitConverter.ToInt32(data, addr + 39)).ToArray()),
			Task.Factory.StartNew(() => findPattern(data, 4, "ba2734ff12488d0d ? ? ? 02e8 ? ? 0200488d05 ? ? ? 01c705 ? ? ? 0201000000")).ContinueWith((Task<int[]> task) => task.Result.Select((int addr) => addr + 34 + BitConverter.ToInt32(data, addr + 26)).ToArray())
		}, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1) ? new Patch[1]
			{
				new Patch(source.First(), "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
			} : null;
		});
	}

	private static Task<Patch[]> findDynresEnable(byte[] data)
	{
		return Task.Factory.ContinueWhenAll(new Task<int[]>[1] { Task.Factory.StartNew(() => findPattern(data, 4, "ba502e23f1488d0d")).ContinueWith((Task<int[]> task) => task.Result.Select((int addr) => addr + 34 + BitConverter.ToInt32(data, addr + 26)).ToArray()) }, delegate(Task<int[]>[] tasks)
		{
			IEnumerable<int> source = tasks.SelectMany((Task<int[]> task) => task.Result);
			return (source.Count() == 1) ? new Patch[1]
			{
				new Patch(source.First(), "01", "00", MemProtection.PAGE_EXECUTE_READWRITE)
			} : null;
		});
	}

	private static int[] findPattern(byte[] data, byte alignment, string pattern)
	{
		List<int> list = new List<int>();
		int num = 0;
		List<byte> list2 = new List<byte>(pattern.Length);
		List<int> list3 = new List<int>(pattern.Length);
		pattern = pattern.Replace(" ", "");
		for (int i = 0; i < pattern.Length; i += 2)
		{
			string text = pattern.Substring(i, 2);
			if (text.StartsWith("?"))
			{
				if (list3.Count == 0)
				{
					alignment++;
					num--;
					i--;
					continue;
				}
				list3.Add(1);
				int num2 = list3.Count - 2;
				while (list3[num2] > 0)
				{
					list3[num2]++;
					num2--;
				}
				list2.Add(0);
				i--;
			}
			else
			{
				list3.Add(0);
				list2.Add(Convert.ToByte(text, 16));
			}
		}
		byte[] array = list2.ToArray();
		int[] array2 = list3.ToArray();
		int num3 = array.Length;
		int num4 = data.Length - num3;
		byte b = array[0];
		for (int j = alignment; j < num4; j += 16)
		{
			if (data[j] != b)
			{
				continue;
			}
			int num5 = 1;
			num5 += array2[num5];
			while (data[j + num5] == array[num5])
			{
				num5++;
				if (num5 == num3)
				{
					list.Add(j + num);
					break;
				}
				num5 += array2[num5];
			}
		}
		return list.ToArray();
	}
}
