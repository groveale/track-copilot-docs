using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace groveale
{
    public static class GetRepoUpdates
    {
        [FunctionName("GetRepoUpdates")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string repoOwner = req.Query["repoOwner"];
            string repoName = req.Query["repoName"];
            string folderPath = req.Query["folderPath"];
            string debugCommit = req.Query["debugCommit"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            repoOwner = repoOwner ?? data?.repoOwner;
            repoName = repoName ?? data?.repoName;
            folderPath = folderPath ?? data?.folderPath;
            debugCommit = debugCommit ?? data?.debugCommit;

            if (string.IsNullOrEmpty(repoOwner) || string.IsNullOrEmpty(repoName))
            {
                return new BadRequestObjectResult("Please pass a repoOwner, repoName on the query string or in the request body");
            }

            var watcher = new GitHubWatcher(repoOwner, repoName, folderPath);

            try {

                if (!string.IsNullOrEmpty(debugCommit))
                {
                    var debugCommitDetails = await watcher.GetCommitDetails(new System.Collections.Generic.List<string> { debugCommit });
                    return new OkObjectResult(new GitHubWatcher.CommitDetailsResponse { RecentCommits = debugCommitDetails } );
                }

                var commits = await watcher.GetCommitsInLast24HoursAsync();

                var commitDetails = await watcher.GetCommitDetails(commits);

                var responseMessage = $"There were {commits.Count} commits in the last 24 hours.";

                return new OkObjectResult(new GitHubWatcher.CommitDetailsResponse { RecentCommits = commitDetails } );
            }
            catch (Exception ex) {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
