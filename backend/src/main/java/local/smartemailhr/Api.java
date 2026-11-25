package local.smartemailhr;

import com.fasterxml.jackson.databind.ObjectMapper;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.security.Keys;
import jakarta.servlet.FilterChain;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.config.annotation.method.configuration.EnableMethodSecurity;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;
import org.springframework.stereotype.Component;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.client.RestClient;
import org.springframework.web.filter.OncePerRequestFilter;
import org.springframework.web.server.ResponseStatusException;

import java.nio.charset.StandardCharsets;
import java.time.*;
import java.util.*;
import java.util.stream.Collectors;

@Configuration
@EnableMethodSecurity
class SecurityConfig {
    @Bean BCryptPasswordEncoder passwordEncoder() { return new BCryptPasswordEncoder(12); }
    @Bean JwtFilter jwtFilter(@Value("${app.jwt.secret}") String secret, @Value("${app.jwt.issuer}") String issuer, @Value("${app.jwt.audience}") String audience) { return new JwtFilter(secret, issuer, audience); }

    @Bean SecurityFilterChain filter(HttpSecurity http, JwtFilter jwt) throws Exception {
        return http.csrf(c -> c.disable())
            .cors(c -> c.configurationSource(req -> {
                var config = new org.springframework.web.cors.CorsConfiguration();
                config.setAllowedOrigins(List.of("http://localhost:4200", "http://127.0.0.1:4200"));
                config.setAllowedMethods(List.of("GET", "POST", "PATCH", "DELETE", "OPTIONS"));
                config.setAllowedHeaders(List.of("*"));
                return config;
            }))
            .sessionManagement(s -> s.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
            .authorizeHttpRequests(a -> a.requestMatchers("/api/auth/login", "/api/candidatures/recevoir", "/api/candidatures/*/email-envoye", "/swagger-ui/**", "/v3/api-docs/**").permitAll().anyRequest().authenticated())
            .addFilterBefore(jwt, UsernamePasswordAuthenticationFilter.class).build();
    }
}

class JwtFilter extends OncePerRequestFilter {
    private final String secret, issuer, audience;
    JwtFilter(String secret, String issuer, String audience) { this.secret = secret; this.issuer = issuer; this.audience = audience; }

    @Override protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain chain) throws java.io.IOException, jakarta.servlet.ServletException {
        String authorization = request.getHeader("Authorization");
        if (authorization != null && authorization.startsWith("Bearer ")) {
            try {
                var claims = Jwts.parser().verifyWith(Keys.hmacShaKeyFor(secret.getBytes(StandardCharsets.UTF_8))).requireIssuer(issuer).requireAudience(audience).build().parseSignedClaims(authorization.substring(7)).getPayload();
                var authorities = List.of(new SimpleGrantedAuthority("ROLE_" + claims.get("role", String.class)));
                SecurityContextHolder.getContext().setAuthentication(new UsernamePasswordAuthenticationToken(claims.getSubject(), null, authorities));
            } catch (Exception ignored) {
                SecurityContextHolder.clearContext();
            }
        }
        chain.doFilter(request, response);
    }
}

@RestControllerAdvice
class ApiExceptionHandler {
    @ExceptionHandler(ResponseStatusException.class)
    ResponseEntity<Map<String, String>> handleStatus(ResponseStatusException exception) {
        return ResponseEntity.status(exception.getStatusCode()).body(Map.of("message", Objects.toString(exception.getReason(), "Erreur de requête.")));
    }

    @ExceptionHandler(org.springframework.dao.DataIntegrityViolationException.class)
    ResponseEntity<Map<String, String>> handleConstraint() {
        return ResponseEntity.badRequest().body(Map.of("message", "Les données fournies ne respectent pas les contraintes de la base."));
    }
}

@RestController @RequestMapping("/api/auth")
class AuthApi {
    private final Users users;
    private final BCryptPasswordEncoder encoder;
    private final String secret, issuer, audience;
    private final long hours;

    AuthApi(Users users, BCryptPasswordEncoder encoder, @Value("${app.jwt.secret}") String secret, @Value("${app.jwt.issuer}") String issuer, @Value("${app.jwt.audience}") String audience, @Value("${app.jwt.expiration-hours}") long hours) {
        this.users = users; this.encoder = encoder; this.secret = secret; this.issuer = issuer; this.audience = audience; this.hours = hours;
    }

