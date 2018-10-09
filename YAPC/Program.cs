using System;
using System.Security;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;

namespace YAPC
{
	public class Program
	{
		private static string _testComputer = string.Empty;
		private static string _domain = "testad";
		public static void Main(string[] args)
		{
			string actionChoice = string.Empty;
			Console.WriteLine($"Enter the name of the computer to connect to. If the comptuer is on different domain, try the FQDN.");
			Console.WriteLine($"If no name is enterd, the local machine is used.");
			Console.Write("Computer Name: ");
			_testComputer = Console.ReadLine();
			if (string.IsNullOrEmpty(_testComputer))
			{
				_testComputer = Environment.MachineName;
			}
			Console.WriteLine($"Reading Registry from {_testComputer}.");

			YAPCPatchManager pm = new YAPCPatchManager(_testComputer);
			pm.ReadRegistry();
			pm.GetUnneededFilesDetails();
			pm.ReviewUnneededFiles();

			Console.WriteLine("Reading Registry complete");
			Console.WriteLine("What would you like to do?");
			Console.WriteLine("(d) - Delete Files");
			Console.WriteLine("(m) - Move Files");
			Console.WriteLine("(e) - Exit");

			actionChoice = Console.ReadLine();
			if (actionChoice == "d")
			{
				pm.DeleteUnneededFiles();
				Console.WriteLine("Successfully deleted orphaned files. Hit any key to close");
				Console.ReadLine();
			}
			else if (actionChoice == "m")
			{
				Console.WriteLine(
					"Where would you like to move the files? Enter the path as the administrative share (\\\\computername.domain.com\\c$\\temp).");
				pm.MoveUnneededFiles(Console.ReadLine());
				Console.WriteLine("Successfully moved orphaned files. Hit any key to close");
				Console.ReadLine();
			}
		}
	}
}