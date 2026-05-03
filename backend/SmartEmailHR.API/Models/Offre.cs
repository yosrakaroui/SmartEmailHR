using System.ComponentModel.DataAnnotations;
using SmartEmailHR.API.Configuration;

namespace SmartEmailHR.API.Models;

public sealed class Offre
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(200)]
    public string Titre { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CompetencesRequises { get; set; } = string.Empty;

    [MaxLength(50)]
    public string NiveauExperience { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Domaine { get; set; } = string.Empty;

    public DateTime DateExpiration { get; set; }

    [MaxLength(20)]
    public string Statut { get; set; } = OffreStatuts.Active;

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Guid CreePar { get; set; }

    public User? Createur { get; set; }

    public ICollection<Candidature> Candidatures { get; set; } = new List<Candidature>();
}

