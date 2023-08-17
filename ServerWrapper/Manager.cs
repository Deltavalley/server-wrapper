using System.Reflection;
using YamlDotNet.Serialization;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Security;
using System.Runtime.CompilerServices;
using static ServerWrapper.ConfigManager;
using static ServerWrapper.MyConsts;
using static ServerWrapper.ConsoleHelper;
using YamlDotNet.Core.Events;
using System.Text;

namespace ServerWrapper
{
	class Manager
	{
		static async Task Main(string[] args)
		{
			Splash();

			//setting up new server
			if (IsNewServer())
			{
				Console.WriteLine($"No \"{CONFIG_NAME}\" found, generating a new one.\n");

				//generate config
				Config config = GenerateConfig();

				//get correct server jar from paper
				await GetPaperJarAsync();

				//rename jar file to "server.jar"
				//RenameServerJar();

				//ask and set eula
				SetEula();
			} else
			{
				Console.WriteLine($"\"{CONFIG_NAME}\" found, press any key to start the server.\n");
				Console.ReadKey(true);
			}
			
			//starting existing server
			bool restart = true;
			while (restart)
			{
				Console.Clear();
				//load config
				Config config = LoadConfig();

				//run server
				Console.WriteLine("Starting server");
				RunServer(config);
				Console.WriteLine("Server stopped");
				restart = VerifyConsoleInput("Restart server? (y/n) ", (x) => x.ToLower() == "y" || x.ToLower() == "n", (x) => x == "y");
			}
			Console.WriteLine("\n\nPress any key to close the wrapper.");
			Console.ReadKey(true);
		}

		static async Task GetPaperJarAsync()
		{
			PaperAPI api = new PaperAPI();

			//ask paper for the most recent versions
			Console.WriteLine("Fetching avalible minecraft versions.");
			var paperVersions = await api.GetPaperVersions();

			//print all valid minecraft versions
			Console.WriteLine("The following minecraft versions are avalible from paper:");
			StringBuilder sb = new();

			int height = paperVersions.versions.Length / 2;
			for (int i = 0; i < height; i++)
			{
				sb.Append(string.Format("{0,-10}{1,-10}\n", paperVersions.versions[i], paperVersions.versions[height + i]));
			}

			//add the missing bit of the left column if we need to (have an odd number of items)
			if (paperVersions.versions.Length % 2 != 0)
			{
				sb.Append($"{paperVersions.versions[height]}\n");
			}
			Console.WriteLine(sb);

			Predicate<string> isValidVersion = (x) =>
			{
				for (int i = 0; i < paperVersions.versions.Length; i++)
				{
					if (paperVersions.versions[i] == x)
					{
						return true;
					}
				}
				return false;
			};
			string selectedVersion = VerifyConsoleInput("Enter an avalible minecraft version:", isValidVersion);

			//find out the latest version number for the selected build
			Console.WriteLine($"Fetching latest build for {selectedVersion}.");
			var versionBuilds = await api.GetFullBuildsForVersion(selectedVersion);
			var latestBuild = versionBuilds.builds.Last();

			Console.WriteLine($"Latest build for {selectedVersion} is {latestBuild.build} ({latestBuild.time}).");

			Console.WriteLine($"Downloading.");
			await api.DownloadServerJar(selectedVersion, latestBuild.build, latestBuild.downloads["application"].name);
			Console.WriteLine($"Download finished.");

		}

		static void SetEula()
		{
			bool eulaAgreement = VerifyConsoleInput("Do you agree to the minecraft EULA as listed at \"https://aka.ms/MinecraftEULA\" (true/false)?",
				(x) => bool.TryParse(x, out _),(x) => bool.Parse(x));

			if (eulaAgreement)
			{
				using StreamWriter sw = new("eula.txt");
				sw.WriteLine("eula=true");
			}
			else
			{
				Console.WriteLine("You did not agree to the EULA.  Exiting.");
				Console.ReadKey(true);
				Environment.Exit(0);
			}
		}

