using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SmartEmailHR.API.Configuration;

namespace SmartEmailHR.API.Services;

public sealed class EmailWorkflowService : IEmailWorkflowService
{
    private readonly HttpClient _httpClient;
    private readonly N8nOptions _options;
    private readonly ILogger<EmailWorkflowService> _logger;

    public EmailWorkflowService(
        HttpClient httpClient,
        IOptions<N8nOptions> options,
        ILogger<EmailWorkflowService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailDispatchResult> SendDecisionEmailAsync(
        EmailDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var webhookUrl = ResolveWebhook(request.Decision);
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return new EmailDispatchResult
            {
                Success = true,
                Error = "Aucun webhook n8n configuré. Email simulé côté backend."
            };
        }

        var payload = new
        {
            candidature_id = request.CandidatureId,
            email_destinataire = request.EmailDestinataire,
            nom_candidat = request.NomCandidat,
            poste = request.Poste,
            sujet = request.Sujet,
            corps_email = request.CorpsEmail
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
        {
            Content = JsonContent.Create(payload)
        };

        if (!string.IsNullOrWhiteSpace(_options.SharedSecret))
        {
            message.Headers.TryAddWithoutValidation("X-N8N-Secret", _options.SharedSecret);
        }

        try
        {
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new EmailDispatchResult
                {
                    Success = true,
                    HttpStatusCode = (int)response.StatusCode
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Échec webhook n8n {Status}: {Error}", response.StatusCode, error);

            return new EmailDispatchResult
            {
                Success = false,
                HttpStatusCode = (int)response.StatusCode,
                Error = string.IsNullOrWhiteSpace(error) ? "Webhook n8n non disponible." : error
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erreur réseau webhook n8n.");
            return new EmailDispatchResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private string ResolveWebhook(string decision)
    {
        var normalized = decision.Trim().ToLowerInvariant();
        if (normalized.Contains("accept"))
        {
            return _options.AcceptationWebhookUrl;
        }

        return _options.RefusWebhookUrl;
    }
}

