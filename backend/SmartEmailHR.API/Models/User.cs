using System.ComponentModel.DataAnnotations;
using SmartEmailHR.API.Configuration;

namespace SmartEmailHR.API.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(200)]
    public string Nom { get; set; } = string.Empty;

    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(120)]
    public string MotDePasseHash { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Role { get; set; } = Roles.Rh;

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public bool Actif { get; set; } = true;

    public ICollection<Offre> OffresCreees { get; set; } = new List<Offre>();
}