		static void RenameServerJar()
		{
			string[] jars = Directory.GetFiles(".", "*.jar", SearchOption.TopDirectoryOnly);
			if (jars.Length == 0)
			{
				Console.WriteLine("No jar file found in current directory!");
				Console.ReadKey(true);
				Environment.Exit(0);
			}

			if (jars.Length >= 2)
			{
				Console.WriteLine("Too many jar files found in current directory!");
				Console.ReadKey(true);
				Environment.Exit(0);
			}

			File.Move(jars[0], ".\\server.jar");
		}
		
		static void RunServer(Config config)
		{
			string flags = GetAikarsFlags(config.memoryGB);
			string command = $"\"{config.javaPath}\" {flags} -jar server.jar nogui";

			Console.WriteLine("==================================");
			System.Diagnostics.Process.Start("cmd.exe", $"/c {command}").WaitForExit();
			//System.Diagnostics.Process.Start("cmd.exe", "/c echo %path%").WaitForExit();
			Console.WriteLine("==================================");
		}

		static string GetAikarsFlags(int memoryGB)
		{
			if (memoryGB >= 12)
			{
				//adjusted flags for large ram
				return $"-Xms{memoryGB}G -Xmx{memoryGB}G -XX:+UseG1GC -XX:+ParallelRefProcEnabled -XX:MaxGCPauseMillis=200 -XX:+UnlockExperimentalVMOptions -XX:+DisableExplicitGC -XX:+AlwaysPreTouch -XX:G1NewSizePercent=40 -XX:G1MaxNewSizePercent=50 -XX:G1HeapRegionSize=16M -XX:G1ReservePercent=15 -XX:G1HeapWastePercent=5 -XX:G1MixedGCCountTarget=4 -XX:InitiatingHeapOccupancyPercent=20 -XX:G1MixedGCLiveThresholdPercent=90 -XX:G1RSetUpdatingPauseTimePercent=5 -XX:SurvivorRatio=32 -XX:+PerfDisableSharedMem -XX:MaxTenuringThreshold=1 -Dusing.aikars.flags=https://mcflags.emc.gs -Daikars.new.flags=true";
			} else
			{
				//normal flags for less than 12gb of ram
				return $"-Xms{memoryGB}G -Xmx{memoryGB}G -XX:+UseG1GC -XX:+ParallelRefProcEnabled -XX:MaxGCPauseMillis=200 -XX:+UnlockExperimentalVMOptions -XX:+DisableExplicitGC -XX:+AlwaysPreTouch -XX:G1NewSizePercent=30 -XX:G1MaxNewSizePercent=40 -XX:G1HeapRegionSize=8M -XX:G1ReservePercent=20 -XX:G1HeapWastePercent=5 -XX:G1MixedGCCountTarget=4 -XX:InitiatingHeapOccupancyPercent=15 -XX:G1MixedGCLiveThresholdPercent=90 -XX:G1RSetUpdatingPauseTimePercent=5 -XX:SurvivorRatio=32 -XX:+PerfDisableSharedMem -XX:MaxTenuringThreshold=1 -Dusing.aikars.flags=https://mcflags.emc.gs -Daikars.new.flags=true";
			}
		}

		static bool IsNewServer()
		{
			//checks if a config file already exists, if it does, a server is also assumed to exist
			return Directory.GetFiles(".", CONFIG_NAME, SearchOption.TopDirectoryOnly).Length == 0;
		}

		static void Splash()
		{
			Console.WriteLine($"----------- Minecraft Server Wrapper v{VERSION} -----------\n");
			Console.WriteLine($"To reset the wrapper, delete the \"{CONFIG_NAME}\" file.\n");
			Console.WriteLine($"By Deltavalley\n\n\n");
			Console.WriteLine($"Press any key to begin . . .");
			Console.ReadKey(true);
			Console.Clear();
		}
	}
}