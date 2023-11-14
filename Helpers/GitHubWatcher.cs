using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace groveale 
{

    public class GitHubWatcher
    {
        private readonly HttpClient httpClient;
        private readonly string _repoOwner;
        private readonly string _repoName;
        private readonly string _folderPath;
        private readonly string _extension;

        public GitHubWatcher(string repoOwner, string repoName, string folderPath, string extension)
        {
            _repoOwner = repoOwner;
            _repoName = repoName;
            _folderPath = folderPath;
            _extension = extension;

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("groveale", "1.0"));
            // string token = Environment.GetEnvironmentVariable("personalAccessToken");
            // string b64Pat = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{token}"));
            // httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", b64Pat);
        }


        public async Task<CommitFilesItem> GetCommit(string id)
        {
            var apiUrl = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/commits/{id}";
            var response = await httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();
            var commit = await response.Content.ReadAsAsync<CommitFilesItem>();
            
            // Filter out files that don't match the extension or file path
            if (!string.IsNullOrEmpty(_extension))
            {
                commit.Files.RemoveAll(f => !f.FileName.EndsWith(_extension));
            }

            if (!string.IsNullOrEmpty(_folderPath))
            {
                commit.Files.RemoveAll(f => !f.FileName.StartsWith(_folderPath));
            }
              
            return commit;
        }

        public async Task<List<string>> GetCommitsInLast24HoursAsync()
        {

            var apiUrl = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/commits";
            if (!string.IsNullOrEmpty(_folderPath))
            {
                apiUrl += $"?path={_folderPath}";
            }
            var response = await httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();
            var commits = await response.Content.ReadAsAsync<List<CommitItem>>();
            var commitsInLast24Hours = new List<string>();
            foreach (var commit in commits)
            {
                if (DateTimeOffset.UtcNow - commit.Commit.Author.Date <= TimeSpan.FromHours(24))
                {
                    commitsInLast24Hours.Add(commit.Sha);
                }
            }
            return commitsInLast24Hours;
        }

        public async Task<List<CommitFilesItem>> GetCommitDetails(List<string> commitIds)
        {
            var commitDetails = new List<CommitFilesItem>();

            foreach (var commitId in commitIds)
            {
                var commit = await GetCommit(commitId);
                commitDetails.Add(commit);
            }

            return commitDetails;
        }

        public class CommitItem
        {
            public string Sha { get; set; }
            public CommitDetails Commit { get; set; }
        }

        public class CommitDetails
        {
            public CommitAuthor Author { get; set; }
            public string Message { get; set; }
        }

        public class CommitAuthor
        {
            public DateTimeOffset Date { get; set; }
        }

        public class CommitFilesItem
        {
            public string Sha { get; set; }
            public CommitDetails Commit { get; set; }

            public List<CommitFile> Files { get; set; }
        }

        public class CommitFile
        {
            public string FileName { get; set; }
            public string Status { get; set; }
        }

        public class CommitDetailsResponse
        {
            public List<CommitFilesItem> RecentCommits { get; set; }
        }
    }
}
