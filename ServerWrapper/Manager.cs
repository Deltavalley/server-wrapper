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

namespace ServerWrapper
{
	//we want to put this exe in a directory with a paper file called "server.jar" and it runs the server etc

	class Manager
	{
		static void Main(string[] args)
		{
			Splash();

			//setting up new server
			if (IsNewServer())
			{
				//rename jar file to "server.jar"
				RenameServerJar();

				Console.WriteLine("No server found, creating a new one\n");

				//generate config
				Config config = GenerateConfig();

				//ask and set eula
				SetEula();
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
			Console.WriteLine("\n\nPress any key to close the wrapper");
			Console.ReadKey(true);
		}

		static void SetEula()
		{
			bool eulaAgreement = VerifyConsoleInput("Do you agree to the minecraft EULA as listed at \"https://aka.ms/MinecraftEULA\" (true/false) ",
				(x) => bool.TryParse(x, out _),(x) => bool.Parse(x));

			if (eulaAgreement)
			{
				using StreamWriter sw = new("eula.txt");
				sw.WriteLine("eula=true");
			}
			else
			{
				Console.WriteLine("You did not agree to the EULA.  Exiting");
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
			string command = $"\"{config.javaPath}\\bin\\java.exe\" {flags} -jar server.jar nogui";

			Console.WriteLine("==================================");
			System.Diagnostics.Process.Start("cmd.exe", $"/c {command}").WaitForExit();
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
			return Directory.GetFiles(".", "wrapper-config.yml", SearchOption.TopDirectoryOnly).Length == 0;
		}

		static void Splash()
		{
			Console.WriteLine($"----------- Minecraft Server Wrapper v{VERSION} -----------\n");
			Console.WriteLine($"By Deltavalley\n\n\n");
			Console.WriteLine($"Press any key to continue . . .");
			Console.ReadKey(true);
			Console.Clear();
		}
	}
}