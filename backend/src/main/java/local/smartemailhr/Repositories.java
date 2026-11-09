package local.smartemailhr;

import org.springframework.data.jpa.repository.JpaRepository;
import java.util.*;

interface Users extends JpaRepository<UserEntity,String> { Optional<UserEntity> findByEmail(String email); }
interface Offres extends JpaRepository<OffreEntity,String> { List<OffreEntity> findAllByOrderByDateCreationDesc(); }
interface Candidatures extends JpaRepository<CandidatureEntity,String> { List<CandidatureEntity> findAllByOrderByDateReceptionDesc(); List<CandidatureEntity> findByOffreId(String offreId); }
interface Analyses extends JpaRepository<AnalyseEntity,String> { Optional<AnalyseEntity> findByCandidatureId(String id); }
interface EmailLogs extends JpaRepository<EmailLogEntity,String> { List<EmailLogEntity> findTop30ByOrderByDateEnvoiDesc(); }
