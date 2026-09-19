using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace HitmanPatcher;

internal class Program
{
	private const string APP_MUTEX_NAME = "Global\\HitmanLauncherMutex";

	private const string GAME_PROCESS_NAME = "HITMAN3";

	private const string PEACOCK_DIR_NAME = "Peacock";

	private const int DEFAULT_PORT = 47000;

	private const int GAME_EXIT_WAIT_MS = 4000;

	private const int PATCH_RETRY_DELAY_MS = 1000;

	private const uint MB_OK_ICONERROR = 16u;

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

	private static void Main(string[] args)
	{
		try
		{
			Run(args);
		}
		catch (Exception ex)
		{
			ShowError("Launcher error: " + ex.Message);
		}
	}

	private static void Run(string[] args)
	{
		if (args.Length == 0)
		{
			ShowError("Please run the game using Steam or HITMAN3.exe instead.");
			return;
		}
		if (!int.TryParse(args[0], out var result))
		{
			ShowError("Invalid parent process PID format: '" + args[0] + "'");
			return;
		}
		Process parentProcess = GetParentProcess(result);
		if (parentProcess == null)
		{
			return;
		}
		using (parentProcess)
		{
			using Mutex mutex = new Mutex(initiallyOwned: false, "Global\\HitmanLauncherMutex");
			if (!AcquireMutex(mutex))
			{
				return;
			}
			try
			{
				ExecuteLauncher(parentProcess);
			}
			finally
			{
				mutex.ReleaseMutex();
			}
		}
	}

	private static Process GetParentProcess(int pid)
	{
		try
		{
			Process processById = Process.GetProcessById(pid);
			if (processById.ProcessName.Equals("HITMAN3", StringComparison.OrdinalIgnoreCase))
			{
				return processById;
			}
			ShowError(string.Format("The parent process (PID: {0}, Name: {1}) is not {2}.exe.", pid, processById.ProcessName, "HITMAN3"));
			return null;
		}
		catch (Exception ex)
		{
			ShowError("Failed to get the parent process: " + ex.Message);
			return null;
		}
	}

	private static bool AcquireMutex(Mutex mutex)
	{
		try
		{
			return mutex.WaitOne(TimeSpan.Zero, exitContext: false) || mutex.WaitOne();
		}
		catch (AbandonedMutexException)
		{
			return true;
		}
	}

	private static void ExecuteLauncher(Process parentProcess)
	{
		int availablePort = GetAvailablePort();
		if (availablePort == -1)
		{
			ShowError("Could not find free TCP port.");
			return;
		}
		(string, string, string)? peacockPaths = GetPeacockPaths();
		if (!peacockPaths.HasValue)
		{
			return;
		}
		Process process = StartPeacockServer(peacockPaths.Value, availablePort);
		if (process == null)
		{
			return;
		}
		using (process)
		{
			if (PatchGame(parentProcess, availablePort))
			{
				WaitForGameExit(parentProcess, process);
			}
		}
	}

	private static (string nodeExe, string serverScript, string workingDir)? GetPeacockPaths()
	{
		try
		{
			DirectoryInfo directoryInfo = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent;
			if (directoryInfo == null)
			{
				ShowError("Could not determine the game's root directory.");
				return null;
			}
			string text = Path.Combine(directoryInfo.FullName, "Peacock");
			string text2 = Path.Combine(text, "nodedist", "node.exe");
			string text3 = Path.Combine(text, "chunk0.js");
			if (!File.Exists(text2))
			{
				ShowError("Peacock's node.exe not found at:\n'" + text2 + "'");
				return null;
			}
			if (File.Exists(text3))
			{
				return (text2, text3, text);
			}
			ShowError("Peacock's server script not found at:\n'" + text3 + "'");
			return null;
		}
		catch (Exception ex)
		{
			ShowError("Error locating Peacock files: " + ex.Message);
			return null;
		}
	}

	private static Process StartPeacockServer((string nodeExe, string serverScript, string workingDir) paths, int port)
	{
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = paths.nodeExe;
			processStartInfo.Arguments = "\"" + paths.serverScript + "\"";
			processStartInfo.WorkingDirectory = paths.workingDir;
			processStartInfo.CreateNoWindow = true;
			processStartInfo.UseShellExecute = false;
			processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			processStartInfo.EnvironmentVariables["HOST"] = "localhost";
			processStartInfo.EnvironmentVariables["PORT"] = port.ToString();
			processStartInfo.EnvironmentVariables["LOG_LEVEL_CONSOLE"] = "info";
			processStartInfo.EnvironmentVariables["LOG_CATEGORY_DISABLED"] = "";
			processStartInfo.EnvironmentVariables["LOG_MAX_FILES"] = "14d";
			Process process = Process.Start(processStartInfo);
			if (process != null && !process.HasExited)
			{
				return process;
			}
			ShowError("Failed to start the server process.");
			return null;
		}
		catch (Exception ex)
		{
			ShowError("Error starting server: " + ex.Message);
			return null;
		}
	}

	private static bool PatchGame(Process process, int port)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		Options val = new Options
		{
			AlwaysSendAuthHeader = true,
			CustomConfigDomain = $"localhost:{port}",
			DisableCertPinning = true,
			DisableForceOfflineOnFailedDynamicResources = true,
			EnableDynamicResources = true,
			SetCustomConfigDomain = true,
			UseHttp = true
		};
		while (!process.HasExited)
		{
			try
			{
				if (MemoryPatcher.Patch(process, val))
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				ShowError("Failed to patch game: " + ex.Message);
				return false;
			}
			Thread.Sleep(1000);
		}
		ShowError($"The game process (PID {process.Id}) exited before it could be patched.");
		return false;
	}

	private static void WaitForGameExit(Process gameProcess, Process peacockProcess)
	{
		try
		{
			gameProcess.WaitForExit();
			Thread.Sleep(4000);
		}
		finally
		{
			try
			{
				if (!peacockProcess.HasExited)
				{
					peacockProcess.Kill();
				}
			}
			catch
			{
			}
		}
	}

	private static int GetAvailablePort()
	{
		int num = TryGetPort(47000);
		if (num == -1)
		{
			return TryGetPort(0);
		}
		return num;
	}

	private static int TryGetPort(int requestedPort)
	{
		TcpListener tcpListener = null;
		try
		{
			tcpListener = new TcpListener(IPAddress.Loopback, requestedPort);
			tcpListener.Start();
			return ((IPEndPoint)tcpListener.LocalEndpoint).Port;
		}
		catch
		{
			return -1;
		}
		finally
		{
			tcpListener?.Stop();
		}
	}

	private static void ShowError(string message)
	{
		MessageBoxW(IntPtr.Zero, message, "Launcher", 16u);
	}
}
