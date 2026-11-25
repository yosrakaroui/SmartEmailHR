package local.smartemailhr;

import org.springframework.data.jpa.repository.JpaRepository;
import java.util.List;
import java.util.Optional;

interface Users extends JpaRepository<UserEntity, String> {
    Optional<UserEntity> findByEmail(String email);
}

interface Offres extends JpaRepository<OffreEntity, String> {
    List<OffreEntity> findAllByOrderByDateCreationDesc();
    long countByStatut(String statut);
}

interface Candidatures extends JpaRepository<CandidatureEntity, String> {
    List<CandidatureEntity> findAllByOrderByDateReceptionDesc();
    List<CandidatureEntity> findAllByOffre_Id(String offreId);
    List<CandidatureEntity> findAllByStatut(String statut);
    long countByStatut(String statut);
}

interface Analyses extends JpaRepository<AnalyseEntity, String> {
    Optional<AnalyseEntity> findByCandidature_Id(String candidatureId);
}

interface EmailLogs extends JpaRepository<EmailLogEntity, String> {
    List<EmailLogEntity> findTop30ByOrderByDateEnvoiDesc();
}
