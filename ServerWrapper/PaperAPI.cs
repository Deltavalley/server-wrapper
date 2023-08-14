using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YamlDotNet.Serialization.BufferedDeserialization;

namespace ServerWrapper
{
	class PaperAPI
	{
		HttpClient client;
		public PaperAPI() 
		{
			client = new HttpClient();
			client.BaseAddress = new Uri("https://api.papermc.io/");
			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(
				new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json") );
		}

		public async Task<ProjectResponse> GetPaperVersions()
		{
			return await client.GetFromJsonAsync<ProjectResponse>("v2/projects/paper");
		}

		public async Task<VersionResponse> GetBuildsForVersion(string version)
		{
			return await client.GetFromJsonAsync<VersionResponse>($"v2/projects/paper/versions/{version}");
		}

		public async Task<BuildsResponse> GetFullBuildsForVersion(string version)
		{
			return await client.GetFromJsonAsync<BuildsResponse>($"v2/projects/paper/versions/{version}/builds");
		}

		public async Task DownloadServerJar(string version, int build, string filename)
		{
			string downloadFileURL = $"https://api.papermc.io/v2/projects/paper/versions/{version}/builds/{build}/downloads/{filename}";
			string filepath = Path.GetFullPath("server.jar");

			var downloadClient = new HttpClientDownloadWithProgress(downloadFileURL, filepath);
			downloadClient.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
			{
				string bytesDownloaded = totalBytesDownloaded.ToString();
				bytesDownloaded = bytesDownloaded.PadLeft(totalFileSize.ToString().Length);
				Console.WriteLine(string.Format("{0,6:F2}%   ({1}/{2:G})",progressPercentage, bytesDownloaded, totalFileSize));
			};

			await downloadClient.StartDownload();
		}
	}

	//shameless copy from https://stackoverflow.com/questions/20661652/progress-bar-with-httpclient -- https://stackoverflow.com/users/439094/ren%c3%a9-sackers
	public class HttpClientDownloadWithProgress : IDisposable
	{
		private readonly string _downloadUrl;
		private readonly string _destinationFilePath;

		private HttpClient _httpClient;

		public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

		public event ProgressChangedHandler ProgressChanged;

		public HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath)
		{
			_downloadUrl = downloadUrl;
			_destinationFilePath = destinationFilePath;
		}

		public async Task StartDownload()
		{
			_httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };

			using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
				await DownloadFileFromHttpResponseMessage(response);
		}

		private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
		{
			response.EnsureSuccessStatusCode();

			var totalBytes = response.Content.Headers.ContentLength;

			using (var contentStream = await response.Content.ReadAsStreamAsync())
				await ProcessContentStream(totalBytes, contentStream);
		}

		private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
		{
			var totalBytesRead = 0L;
			var readCount = 0L;
			var buffer = new byte[8192];
			var isMoreToRead = true;

			using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
			{
				do
				{
					var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
					if (bytesRead == 0)
					{
						isMoreToRead = false;
						TriggerProgressChanged(totalDownloadSize, totalBytesRead);
						continue;
					}

					await fileStream.WriteAsync(buffer, 0, bytesRead);

					totalBytesRead += bytesRead;
					readCount += 1;

					if (readCount % 100 == 0)
						TriggerProgressChanged(totalDownloadSize, totalBytesRead);
				}
				while (isMoreToRead);
			}
		}

		private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
		{
			if (ProgressChanged == null)
				return;

			double? progressPercentage = null;
			if (totalDownloadSize.HasValue)
				progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);

			ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
		}

		public void Dispose()
		{
			_httpClient?.Dispose();
		}
	}
}
