package local.smartemailhr;

import jakarta.persistence.*;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

@Entity @Table(name = "Users") class UserEntity {
    @Id @Column(name = "Id", length = 36) String id = UUID.randomUUID().toString();
    @Column(name = "Nom", nullable = false, length = 200) String nom;
    @Column(name = "Email", nullable = false, unique = true, length = 320) String email;
    @Column(name = "MotDePasseHash", nullable = false, length = 120) String motDePasseHash;
    @Column(name = "Role", nullable = false, length = 10) String role = "rh";
    @Column(name = "DateCreation", nullable = false) LocalDateTime dateCreation = LocalDateTime.now();
    @Column(name = "Actif", nullable = false) boolean actif = true;
    @OneToMany(mappedBy = "createur") List<OffreEntity> offresCreees = new ArrayList<>();
}

@Entity @Table(name = "Offres") class OffreEntity {
    @Id @Column(name = "Id", length = 36) String id = UUID.randomUUID().toString();
    @Column(name = "Titre", nullable = false, length = 200) String titre;
    @Lob @Column(name = "Description", nullable = false) String description;
    @Lob @Column(name = "CompetencesRequises", nullable = false) String competencesRequises;
    @Column(name = "NiveauExperience", nullable = false, length = 50) String niveauExperience;
    @Column(name = "Domaine", nullable = false, length = 100) String domaine;
    @Column(name = "DateExpiration", nullable = false) LocalDateTime dateExpiration;
    @Column(name = "Statut", nullable = false, length = 20) String statut = "Active";
    @Column(name = "DateCreation", nullable = false) LocalDateTime dateCreation = LocalDateTime.now();
    @ManyToOne(fetch = FetchType.LAZY) @JoinColumn(name = "CreePar", nullable = false, foreignKey = @ForeignKey(name = "FK_Offres_Users_CreePar")) UserEntity createur;
    @OneToMany(mappedBy = "offre", cascade = CascadeType.ALL, orphanRemoval = true) List<CandidatureEntity> candidatures = new ArrayList<>();
}

@Entity @Table(name = "Candidatures") class CandidatureEntity {
    @Id @Column(name = "Id", length = 36) String id = UUID.randomUUID().toString();
    @ManyToOne(fetch = FetchType.LAZY) @JoinColumn(name = "OffreId", nullable = false, foreignKey = @ForeignKey(name = "FK_Candidatures_Offres_OffreId")) OffreEntity offre;
    @Column(name = "NomCandidat", nullable = false, length = 200) String nomCandidat;
    @Column(name = "EmailCandidat", nullable = false, length = 320) String emailCandidat;
    @Lob @Column(name = "ContenuCV", nullable = false) String contenuCV;
    @Column(name = "ObjetEmail", nullable = false, length = 260) String objetEmail;
    @Column(name = "DateReception", nullable = false) LocalDateTime dateReception = LocalDateTime.now();
    @Column(name = "Statut", nullable = false, length = 20) String statut = "EnAttente";
    @Column(name = "EmailReponseEnvoye", nullable = false) boolean emailReponseEnvoye;
    @Column(name = "CvUrl", length = 1000) String cvUrl;
    @OneToOne(mappedBy = "candidature", cascade = CascadeType.ALL, orphanRemoval = true) AnalyseEntity analyseIA;
    @OneToMany(mappedBy = "candidature", cascade = CascadeType.ALL, orphanRemoval = true) List<EmailLogEntity> emailLogs = new ArrayList<>();
}

@Entity @Table(name = "AnalysesIA") class AnalyseEntity {
    @Id @Column(name = "Id", length = 36) String id = UUID.randomUUID().toString();
    @OneToOne(fetch = FetchType.LAZY) @JoinColumn(name = "CandidatureId", nullable = false, unique = true, foreignKey = @ForeignKey(name = "FK_AnalysesIA_Candidatures_CandidatureId")) CandidatureEntity candidature;
    @Column(name = "Score", nullable = false) int score;
    @Lob @Column(name = "ResumeCompetences", nullable = false) String resumeCompetences;
    @Lob @Column(name = "CompetencesDetectees", nullable = false) String competencesDetectees;
    @Column(name = "Classification", nullable = false, length = 100) String classification;
    @Column(name = "CoherencePoste", nullable = false) boolean coherencePoste;
    @Column(name = "DecisionSuggeree", nullable = false, length = 20) String decisionSuggeree;
    @Column(name = "DateAnalyse", nullable = false) LocalDateTime dateAnalyse = LocalDateTime.now();
}

@Entity @Table(name = "EmailLogs") class EmailLogEntity {
    @Id @Column(name = "Id", length = 36) String id = UUID.randomUUID().toString();
    @ManyToOne(fetch = FetchType.LAZY) @JoinColumn(name = "CandidatureId", nullable = false, foreignKey = @ForeignKey(name = "FK_EmailLogs_Candidatures_CandidatureId")) CandidatureEntity candidature;
    @Column(name = "TypeDecision", nullable = false, length = 20) String typeDecision;
    @Column(name = "Sujet", nullable = false, length = 260) String sujet;
    @Lob @Column(name = "Corps", nullable = false) String corps;
    @Column(name = "Destinataire", nullable = false, length = 320) String destinataire;
    @Column(name = "Reussi", nullable = false) boolean reussi;
    @Column(name = "Erreur", length = 2000) String erreur;
    @Column(name = "DateEnvoi", nullable = false) LocalDateTime dateEnvoi = LocalDateTime.now();
}