    record Login(@Email @NotBlank String email, @NotBlank String motDePasse) {}
    record Summary(String id, String nom, String email, String role, boolean actif) {}

    @PostMapping("/login") ResponseEntity<?> login(@Valid @RequestBody Login request) {
        var user = users.findByEmail(request.email().trim().toLowerCase(Locale.ROOT)).orElse(null);
        if (user == null || !user.actif || !encoder.matches(request.motDePasse(), user.motDePasseHash)) return ResponseEntity.status(401).body(Map.of("message", "Identifiants invalides."));
        var expiry = Instant.now().plus(Duration.ofHours(hours));
        var token = Jwts.builder().issuer(issuer).audience().add(audience).and().subject(user.id).claim("role", user.role).issuedAt(new Date()).expiration(Date.from(expiry)).signWith(Keys.hmacShaKeyFor(secret.getBytes(StandardCharsets.UTF_8))).compact();
        return ResponseEntity.ok(Map.of("token", token, "expireLe", expiry, "utilisateur", summary(user)));
    }

    @GetMapping("/me") Summary me(org.springframework.security.core.Authentication authentication) {
        return users.findById((String) authentication.getPrincipal()).map(AuthApi::summary).orElseThrow(() -> new ResponseStatusException(HttpStatus.UNAUTHORIZED, "Session invalide."));
    }

    static Summary summary(UserEntity user) { return new Summary(user.id, user.nom, user.email, user.role, user.actif); }
}

@RestController @RequestMapping("/api/offres")
@org.springframework.security.access.prepost.PreAuthorize("hasAnyRole('rh','admin')")
@org.springframework.transaction.annotation.Transactional
class OffresApi {
    private final Offres offres;
    private final Candidatures candidatures;
    private final Users users;
    private final ObjectMapper mapper;

    OffresApi(Offres offres, Candidatures candidatures, Users users, ObjectMapper mapper) { this.offres = offres; this.candidatures = candidatures; this.users = users; this.mapper = mapper; }

    @GetMapping List<Map<String, Object>> all(@RequestParam(required = false) String domaine, @RequestParam(required = false) String statut) {
        updateExpired();
        return offres.findAllByOrderByDateCreationDesc().stream().filter(o -> blank(domaine) || o.domaine.equalsIgnoreCase(domaine.trim())).filter(o -> blank(statut) || o.statut.equalsIgnoreCase(normalizeOfferStatus(statut))).map(this::dto).toList();
    }

    @GetMapping("/{id}") Map<String, Object> one(@PathVariable String id) {
        updateExpired();
        var offer = offres.findById(id).orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Offre introuvable."));
        var result = dto(offer);
        result.put("candidatures", candidatures.findAllByOffre_Id(id).stream().map(c -> candidateSummary(c, offer)).toList());
        return result;
    }

