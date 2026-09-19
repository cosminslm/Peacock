using System;
using System.Collections.Generic;
using System.IO;

namespace HitmanPatcher;

public class Settings
{
	public MemoryPatcher.Options patchOptions;

	public bool startInTray;

	public bool minimizeToTray;

	public bool darkModeEnabled;

	public List<string> trayDomains;

	public Settings()
	{
		patchOptions = new MemoryPatcher.Options
		{
			CustomConfigDomain = "127.0.0.1",
			UseHttp = true,
			DisableCertPinning = true,
			AlwaysSendAuthHeader = true,
			SetCustomConfigDomain = true,
			EnableDynamicResources = true,
			DisableForceOfflineOnFailedDynamicResources = true
		};
		darkModeEnabled = false;
		startInTray = false;
		minimizeToTray = false;
		trayDomains = new List<string>();
	}

	private static string GetSavePath()
	{
		if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PeacockProject"))
		{
			Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PeacockProject");
		}
		string text = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PeacockProject\\";
		string path = text + "peacock_patcher.conf";
		string result = text + "peacock_patcher2.conf";
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		return result;
	}

	public void SaveToFile()
	{
		List<string> list = new List<string>();
		list.Add($"CustomConfigDomain={patchOptions.CustomConfigDomain}");
		list.Add($"UseHttp={patchOptions.UseHttp}");
		list.Add($"DisableForceDynamicResources={patchOptions.DisableForceOfflineOnFailedDynamicResources}");
		list.Add($"DarkModeEnabled={darkModeEnabled}");
		list.Add($"startInTray={startInTray}");
		list.Add($"minToTray={minimizeToTray}");
		foreach (string trayDomain in trayDomains)
		{
			list.Add($"trayDomain={trayDomain}");
		}
		File.WriteAllLines(GetSavePath(), list);
	}

	public static Settings GetFromFile()
	{
		Settings settings = new Settings();
		if (File.Exists(GetSavePath()))
		{
			string[] array = File.ReadAllLines(GetSavePath());
			if (array.Length == 1)
			{
				settings.patchOptions.CustomConfigDomain = array[0];
			}
			else
			{
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (!(text == ""))
					{
						string[] array3 = text.Split(new char[1] { '=' }, 2);
						switch (array3[0])
						{
						case "CustomConfigDomain":
							settings.patchOptions.CustomConfigDomain = array3[1];
							break;
						case "UseHttp":
							settings.patchOptions.UseHttp = bool.Parse(array3[1]);
							break;
						case "DisableForceDynamicResources":
							settings.patchOptions.DisableForceOfflineOnFailedDynamicResources = bool.Parse(array3[1]);
							break;
						case "DarkModeEnabled":
							settings.darkModeEnabled = bool.Parse(array3[1]);
							break;
						case "startInTray":
							settings.startInTray = bool.Parse(array3[1]);
							break;
						case "minToTray":
							settings.minimizeToTray = bool.Parse(array3[1]);
							break;
						case "trayDomain":
							settings.trayDomains.Add(array3[1]);
							break;
						}
					}
				}
			}
		}
		return settings;
	}
}
