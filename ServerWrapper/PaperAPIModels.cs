using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Transactions;
using static System.Net.WebRequestMethods;

namespace ServerWrapper
{
	public record class ProjectResponse(string project_id, string project_name, string[] version_groups, string[] versions);
	public record class VersionResponse(string project_id, string project_name, string[] version, int[] builds);
	public record class BuildsResponse(string project_id, string project_name, string version, VersionBuild[] builds);
		public record class VersionBuild(int build, DateTime time, string channel, bool promoted, Change[] changes, Dictionary<string,Download> downloads);
		public record class Change(string commit, string summary, string message);
		public record class Download(string name, string sha256);
}
