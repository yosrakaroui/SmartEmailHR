package local.smartemailhr;

import jakarta.persistence.*;
import java.time.LocalDateTime;
import java.util.UUID;

@Entity @Table(name="Users") class UserEntity {
 @Id String id=UUID.randomUUID().toString(); @Column(nullable=false,length=200) String nom; @Column(nullable=false,unique=true,length=320) String email;
 @Column(nullable=false,length=120) String motDePasseHash; @Column(nullable=false,length=10) String role="rh"; LocalDateTime dateCreation=LocalDateTime.now(); boolean actif=true;
}
@Entity @Table(name="Offres") class OffreEntity {
 @Id String id=UUID.randomUUID().toString(); @Column(nullable=false,length=200) String titre; @Lob @Column(nullable=false) String description;
 @Lob @Column(nullable=false) String competencesRequises; @Column(nullable=false,length=50) String niveauExperience; @Column(nullable=false,length=100) String domaine;
 LocalDateTime dateExpiration; @Column(nullable=false,length=20) String statut="Active"; LocalDateTime dateCreation=LocalDateTime.now(); String creePar;
}
@Entity @Table(name="Candidatures") class CandidatureEntity {
 @Id String id=UUID.randomUUID().toString(); @Column(nullable=false) String offreId; @Column(nullable=false,length=200) String nomCandidat;
 @Column(nullable=false,length=320) String emailCandidat; @Lob @Column(nullable=false) String contenuCV; @Column(nullable=false,length=260) String objetEmail;
 LocalDateTime dateReception=LocalDateTime.now(); @Column(nullable=false,length=20) String statut="EnAttente"; boolean emailReponseEnvoye; @Column(length=1000) String cvUrl;
}
@Entity @Table(name="AnalysesIA") class AnalyseEntity {
 @Id String id=UUID.randomUUID().toString(); @Column(nullable=false,unique=true) String candidatureId; int score; @Lob String resumeCompetences; @Lob String competencesDetectees;
 @Column(length=100) String classification; boolean coherencePoste; @Column(length=20) String decisionSuggeree; LocalDateTime dateAnalyse=LocalDateTime.now();
}
@Entity @Table(name="EmailLogs") class EmailLogEntity {
 @Id String id=UUID.randomUUID().toString(); String candidatureId; @Column(length=20) String typeDecision; @Column(length=260) String sujet; @Lob String corps;
 @Column(length=320) String destinataire; boolean reussi; @Column(length=2000) String erreur; LocalDateTime dateEnvoi=LocalDateTime.now();
}
