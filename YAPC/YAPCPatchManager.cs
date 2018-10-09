using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Management.Infrastructure;
using Microsoft.Win32;

namespace YAPC
{
	public class YAPCPatchManager
	{
		private static string _computerName;
		private List<Registry_PatchCodeModel> _nonAppliedPatchCodes = new List<Registry_PatchCodeModel>();
		private List<string> _filesToKeep = new List<string>();
		private List<string> _appliedPatches = new List<string>();
		private List<FileToDeleteModel> _filesToDelete = new List<FileToDeleteModel>();
		private RegistryKey _patchRegistryKey;

		public YAPCPatchManager(string computerName)
		{
			_computerName = computerName;
		}

		public void ReadRegistry()
		{
			RegistryKey rkey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, _computerName, RegistryView.Registry64);
			RegistryKey rkeySoftware = rkey.OpenSubKey("Software");
			RegistryKey rKeyMicro = rkeySoftware.OpenSubKey("Microsoft");
			RegistryKey rKeyWindows = rKeyMicro.OpenSubKey("Windows");
			RegistryKey rKeyCurrentVersion = rKeyWindows.OpenSubKey("CurrentVersion");
			RegistryKey rKeyInstaller = rKeyCurrentVersion.OpenSubKey("Installer");
			RegistryKey rKeyUserData = rKeyInstaller.OpenSubKey("UserData");
			RegistryKey rKeyS1 = rKeyUserData.OpenSubKey("S-1-5-18");
			RegistryKey rKeyProducts = rKeyS1.OpenSubKey("Products");
			RegistryKey rKeyPatches = rKeyS1.OpenSubKey("Patches",true);

			List<string> productGuids = rKeyProducts.GetSubKeyNames().ToList(); //get the product SQUIDs
			foreach (string guid in productGuids)
			{
				RegistryKey rKeyProduct = rKeyProducts.OpenSubKey(guid); //current product key
				RegistryKey rKeyProductPatches = rKeyProduct.OpenSubKey("Patches"); //patches subkey

				string[] activePatches = rKeyProductPatches.GetValue("AllPatches") as string[]; //all patches == applied patches
				string[] productPatchSubKeys = rKeyProductPatches.GetSubKeyNames(); //AllPatches multi string value holds the SQUIDs for active/applied/registered/etc patches.

				foreach (string k in productPatchSubKeys)
				{
					_appliedPatches.Add(k);
					if (!activePatches.Contains(k)) //not an applied patch, its an orphaned file
					{
						RegistryKey rKeyinstallProperties = rKeyProduct.OpenSubKey("InstallProperties");
						RegistryKey rKeyProductPatch = rKeyProductPatches.OpenSubKey(k);
						string uninstallString = rKeyinstallProperties.GetValue("UninstallString").ToString();
						string msiExec = "MsiExec.exe /X";
						Registry_PatchCodeModel patchModel = new Registry_PatchCodeModel();
						patchModel.ProductCode = uninstallString.Remove(0, msiExec.Length).Replace("{", "").Replace("}","");
						patchModel.PatchCode = k;
						patchModel.ProductName = rKeyinstallProperties.GetValue("DisplayName").ToString();
						patchModel.ProductVersion = rKeyinstallProperties.GetValue("DisplayVersion").ToString();
						patchModel.ProductPublisher = rKeyinstallProperties.GetValue("Publisher").ToString();
						patchModel.PatchName = rKeyProductPatch.GetValue("DisplayName").ToString();
						_nonAppliedPatchCodes.Add(patchModel);
					}
				}
			}

			List<string> patchCodes = rKeyPatches.GetSubKeyNames().ToList();
			foreach (string pCode in patchCodes) //for each key under UserData\S-1-5-18\Patches
			{
				Registry_PatchCodeModel patchModel = _nonAppliedPatchCodes.FirstOrDefault(p => p.PatchCode == pCode);
				if (patchModel != null)
				{
					RegistryKey rKeyPCode = rKeyPatches.OpenSubKey(pCode);
					string newLocation = rKeyPCode.GetValue("LocalPackage").ToString().Replace("C:\\", $@"\\{_computerName}\C$\");
					patchModel.FilePath = newLocation;
				}
				else
				{
					RegistryKey rKeyPCode = rKeyPatches.OpenSubKey(pCode);
					string newLocation = rKeyPCode.GetValue("LocalPackage").ToString().Replace("C:\\", $@"\\{_computerName}\C$\");
					_filesToKeep.Add(newLocation);
				}
			}
		}

		public void DeleteUnneededFiles()
		{
			foreach (FileToDeleteModel fileTD in _filesToDelete)
			{
				if (File.Exists(fileTD.FullPath))
				{
					File.SetAttributes(fileTD.FullPath, FileAttributes.Normal);
					File.Delete(fileTD.FullPath);
				}
			}
		}

		public void MoveUnneededFiles(string newDirectory)
		{
			if (!Directory.Exists(newDirectory))
			{
				Directory.CreateDirectory(newDirectory);
			}
			foreach (FileToDeleteModel fileM in _filesToDelete)
			{
				FileInfo fi = new FileInfo(fileM.FullPath);
				if (File.Exists(fileM.FullPath))
				{
					File.SetAttributes(fileM.FullPath, FileAttributes.Normal);
					
					File.Move(fileM.FullPath, newDirectory+@"\"+fileM.Name);
				}
			}
		}

		public void GetUnneededFilesDetails()
		{
			foreach (Registry_PatchCodeModel patch in _nonAppliedPatchCodes.Where(pc => !string.IsNullOrEmpty(pc.FilePath)))
			{
				FileInfo fi = new FileInfo(patch.FilePath);
				if (File.Exists(patch.FilePath))
				{
					FileToDeleteModel fm = new FileToDeleteModel();
					fm.Name = fi.Name;
					fm.SizeInBytes = fi.Length;
					fm.Location = fi.Directory.FullName;
					fm.FullPath = fi.FullName;
					fm.ProductCode = patch.ProductCode;
					fm.PatchCode = patch.PatchCode;
					fm.ProductName = patch.ProductName;
					fm.ProductVersion = patch.ProductVersion;
					fm.ProductPublisher = patch.ProductPublisher;
					fm.PatchName = patch.PatchName;
					_filesToDelete.Add(fm);
				}
			}
		}

		public void ReviewUnneededFiles()
		{
			long totalBytes = 0;
			Console.BackgroundColor = ConsoleColor.Blue;
			List<string> distinctProductNames = _filesToDelete.Select(f => $"{f.ProductPublisher} - {f.ProductName} - {f.ProductVersion}").Distinct().ToList();
			foreach(string dpn in distinctProductNames)
			{
				Console.WriteLine($"{dpn}");
				List<FileToDeleteModel> productFilesToDelete = _filesToDelete.Where(f => $"{f.ProductPublisher} - {f.ProductName} - {f.ProductVersion}" == dpn).ToList();
				foreach (FileToDeleteModel p in productFilesToDelete)
				{
					Console.WriteLine($"\t{p.PatchName} : {p.SizeInBytes / 1024 / 1024} MB");
					Console.WriteLine($"\t\t {p.FullPath}");
					totalBytes += p.SizeInBytes;
				}
			}

			Console.WriteLine($"Total Orphaned Files: {_filesToDelete.Count}");
			Console.WriteLine($"Total Size of Orphaned Files: {totalBytes / 1024 / 1024} MB");
			Console.BackgroundColor = ConsoleColor.Black;
		}
	}
}