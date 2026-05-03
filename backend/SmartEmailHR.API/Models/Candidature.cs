using System.ComponentModel.DataAnnotations;
using SmartEmailHR.API.Configuration;

namespace SmartEmailHR.API.Models;

public sealed class Candidature
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OffreId { get; set; }

    public Offre? Offre { get; set; }

    [MaxLength(200)]
    public string NomCandidat { get; set; } = string.Empty;

    [MaxLength(320)]
    public string EmailCandidat { get; set; } = string.Empty;

    public string ContenuCV { get; set; } = string.Empty;

    [MaxLength(260)]
    public string ObjetEmail { get; set; } = string.Empty;

    public DateTime DateReception { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string Statut { get; set; } = CandidatureStatuts.EnAttente;

    public bool EmailReponseEnvoye { get; set; }

    [MaxLength(1000)]
    public string? CvUrl { get; set; }

    public AnalyseIA? AnalyseIA { get; set; }

    public ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();
}

