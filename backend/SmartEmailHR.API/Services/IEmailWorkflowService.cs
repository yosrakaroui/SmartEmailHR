namespace SmartEmailHR.API.Services;

public interface IEmailWorkflowService
{
    Task<EmailDispatchResult> SendDecisionEmailAsync(EmailDispatchRequest request, CancellationToken cancellationToken = default);
}

