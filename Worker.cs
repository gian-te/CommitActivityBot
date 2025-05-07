namespace CommitActivityBot;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string repoPath = @"C:\Repos\CommitActivityBot"; // <- update this!
    private readonly int minCommitsPerDay = 1;
    private readonly int maxCommitsPerDay = 5;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            int commitCount = new Random().Next(minCommitsPerDay, maxCommitsPerDay + 1);
            _logger.LogInformation($"Planning {commitCount} commits for today...");

            var commitTimes = GenerateRandomCommitTimes(commitCount);
            foreach (var commitTime in commitTimes)
            {
                var waitTime = commitTime - DateTime.Now;
                if (waitTime.TotalSeconds > 0)
                {
                    _logger.LogInformation($"Waiting until {commitTime} for next commit...");
                    await Task.Delay(waitTime, stoppingToken);
                }

                _logger.LogInformation($"Making commit at {DateTime.Now}...");
                MakeCommit();
            }

            var sleepUntil = DateTime.Today.AddDays(1) - DateTime.Now;
            _logger.LogInformation("Done for today. Sleeping until tomorrow.");
            await Task.Delay(sleepUntil, stoppingToken);
        }
    }

    private DateTime[] GenerateRandomCommitTimes(int count)
    {
        var rnd = new Random();
        return Enumerable.Range(0, count)
            .Select(_ => DateTime.Today.AddHours(rnd.Next(8, 22)).AddMinutes(rnd.Next(0, 60)))
            .OrderBy(x => x)
            .ToArray();
    }

    private void MakeCommit()
    {
        try
        {

            RunGitCommand("add .");
            RunGitCommand($"commit -m \"Auto commit {DateTime.Now}\"");
            RunGitCommand("push");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Commit failed: {ex.Message}");
        }
    }

    private void RunGitCommand(string args)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var proc = Process.Start(psi);
        proc.WaitForExit();

        string output = proc.StandardOutput.ReadToEnd();
        string error = proc.StandardError.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(output)) _logger.LogInformation(output);
        if (!string.IsNullOrWhiteSpace(error)) _logger.LogError(error);
    }
}