    @PostMapping ResponseEntity<?> create(@Valid @RequestBody OfferRequest request, org.springframework.security.core.Authentication authentication) {
        var offer = new OffreEntity();
        offer.titre = request.titre().trim(); offer.description = request.description().trim(); offer.competencesRequises = skillsJson(request.competencesRequises());
        offer.niveauExperience = request.niveauExperience().trim(); offer.domaine = request.domaine().trim(); offer.dateExpiration = parseDate(request.dateExpiration());
        if (offer.dateExpiration.toLocalDate().isBefore(LocalDate.now())) throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "La date d'expiration doit être aujourd'hui ou dans le futur.");
        offer.createur = users.findById((String) authentication.getPrincipal()).orElseThrow(() -> new ResponseStatusException(HttpStatus.UNAUTHORIZED, "Utilisateur introuvable."));
        return ResponseEntity.status(201).body(dto(offres.save(offer)));
    }

    @PatchMapping("/{id}") Map<String, Object> update(@PathVariable String id, @RequestBody Map<String, Object> body) {
        var offer = offres.findById(id).orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Offre introuvable."));
        if (body.containsKey("titre")) offer.titre = requiredText(body, "titre");
        if (body.containsKey("description")) offer.description = requiredText(body, "description");
        if (body.containsKey("competencesRequises")) offer.competencesRequises = skillsJson(body.get("competencesRequises"));
        if (body.containsKey("niveauExperience")) offer.niveauExperience = requiredText(body, "niveauExperience");
        if (body.containsKey("domaine")) offer.domaine = requiredText(body, "domaine");
        if (body.containsKey("dateExpiration")) offer.dateExpiration = parseDate(body.get("dateExpiration"));
        if (body.containsKey("statut")) offer.statut = normalizeOfferStatus(Objects.toString(body.get("statut"), ""));
        return dto(offres.save(offer));
    }

    @DeleteMapping("/{id}") ResponseEntity<Void> delete(@PathVariable String id) {
        var offer = offres.findById(id).orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Offre introuvable."));
        offres.delete(offer);
        return ResponseEntity.noContent().build();
    }

    void updateExpired() {
        var today = LocalDate.now();
        offres.findAll().stream().filter(o -> o.statut.equals("Active") && o.dateExpiration.toLocalDate().isBefore(today)).forEach(o -> o.statut = "Expiree");
        offres.flush();
    }

    Map<String, Object> dto(OffreEntity offer) {
        var result = new LinkedHashMap<String, Object>();
        result.put("id", offer.id); result.put("titre", offer.titre); result.put("description", offer.description); result.put("competencesRequises", readSkills(offer.competencesRequises));
        result.put("niveauExperience", offer.niveauExperience); result.put("domaine", offer.domaine); result.put("dateExpiration", offer.dateExpiration); result.put("statut", offer.statut);
        result.put("dateCreation", offer.dateCreation); result.put("creePar", offer.createur.id); result.put("nombreCandidatures", candidatures.findAllByOffre_Id(offer.id).size());
        return result;
    }

    private Map<String, Object> candidateSummary(CandidatureEntity candidate, OffreEntity offer) {
        var result = new LinkedHashMap<String, Object>(); result.put("id", candidate.id); result.put("offreId", offer.id); result.put("titreOffre", offer.titre); result.put("domaine", offer.domaine);
        result.put("nomCandidat", candidate.nomCandidat); result.put("emailCandidat", candidate.emailCandidat); result.put("dateReception", candidate.dateReception); result.put("statut", candidate.statut);
        result.put("emailReponseEnvoye", candidate.emailReponseEnvoye); result.put("cvUrl", candidate.cvUrl); result.put("analyseIA", candidate.analyseIA == null ? null : analysisDto(candidate.analyseIA)); return result;
    }

    private Map<String, Object> analysisDto(AnalyseEntity analysis) {
        var result = new LinkedHashMap<String, Object>(); result.put("score", analysis.score); result.put("resumeCompetences", analysis.resumeCompetences); result.put("competencesDetectees", readSkills(analysis.competencesDetectees));
        result.put("classification", analysis.classification); result.put("coherencePoste", analysis.coherencePoste); result.put("decisionSuggeree", analysis.decisionSuggeree); result.put("dateAnalyse", analysis.dateAnalyse); return result;
    }

    List<?> readSkills(String value) { try { return mapper.readValue(value, List.class); } catch (Exception ignored) { return Arrays.stream(Objects.toString(value, "").split(",")).filter(s -> !s.isBlank()).toList(); } }
    String skillsJson(Object value) { try { return mapper.writeValueAsString(value instanceof List<?> ? value : List.of()); } catch (Exception ignored) { return "[]"; } }
    private boolean blank(String value) { return value == null || value.isBlank(); }
    private String normalizeOfferStatus(String value) { String v = value.trim().toLowerCase(Locale.ROOT); return switch (v) { case "expiree", "expirée" -> "Expiree"; case "desactivee", "désactivée", "desactive" -> "Desactivee"; default -> "Active"; }; }
    private String requiredText(Map<String, Object> body, String key) { String value = Objects.toString(body.get(key), "").trim(); if (value.isEmpty()) throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Le champ " + key + " est obligatoire."); return value; }
    private LocalDateTime parseDate(Object value) { try { String text = Objects.toString(value); return text.length() == 10 ? LocalDate.parse(text).atStartOfDay() : LocalDateTime.parse(text); } catch (Exception exception) { throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Format de date invalide."); } }
}

record OfferRequest(@NotBlank @Size(max = 200) String titre, @NotBlank String description, @NotNull List<String> competencesRequises, @NotBlank @Size(max = 50) String niveauExperience, @NotBlank @Size(max = 100) String domaine, @NotBlank String dateExpiration) {}

@RestController @RequestMapping("/api/candidatures")
@org.springframework.transaction.annotation.Transactional
class CandidaturesApi {
    private final Candidatures candidatures; private final Offres offres; private final EmailLogs logs; private final String n8nSecret, acceptUrl, refusUrl; private final GroqService groq; private final ObjectMapper mapper; private final RestClient client = RestClient.create();
    CandidaturesApi(Candidatures candidatures, Offres offres, EmailLogs logs, @Value("${app.n8n.shared-secret}") String n8nSecret, @Value("${app.n8n.acceptation-url}") String acceptUrl, @Value("${app.n8n.refus-url}") String refusUrl, GroqService groq, ObjectMapper mapper) { this.candidatures = candidatures; this.offres = offres; this.logs = logs; this.n8nSecret = n8nSecret; this.acceptUrl = acceptUrl; this.refusUrl = refusUrl; this.groq = groq; this.mapper = mapper; }

        @PostMapping("/recevoir") ResponseEntity<?> receive(@RequestHeader(value = "X-N8N-Secret", required = false) String secret, @Valid @RequestBody ReceiveRequest request) {
        if (!validSecret(secret)) return ResponseEntity.status(401).body(Map.of("message", "Requête n8n non autorisée."));
        String subject = request.objetEmail().trim();
        var offer = offres.findAll().stream().filter(o -> match(subject, o.titre) > 0).max(Comparator.comparingInt(o -> match(subject, o.titre))).orElse(null);
        if (offer == null) return ResponseEntity.badRequest().body(Map.of("message", "Aucune offre correspondante trouvée pour l'objet de l'email."));
        boolean closed = !offer.statut.equals("Active") || offer.dateExpiration.toLocalDate().isBefore(LocalDate.now());
        var candidate = new CandidatureEntity(); candidate.offre = offer; candidate.nomCandidat = buildCandidateName(request.nomCandidat(), request.emailCandidat());
        candidate.emailCandidat = request.emailCandidat().trim().toLowerCase(Locale.ROOT); candidate.contenuCV = Objects.toString(request.contenuCv(), ""); candidate.objetEmail = subject;
        candidate.cvUrl = request.cvUrl(); candidate.statut = closed ? "Refuse" : "EnAttente";
        var analysisData = closed ? fallbackAnalysis(offer, "") : groq.analyze(offer, candidate.contenuCV);
        var analysis = new AnalyseEntity(); analysis.candidature = candidate; analysis.score = clamp(analysisData.score()); analysis.resumeCompetences = analysisData.resume();
        analysis.competencesDetectees = writeJson(analysisData.skills()); analysis.classification = analysisData.classification(); analysis.coherencePoste = analysisData.coherent(); analysis.decisionSuggeree = analysisData.suggestion();
        candidate.analyseIA = analysis; candidatures.save(candidate);
        if (closed) {
            var email = groq.generateEmail("Refuse", candidate.nomCandidat, offer.titre, analysis.resumeCompetences);
            candidate.emailReponseEnvoye = dispatch(candidate, "Refuse", email.subject(), email.body());
            candidatures.save(candidate);
        }
        return ResponseEntity.ok(Map.of("candidatureId", candidate.id, "offreId", offer.id, "statut", candidate.statut, "score", analysis.score, "decisionSuggeree", analysis.decisionSuggeree, "offreFermee", closed));
    }

    @GetMapping @org.springframework.security.access.prepost.PreAuthorize("hasAnyRole('rh','admin')")
    List<Map<String, Object>> all(@RequestParam(required = false) String offreId, @RequestParam(required = false) String statut, @RequestParam(required = false) String domaine, @RequestParam(required = false) String recherche) {
        return candidatures.findAllByOrderByDateReceptionDesc().stream()
            .filter(c -> blank(offreId) || c.offre.id.equals(offreId)).filter(c -> blank(statut) || c.statut.equalsIgnoreCase(normalizeDecision(statut)))
            .filter(c -> blank(domaine) || c.offre.domaine.equalsIgnoreCase(domaine.trim()))
            .filter(c -> blank(recherche) || (c.nomCandidat + " " + c.emailCandidat + " " + Objects.toString(c.analyseIA == null ? null : c.analyseIA.resumeCompetences, "")).toLowerCase(Locale.ROOT).contains(recherche.trim().toLowerCase(Locale.ROOT)))
            .map(c -> candidateDto(c, false)).toList();
    }

    @GetMapping("/{id}") @org.springframework.security.access.prepost.PreAuthorize("hasAnyRole('rh','admin')")
    Map<String, Object> one(@PathVariable String id) { return candidateDto(candidatures.findById(id).orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Candidature introuvable.")), true); }

    @PatchMapping("/{id}/decision") @org.springframework.security.access.prepost.PreAuthorize("hasAnyRole('rh','admin')")
    ResponseEntity<?> decision(@PathVariable String id, @RequestBody Map<String, Object> body) {
        var candidate = candidatures.findById(id).orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Candidature introuvable."));
        String decision = normalizeDecision(Objects.toString(body.get("decision"), ""));
        if (decision.equals("EnAttente")) return ResponseEntity.badRequest().body(Map.of("message", "Décision invalide. Valeurs attendues: Accepte ou Refuse."));
        candidate.statut = decision;
        String subject = Objects.toString(body.get("sujetEmail"), ""); String content = Objects.toString(body.get("corpsEmail"), "");
        if (Boolean.TRUE.equals(body.get("envoyerEmail"))) {
            boolean sent = dispatch(candidate, decision, subject, content);
            candidate.emailReponseEnvoye = sent;
        }
        candidatures.save(candidate);
        return ResponseEntity.ok(Map.of("candidatureId", candidate.id, "statut", candidate.statut, "emailReponseEnvoye", candidate.emailReponseEnvoye, "sujetEmail", subject, "corpsEmail", content));
    }

    @PatchMapping("/{id}/email-envoye") ResponseEntity<?> emailStatus(@PathVariable String id, @RequestHeader(value = "X-N8N-Secret", required = false) String secret, @RequestBody Map<String, Object> body) {
        if (!validSecret(secret)) return ResponseEntity.status(401).body(Map.of("message", "Requête n8n non autorisée."));
        var candidate = candidatures.findById(id).orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Candidature introuvable."));
        candidate.emailReponseEnvoye = Boolean.TRUE.equals(body.get("emailReponseEnvoye")); candidatures.save(candidate);
        return ResponseEntity.ok(Map.of("message", "Statut email mis à jour."));
    }

    Map<String, Object> candidateDto(CandidatureEntity candidate, boolean detail) {
        var result = new LinkedHashMap<String, Object>(); result.put("id", candidate.id); result.put("offreId", candidate.offre.id); result.put("titreOffre", candidate.offre.titre); result.put("domaine", candidate.offre.domaine);
        result.put("nomCandidat", candidate.nomCandidat); result.put("emailCandidat", candidate.emailCandidat); result.put("dateReception", candidate.dateReception); result.put("statut", candidate.statut);
        result.put("emailReponseEnvoye", candidate.emailReponseEnvoye); result.put("cvUrl", candidate.cvUrl); result.put("analyseIA", candidate.analyseIA == null ? null : analysisDto(candidate.analyseIA));
        if (detail) { result.put("contenuCv", candidate.contenuCV); result.put("objetEmail", candidate.objetEmail); } return result;
    }

    private Map<String, Object> analysisDto(AnalyseEntity analysis) {
        var result = new LinkedHashMap<String, Object>(); result.put("score", analysis.score); result.put("resumeCompetences", analysis.resumeCompetences); result.put("competencesDetectees", parseJsonList(analysis.competencesDetectees));
        result.put("classification", analysis.classification); result.put("coherencePoste", analysis.coherencePoste); result.put("decisionSuggeree", analysis.decisionSuggeree); result.put("dateAnalyse", analysis.dateAnalyse); return result;
    }

    private boolean validSecret(String provided) { return !blank(n8nSecret) && !n8nSecret.startsWith("CHANGE_ME") && Objects.equals(n8nSecret, provided); }
    private void logEmail(CandidatureEntity candidate, String decision, String subject, String body, boolean success, String error) { var log = new EmailLogEntity(); log.candidature = candidate; log.typeDecision = decision; log.sujet = Objects.toString(subject, ""); log.corps = Objects.toString(body, ""); log.destinataire = candidate.emailCandidat; log.reussi = success; log.erreur = error; candidate.emailLogs.add(log); }
    private boolean dispatch(CandidatureEntity candidate, String decision, String subject, String body) {
        String url = decision.equals("Accepte") ? acceptUrl : refusUrl;
        var payload = new LinkedHashMap<String, Object>(); payload.put("candidature_id", candidate.id); payload.put("email_destinataire", candidate.emailCandidat); payload.put("nom_candidat", candidate.nomCandidat); payload.put("poste", candidate.offre.titre); payload.put("sujet", subject); payload.put("corps_email", body);
        boolean success = false; String error = null;
        try { var response = client.post().uri(url).header("X-N8N-Secret", n8nSecret).body(payload).retrieve().toBodilessEntity(); success = response.getStatusCode().is2xxSuccessful(); }
        catch (Exception exception) { error = exception.getMessage(); }
        logEmail(candidate, decision.equals("Accepte") ? "acceptation" : "refus", subject, body, success, error);
        return success;
    }
    private String buildCandidateName(String provided, String email) { if (!blank(provided)) return provided.trim(); String name = email.substring(0, email.indexOf('@')).replace('.', ' ').replace('_', ' ').trim(); return name.isEmpty() ? "Candidat Inconnu" : Arrays.stream(name.split("\\s+")).map(s -> Character.toUpperCase(s.charAt(0)) + s.substring(1)).collect(Collectors.joining(" ")); }
    private int match(String subject, String title) { String s = subject.toLowerCase(Locale.ROOT), t = title.toLowerCase(Locale.ROOT); if (s.contains(t)) return 100; return (int) Arrays.stream(t.split("\\s+")).filter(w -> w.length() > 2 && s.contains(w)).count() * 10; }
    private String normalizeDecision(String value) { String v = value.toLowerCase(Locale.ROOT); if (v.contains("accept")) return "Accepte"; if (v.contains("refus") || v.contains("reject")) return "Refuse"; return "EnAttente"; }
    private AnalysisData fallbackAnalysis(OffreEntity offer, String cv) { return groq.fallback(offer, cv); }
    private String writeJson(Object value) { try { return mapper.writeValueAsString(value); } catch (Exception ignored) { return "[]"; } }
    private List<?> parseJsonList(String value) { try { return mapper.readValue(value, List.class); } catch (Exception ignored) { return List.of(); } }
    private boolean blank(String value) { return value == null || value.isBlank(); }
    private int clamp(int value) { return Math.max(0, Math.min(100, value)); }
}

record ReceiveRequest(@NotBlank @Size(max = 260) String objetEmail, @NotBlank @Email @Size(max = 320) String emailCandidat, @Size(max = 200) String nomCandidat, String contenuCv, @Size(max = 1000) String cvUrl) {}

@RestController @RequestMapping("/api")
@org.springframework.transaction.annotation.Transactional(readOnly = true)
class AdminApi {
    private final Users users; private final BCryptPasswordEncoder encoder; private final Candidatures candidatures; private final Offres offres; private final EmailLogs logs;
    AdminApi(Users users, BCryptPasswordEncoder encoder, Candidatures candidatures, Offres offres, EmailLogs logs) { this.users = users; this.encoder = encoder; this.candidatures = candidatures; this.offres = offres; this.logs = logs; }

    @GetMapping("/users") @org.springframework.security.access.prepost.PreAuthorize("hasRole('admin')") List<?> users() { return users.findAll().stream().sorted(Comparator.comparing(u -> u.nom, String.CASE_INSENSITIVE_ORDER)).map(AuthApi::summary).toList(); }
    @PostMapping("/users") @org.springframework.transaction.annotation.Transactional @org.springframework.security.access.prepost.PreAuthorize("hasRole('admin')") ResponseEntity<?> create(@Valid @RequestBody CreateUserRequest request) {
        String email = request.email().trim().toLowerCase(Locale.ROOT); if (users.findByEmail(email).isPresent()) return ResponseEntity.badRequest().body(Map.of("message", "Cet email est déjà utilisé."));
        String role = request.role().trim().toLowerCase(Locale.ROOT); if (!Set.of("rh", "admin").contains(role)) return ResponseEntity.badRequest().body(Map.of("message", "Rôle invalide. Valeurs possibles: rh ou admin."));
        var user = new UserEntity(); user.nom = request.nom().trim(); user.email = email; user.motDePasseHash = encoder.encode(request.motDePasse()); user.role = role;
        return ResponseEntity.status(201).body(AuthApi.summary(users.save(user)));
    }
    @PatchMapping("/users/{id}/status") @org.springframework.transaction.annotation.Transactional @org.springframework.security.access.prepost.PreAuthorize("hasRole('admin')") ResponseEntity<?> status(@PathVariable String id, @RequestBody Map<String, Object> body) {
        var user = users.findById(id).orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Utilisateur introuvable.")); user.actif = Boolean.TRUE.equals(body.get("actif")); return ResponseEntity.ok(AuthApi.summary(users.save(user)));
    }

    @GetMapping("/stats/global") @org.springframework.security.access.prepost.PreAuthorize("hasRole('admin')") Map<String, Object> stats() {
        var all = candidatures.findAll(); var byDomain = all.stream().collect(Collectors.groupingBy(c -> c.offre.domaine, TreeMap::new, Collectors.toList())).entrySet().stream().map(entry -> Map.of("domaine", entry.getKey(), "nombreCandidatures", entry.getValue().size(), "acceptees", entry.getValue().stream().filter(c -> c.statut.equals("Accepte")).count(), "refusees", entry.getValue().stream().filter(c -> c.statut.equals("Refuse")).count())).toList();
        var ranked = all.stream().filter(c -> c.analyseIA != null).sorted(Comparator.comparingInt((CandidatureEntity c) -> c.analyseIA.score).reversed()).toList();
        var top = scoreItems(ranked.stream().filter(c -> c.statut.equals("Accepte")).limit(5).toList()); var weak = scoreItems(ranked.stream().filter(c -> c.statut.equals("Refuse")).sorted(Comparator.comparingInt(c -> c.analyseIA.score)).limit(5).toList());
        var result = new LinkedHashMap<String, Object>(); result.put("totalCandidatures", all.size()); result.put("candidaturesAcceptees", candidatures.countByStatut("Accepte")); result.put("candidaturesRefusees", candidatures.countByStatut("Refuse")); result.put("candidaturesEnAttente", candidatures.countByStatut("EnAttente"));
        result.put("offresActives", offres.countByStatut("Active")); result.put("offresExpirees", offres.countByStatut("Expiree")); result.put("offresDesactivees", offres.countByStatut("Desactivee")); result.put("statsParDomaine", byDomain); result.put("topCandidats", top); result.put("faiblesScores", weak); return result;
    }

    @GetMapping("/stats/emails-recents") @org.springframework.security.access.prepost.PreAuthorize("hasRole('admin')") List<Map<String, Object>> recent() {
        return logs.findTop30ByOrderByDateEnvoiDesc().stream().<Map<String, Object>>map(log -> { var item = new LinkedHashMap<String, Object>(); item.put("id", log.id); item.put("candidatureId", log.candidature.id); item.put("nomCandidat", log.candidature.nomCandidat); item.put("destinataire", log.destinataire); item.put("typeDecision", log.typeDecision); item.put("sujet", log.sujet); item.put("reussi", log.reussi); item.put("erreur", log.erreur); item.put("dateEnvoi", log.dateEnvoi); return item; }).toList();
    }
    @GetMapping("/stats/top-candidats") @org.springframework.security.access.prepost.PreAuthorize("hasRole('admin')") List<Map<String, Object>> top() { return scoreItems(candidatures.findAll().stream().filter(c -> c.statut.equals("Accepte") && c.analyseIA != null).sorted(Comparator.comparingInt((CandidatureEntity c) -> c.analyseIA.score).reversed()).limit(10).toList()); }
    private List<Map<String, Object>> scoreItems(List<CandidatureEntity> list) { return list.stream().map(c -> Map.<String, Object>of("candidatureId", c.id, "nomCandidat", c.nomCandidat, "titreOffre", c.offre.titre, "score", c.analyseIA.score, "statut", c.statut)).toList(); }
    private boolean blank(String value) { return value == null || value.isBlank(); }
    private int clamp(int value) { return Math.max(0, Math.min(100, value)); }
    private String writeJson(Object value) { try { return new ObjectMapper().writeValueAsString(value); } catch (Exception ignored) { return "[]"; } }
}

record CreateUserRequest(@NotBlank @Size(max = 200) String nom, @NotBlank @Email @Size(max = 320) String email, @NotBlank @Size(min = 8, max = 100) String motDePasse, @NotBlank String role) {}

@RestController @RequestMapping("/api/emails")
@org.springframework.security.access.prepost.PreAuthorize("hasAnyRole('rh','admin')")
@org.springframework.transaction.annotation.Transactional
class EmailsApi {
    private final Candidatures candidatures; private final EmailLogs logs; private final GroqService groq; private final RestClient client = RestClient.create(); private final String acceptUrl, refusUrl, n8nSecret;
    EmailsApi(Candidatures candidatures, EmailLogs logs, GroqService groq, @Value("${app.n8n.acceptation-url}") String acceptUrl, @Value("${app.n8n.refus-url}") String refusUrl, @Value("${app.n8n.shared-secret}") String n8nSecret) { this.candidatures = candidatures; this.logs = logs; this.groq = groq; this.acceptUrl = acceptUrl; this.refusUrl = refusUrl; this.n8nSecret = n8nSecret; }

    @PostMapping("/generer") Map<String, String> generate(@RequestBody Map<String, Object> body) {
        var candidate = findCandidate(body); String decision = normalizeDecision(Objects.toString(body.get("decision"), "Refuse"));
        var email = groq.generateEmail(decision, candidate.nomCandidat, candidate.offre.titre, candidate.analyseIA == null ? "" : candidate.analyseIA.resumeCompetences);
        return Map.of("sujet", email.subject(), "corps", email.body());
    }

    @PostMapping("/envoyer") Map<String, Object> send(@RequestBody Map<String, Object> body) {
        var candidate = findCandidate(body); String decision = normalizeDecision(Objects.toString(body.get("decision"), ""));
        if (decision.equals("EnAttente")) throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Décision invalide.");
        var generated = groq.generateEmail(decision, candidate.nomCandidat, candidate.offre.titre, candidate.analyseIA == null ? "" : candidate.analyseIA.resumeCompetences);
        String subject = Objects.toString(body.get("sujet"), generated.subject()); String content = Objects.toString(body.get("corps"), generated.body());
        String url = decision.equals("Accepte") ? acceptUrl : refusUrl; boolean success = false; String error = null; int status = 0;
        try {
            var payload = new LinkedHashMap<String, Object>(); payload.put("candidature_id", candidate.id); payload.put("email_destinataire", candidate.emailCandidat); payload.put("nom_candidat", candidate.nomCandidat); payload.put("poste", candidate.offre.titre); payload.put("decision", decision); payload.put("sujet", subject); payload.put("corps_email", content);
            var response = client.post().uri(url).header("X-N8N-Secret", n8nSecret).body(payload).retrieve().toBodilessEntity(); success = response.getStatusCode().is2xxSuccessful(); status = response.getStatusCode().value();
        } catch (Exception exception) { error = exception.getMessage(); }
        if (body.get("mettreAJourStatut") == null || Boolean.TRUE.equals(body.get("mettreAJourStatut"))) candidate.statut = decision;
        candidate.emailReponseEnvoye = success; candidatures.save(candidate);
        var log = new EmailLogEntity(); log.candidature = candidate; log.typeDecision = decision.equals("Accepte") ? "acceptation" : "refus"; log.sujet = subject; log.corps = content; log.destinataire = candidate.emailCandidat; log.reussi = success; log.erreur = error; logs.save(log);
        return Map.of("success", success, "error", Objects.toString(error, ""), "httpStatusCode", status);
    }

    private CandidatureEntity findCandidate(Map<String, Object> body) { String id = Objects.toString(body.get("candidatureId"), ""); return candidatures.findById(id).orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Candidature introuvable.")); }
    private static String normalizeDecision(String value) { String normalized = value.toLowerCase(Locale.ROOT); if (normalized.contains("accept")) return "Accepte"; if (normalized.contains("refus") || normalized.contains("reject")) return "Refuse"; return "EnAttente"; }
}
