using Olly.Cards.Progress;
using Olly.Storage.Models.Jobs;

namespace Olly.Cards.Extensions;

public static class JobStatusExtensions
{
    public static ProgressStyle ToProgressStyle(this JobStatus status)
    {
        if (status.IsError) return ProgressStyle.Error;
        if (status.IsRunning) return ProgressStyle.InProgress;
        if (status.IsWarning) return ProgressStyle.Warning;
        return ProgressStyle.Success;
    }
}