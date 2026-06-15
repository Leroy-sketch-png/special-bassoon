using Hangfire;
using MoePortal.Core.Interfaces;

namespace MoePortal.Api.Jobs;

public class LifecycleJob
{
    private readonly IEligibilityService _eligibilityService;

    public LifecycleJob(IEligibilityService eligibilityService)
    {
        _eligibilityService = eligibilityService;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessCitizenLifecycleAsync(Guid citizenId, CancellationToken ct)
    {
        await _eligibilityService.EvaluateAndApplyLifecycleAsync(citizenId, ct);
    }
}
