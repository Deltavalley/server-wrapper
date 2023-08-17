using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ServerWrapper.ConsoleHelper;
using static ServerWrapper.MyConsts;
using YamlDotNet.Serialization;

namespace ServerWrapper
{
	public static class ConfigManager
	{
		public static Config LoadConfig()
		{
			//access config file
			string content = File.ReadAllText(CONFIG_NAME);

			//deserialize and return
			Deserializer deserializer = new();
			return deserializer.Deserialize<Config>(content);
		}

		public static Config GenerateConfig()
		{
			//Console.WriteLine("Generating config file . . .");

			//generate and return a new config specified by the user
			Config config = new();

			Predicate<string> javaPathIsValid = (x) =>
			{
				return x == "" || File.Exists(x);
			};
			Converter<string, string> convertBlank = (x) => { return (x == "") ? "java" : x; };
			config.javaPath = VerifyConsoleInput("Input java.exe full path (optional):", javaPathIsValid, convertBlank, true);

			config.useAikarsFlags = VerifyConsoleInput("Use Aikar's flags? (true/false):", (string x) => bool.TryParse(x, out _), (string x) => bool.Parse(x));

			Predicate<string> memoryIsValid = (string x) =>
			{
				int num;
				if (int.TryParse(x, out num))
				{
					return num > 0;
				}
				return false;
			};
			config.memoryGB = VerifyConsoleInput("Memory (GB):", memoryIsValid, (string x) => int.Parse(x));

			Serializer serializer = new Serializer();
			string configString = serializer.Serialize(config);

			using (StreamWriter sw = new(CONFIG_NAME))
			{
				sw.Write(configString);
			}

			return config;
		}
	}
}
