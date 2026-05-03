using System.ComponentModel.DataAnnotations;

namespace SmartEmailHR.API.Models;

public sealed class EmailLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CandidatureId { get; set; }

    public Candidature? Candidature { get; set; }

    [MaxLength(20)]
    public string TypeDecision { get; set; } = string.Empty;

    [MaxLength(260)]
    public string Sujet { get; set; } = string.Empty;

    public string Corps { get; set; } = string.Empty;

    [MaxLength(320)]
    public string Destinataire { get; set; } = string.Empty;

    public bool Reussi { get; set; }

    [MaxLength(2000)]
    public string? Erreur { get; set; }

    public DateTime DateEnvoi { get; set; } = DateTime.UtcNow;
}

