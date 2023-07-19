namespace ServerWrapper
{
	public class Config
	{
		public string javaPath { get; set; }
		public bool useAikarsFlags { get; set; }
		public int memoryGB { get; set; }

		public Config()
		{
			javaPath = "";
			useAikarsFlags = false;
			memoryGB = 0;
		}
	}
}
