package local.smartemailhr;

import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Component;
import java.time.LocalDate;

@Component class DemoData implements org.springframework.boot.CommandLineRunner {
    private final Users users; private final Offres offres; private final BCryptPasswordEncoder encoder;
    DemoData(Users users, Offres offres, BCryptPasswordEncoder encoder) { this.users = users; this.offres = offres; this.encoder = encoder; }
    public void run(String... args) {
        if (users.count() > 0) return;
        var admin = new UserEntity(); admin.id = "10000000-0000-0000-0000-000000000001"; admin.nom = "Admin SmartEmail"; admin.email = "admin@smartemailhr.local"; admin.motDePasseHash = encoder.encode("Admin@123"); admin.role = "admin"; users.save(admin);
        var hr = new UserEntity(); hr.id = "10000000-0000-0000-0000-000000000002"; hr.nom = "Responsable RH Demo"; hr.email = "rh@smartemailhr.local"; hr.motDePasseHash = encoder.encode("Rh@123456"); hr.role = "rh"; users.save(hr);
        var offer = new OffreEntity(); offer.id = "20000000-0000-0000-0000-000000000001"; offer.titre = "Développeur Frontend Angular"; offer.description = "Concevoir des interfaces Angular performantes et maintenables."; offer.competencesRequises = "[\"Angular\",\"TypeScript\",\"HTML\",\"CSS\",\"RxJS\"]"; offer.niveauExperience = "Junior"; offer.domaine = "Développement Web"; offer.dateExpiration = LocalDate.now().plusDays(30).atStartOfDay(); offer.createur = hr; offres.save(offer);
    }

}
