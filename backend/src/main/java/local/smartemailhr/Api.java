package local.smartemailhr;

import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.security.Keys;
import jakarta.servlet.FilterChain;
import jakarta.servlet.http.*;
import jakarta.validation.Valid;
import jakarta.validation.constraints.*;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.*;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.method.configuration.EnableMethodSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;
import org.springframework.stereotype.Component;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.filter.OncePerRequestFilter;
import org.springframework.web.client.RestClient;
import java.nio.charset.StandardCharsets;
import java.time.*;
import java.util.*;
import java.util.stream.*;

@Configuration
@EnableMethodSecurity
class SecurityConfig {
 @Bean BCryptPasswordEncoder passwordEncoder(){return new BCryptPasswordEncoder(12);}
 @Bean JwtFilter jwtFilter(@Value("${app.jwt.secret}") String secret,@Value("${app.jwt.issuer}") String issuer,@Value("${app.jwt.audience}") String audience){return new JwtFilter(secret,issuer,audience);}
 @Bean SecurityFilterChain filter(HttpSecurity http,JwtFilter jwt) throws Exception {
  return http.csrf(c->c.disable()).cors(c->c.configurationSource(req->{var x=new org.springframework.web.cors.CorsConfiguration();x.setAllowedOrigins(List.of("http://localhost:4200","http://127.0.0.1:4200"));x.setAllowedMethods(List.of("GET","POST","PATCH","DELETE","OPTIONS"));x.setAllowedHeaders(List.of("*"));return x;})).sessionManagement(s->s.sessionCreationPolicy(SessionCreationPolicy.STATELESS)).authorizeHttpRequests(a->a.requestMatchers("/api/auth/login","/api/candidatures/recevoir","/api/candidatures/*/email-envoye","/swagger-ui/**","/v3/api-docs/**").permitAll().anyRequest().authenticated()).addFilterBefore(jwt,UsernamePasswordAuthenticationFilter.class).build();
 }
}
class JwtFilter extends OncePerRequestFilter {
 private final String secret,issuer,audience; JwtFilter(String s,String i,String a){secret=s;issuer=i;audience=a;}
 protected void doFilterInternal(HttpServletRequest req,HttpServletResponse res,FilterChain chain)throws java.io.IOException, jakarta.servlet.ServletException {
  var auth=req.getHeader("Authorization"); if(auth!=null&&auth.startsWith("Bearer ")) try {var p=Jwts.parser().verifyWith(Keys.hmacShaKeyFor(secret.getBytes(StandardCharsets.UTF_8))).requireIssuer(issuer).requireAudience(audience).build().parseSignedClaims(auth.substring(7)).getPayload(); var authorities=List.of(new SimpleGrantedAuthority("ROLE_"+p.get("role",String.class))); SecurityContextHolder.getContext().setAuthentication(new UsernamePasswordAuthenticationToken(p.getSubject(),null,authorities));}catch(Exception ignored){}
  chain.doFilter(req,res);
 }
}

@RestController @RequestMapping("/api/auth")
class AuthApi {
 private final Users users; private final BCryptPasswordEncoder encoder; private final String secret,issuer,audience; private final long hours;
 AuthApi(Users u,BCryptPasswordEncoder e,@Value("${app.jwt.secret}")String s,@Value("${app.jwt.issuer}")String i,@Value("${app.jwt.audience}")String a,@Value("${app.jwt.expiration-hours}")long h){users=u;encoder=e;secret=s;issuer=i;audience=a;hours=h;}
 record Login(@Email @NotBlank String email,@NotBlank String motDePasse){} record Summary(String id,String nom,String email,String role,boolean actif){}
 @PostMapping("/login") ResponseEntity<?> login(@Valid @RequestBody Login r){var u=users.findByEmail(r.email().trim().toLowerCase()).orElse(null);if(u==null||!u.actif||!encoder.matches(r.motDePasse(),u.motDePasseHash))return ResponseEntity.status(401).body(Map.of("message","Identifiants invalides."));var exp=Instant.now().plus(Duration.ofHours(hours));var token=Jwts.builder().issuer(issuer).audience().add(audience).and().subject(u.id).claim("role",u.role).issuedAt(Date.from(Instant.now())).expiration(Date.from(exp)).signWith(Keys.hmacShaKeyFor(secret.getBytes(StandardCharsets.UTF_8))).compact();return ResponseEntity.ok(Map.of("token",token,"expireLe",exp,"utilisateur",summary(u)));}
 @GetMapping("/me") Summary me(org.springframework.security.core.Authentication a){return summary(users.findById((String)a.getPrincipal()).orElseThrow());}
 static Summary summary(UserEntity u){return new Summary(u.id,u.nom,u.email,u.role,u.actif);}
}

@RestController @RequestMapping("/api/offres")
class OffresApi {
 private final Offres offres;private final Candidatures candidatures; private final ObjectMapperHelper json=new ObjectMapperHelper();
 OffresApi(Offres o,Candidatures c){offres=o;candidatures=c;}
 @GetMapping List<Map<String,Object>> all(@RequestParam(required=false)String domaine,@RequestParam(required=false)String statut){return offres.findAllByOrderByDateCreationDesc().stream().filter(o->domaine==null||o.domaine.equalsIgnoreCase(domaine)).filter(o->statut==null||o.statut.equalsIgnoreCase(statut)).map(this::dto).toList();}
 @GetMapping("/{id}") ResponseEntity<?> one(@PathVariable String id){return offres.findById(id).<ResponseEntity<?>>map(o->{var m=dto(o);m.put("candidatures",candidatures.findByOffreId(id).stream().map(c->Map.of("id",c.id,"offreId",c.offreId,"nomCandidat",c.nomCandidat,"emailCandidat",c.emailCandidat,"statut",c.statut,"dateReception",c.dateReception)).toList());return ResponseEntity.ok(m);}).orElseGet(()->ResponseEntity.notFound().build());}
 @PostMapping ResponseEntity<?> create(@RequestBody Map<String,Object> b,org.springframework.security.core.Authentication a){var o=new OffreEntity();o.titre=str(b,"titre");o.description=str(b,"description");o.competencesRequises=json.skills(b.get("competencesRequises"));o.niveauExperience=str(b,"niveauExperience");o.domaine=str(b,"domaine");o.dateExpiration=date(b.get("dateExpiration"));o.creePar=(String)a.getPrincipal();o.statut="Active";return ResponseEntity.status(201).body(dto(offres.save(o)));}
 @PatchMapping("/{id}") ResponseEntity<?> update(@PathVariable String id,@RequestBody Map<String,Object>b){var o=offres.findById(id).orElse(null);if(o==null)return ResponseEntity.notFound().build();if(b.containsKey("titre"))o.titre=str(b,"titre");if(b.containsKey("description"))o.description=str(b,"description");if(b.containsKey("competencesRequises"))o.competencesRequises=json.skills(b.get("competencesRequises"));if(b.containsKey("niveauExperience"))o.niveauExperience=str(b,"niveauExperience");if(b.containsKey("domaine"))o.domaine=str(b,"domaine");if(b.containsKey("dateExpiration"))o.dateExpiration=date(b.get("dateExpiration"));if(b.containsKey("statut"))o.statut=str(b,"statut");return ResponseEntity.ok(dto(offres.save(o)));}
 @DeleteMapping("/{id}") ResponseEntity<?> delete(@PathVariable String id){if(!offres.existsById(id))return ResponseEntity.notFound().build();offres.deleteById(id);return ResponseEntity.noContent().build();}
 Map<String,Object> dto(OffreEntity o){var m=new LinkedHashMap<String,Object>();m.put("id",o.id);m.put("titre",o.titre);m.put("description",o.description);m.put("competencesRequises",json.readSkills(o.competencesRequises));m.put("niveauExperience",o.niveauExperience);m.put("domaine",o.domaine);m.put("dateExpiration",o.dateExpiration);m.put("statut",o.statut);m.put("dateCreation",o.dateCreation);m.put("creePar",o.creePar);m.put("nombreCandidatures",candidatures.findByOffreId(o.id).size());return m;}
 static String str(Map<String,Object>b,String k){return Objects.toString(b.get(k),"");}static LocalDateTime date(Object d){return d instanceof String s?LocalDate.parse(s.substring(0,10)).atStartOfDay():LocalDateTime.now().plusDays(30);}
}

@Component class ObjectMapperHelper {
 private final com.fasterxml.jackson.databind.ObjectMapper mapper=new com.fasterxml.jackson.databind.ObjectMapper();
 String skills(Object v){try{return mapper.writeValueAsString(v instanceof List<?>?v:List.of());}catch(Exception e){return "[]";}}
 List<?> readSkills(String s){try{return mapper.readValue(s, List.class);}catch(Exception e){return Arrays.stream(Objects.toString(s,"").split(",")).filter(x->!x.isBlank()).toList();}}
}

@RestController @RequestMapping("/api/candidatures")
class CandidaturesApi {
 private final Candidatures cs;private final Offres os;private final Analyses as;private final EmailLogs logs;private final String n8nSecret;private final ObjectMapperHelper json;
 CandidaturesApi(Candidatures c,Offres o,Analyses a,EmailLogs l,@Value("${app.n8n.shared-secret}")String s,ObjectMapperHelper j){cs=c;os=o;as=a;logs=l;n8nSecret=s;json=j;}
 @PostMapping("/recevoir") ResponseEntity<?> receive(@RequestHeader(value="X-N8N-Secret",required=false)String secret,@RequestBody Map<String,Object>b){if(n8nSecret.startsWith("CHANGE_ME")||!Objects.equals(n8nSecret,secret))return ResponseEntity.status(401).body(Map.of("message","Requête n8n non autorisée."));var subject=Objects.toString(b.get("objetEmail"),"");var offer=os.findAll().stream().max(Comparator.comparingInt(o->match(subject,o.titre))).orElse(null);if(offer==null||match(subject,offer.titre)==0)return ResponseEntity.badRequest().body(Map.of("message","Aucune offre correspondante trouvée pour l'objet de l'email."));var c=new CandidatureEntity();c.offreId=offer.id;c.nomCandidat=Objects.toString(b.get("nomCandidat"),Objects.toString(b.get("emailCandidat"),"Candidat"));c.emailCandidat=Objects.toString(b.get("emailCandidat"),"").toLowerCase();c.contenuCV=Objects.toString(b.get("contenuCv"),"");final String cvText=c.contenuCV;c.objetEmail=subject;c.cvUrl=(String)b.get("cvUrl");c.statut=offer.statut.equals("Active")&&offer.dateExpiration.toLocalDate().compareTo(LocalDate.now())>=0?"EnAttente":"Refuse";c=cs.save(c);var analysis=new AnalyseEntity();analysis.candidatureId=c.id;var skills=json.readSkills(offer.competencesRequises).stream().map(Object::toString).toList();var hit=skills.stream().filter(s->cvText.toLowerCase().contains(s.toLowerCase())).toList();analysis.score=skills.isEmpty()?55:hit.size()*100/skills.size();analysis.resumeCompetences=hit.isEmpty()?"Le CV contient peu d'elements alignes sur les competences demandees.":"Compétences pertinentes : "+String.join(", ",hit);analysis.competencesDetectees=json.skills(hit);analysis.classification=offer.domaine;analysis.coherencePoste=analysis.score>=45;analysis.decisionSuggeree=analysis.score>=75?"Accepté":analysis.score>=50?"À examiner":"Refusé";analysis=as.save(analysis);return ResponseEntity.ok(Map.of("candidatureId",c.id,"offreId",offer.id,"statut",c.statut,"score",analysis.score,"decisionSuggeree",analysis.decisionSuggeree,"offreFermee",c.statut.equals("Refuse")));}
 @GetMapping List<Map<String,Object>> all(@RequestParam(required=false)String offreId,@RequestParam(required=false)String statut,@RequestParam(required=false)String domaine,@RequestParam(required=false)String recherche){return cs.findAllByOrderByDateReceptionDesc().stream().filter(c->offreId==null||c.offreId.equals(offreId)).filter(c->statut==null||c.statut.equalsIgnoreCase(statut)).filter(c->domaine==null||os.findById(c.offreId).map(o->o.domaine.equalsIgnoreCase(domaine)).orElse(false)).filter(c->recherche==null||(c.nomCandidat+" "+c.emailCandidat).toLowerCase().contains(recherche.toLowerCase())).map(c->candidate(c,false)).toList();}
 @GetMapping("/{id}") ResponseEntity<?> one(@PathVariable String id){return cs.findById(id).<ResponseEntity<?>>map(c->ResponseEntity.ok(candidate(c,true))).orElseGet(()->ResponseEntity.notFound().build());}
 @PatchMapping("/{id}/decision") ResponseEntity<?> decision(@PathVariable String id,@RequestBody Map<String,Object>b){var c=cs.findById(id).orElse(null);if(c==null)return ResponseEntity.notFound().build();c.statut=norm(Objects.toString(b.get("decision"),"Refuse"));c=cs.save(c);return ResponseEntity.ok(Map.of("candidatureId",c.id,"statut",c.statut,"emailReponseEnvoye",c.emailReponseEnvoye,"sujetEmail",Objects.toString(b.get("sujetEmail"),""),"corpsEmail",Objects.toString(b.get("corpsEmail"),"")));}
 @PatchMapping("/{id}/email-envoye") ResponseEntity<?> emailStatus(@PathVariable String id,@RequestHeader(value="X-N8N-Secret",required=false)String s,@RequestBody Map<String,Object>b){if(!Objects.equals(n8nSecret,s))return ResponseEntity.status(401).build();var c=cs.findById(id).orElse(null);if(c==null)return ResponseEntity.notFound().build();c.emailReponseEnvoye=Boolean.TRUE.equals(b.get("emailReponseEnvoye"));cs.save(c);return ResponseEntity.ok(Map.of("message","Statut email mis à jour."));}
 Map<String,Object> candidate(CandidatureEntity c,boolean detail){var o=os.findById(c.offreId).orElse(null);var a=as.findByCandidatureId(c.id).orElse(null);var m=new LinkedHashMap<String,Object>();m.put("id",c.id);m.put("offreId",c.offreId);m.put("titreOffre",o==null?"":o.titre);m.put("domaine",o==null?"":o.domaine);m.put("nomCandidat",c.nomCandidat);m.put("emailCandidat",c.emailCandidat);m.put("dateReception",c.dateReception);m.put("statut",c.statut);m.put("emailReponseEnvoye",c.emailReponseEnvoye);m.put("cvUrl",c.cvUrl);m.put("analyseIA",a==null?null:Map.of("score",a.score,"resumeCompetences",a.resumeCompetences,"competencesDetectees",json.readSkills(a.competencesDetectees),"classification",a.classification,"coherencePoste",a.coherencePoste,"decisionSuggeree",a.decisionSuggeree,"dateAnalyse",a.dateAnalyse));if(detail){m.put("contenuCv",c.contenuCV);m.put("objetEmail",c.objetEmail);}return m;}
 static int match(String s,String t){var x=s.toLowerCase();if(x.contains(t.toLowerCase()))return 100;return (int)Arrays.stream(t.toLowerCase().split(" ")).filter(w->x.contains(w)).count()*10;}static String norm(String x){return x.toLowerCase().contains("accept")?"Accepte":x.toLowerCase().contains("refus")?"Refuse":"EnAttente";}
}

@RestController @RequestMapping("/api")
@org.springframework.security.access.prepost.PreAuthorize("hasRole('admin')")
class AdminApi {
 private final Users users;private final BCryptPasswordEncoder encoder;private final Candidatures cs;private final Offres os;private final Analyses as;private final EmailLogs logs;
 AdminApi(Users u,BCryptPasswordEncoder e,Candidatures c,Offres o,Analyses a,EmailLogs l){users=u;encoder=e;cs=c;os=o;as=a;logs=l;}
 @GetMapping("/users") List<?> users(){return users.findAll().stream().map(AuthApi::summary).toList();}
 @PostMapping("/users") ResponseEntity<?> create(@RequestBody Map<String,Object>b){String email=Objects.toString(b.get("email"),"").toLowerCase();if(users.findByEmail(email).isPresent())return ResponseEntity.badRequest().body(Map.of("message","Cet email est déjà utilisé."));var u=new UserEntity();u.nom=Objects.toString(b.get("nom"),"");u.email=email;u.motDePasseHash=encoder.encode(Objects.toString(b.get("motDePasse"),""));u.role=Objects.toString(b.get("role"),"rh").toLowerCase();return ResponseEntity.status(201).body(AuthApi.summary(users.save(u)));}
 @PatchMapping("/users/{id}/status") ResponseEntity<?> status(@PathVariable String id,@RequestBody Map<String,Object>b){var u=users.findById(id).orElse(null);if(u==null)return ResponseEntity.notFound().build();u.actif=Boolean.TRUE.equals(b.get("actif"));return ResponseEntity.ok(AuthApi.summary(users.save(u)));}
 @GetMapping("/stats/global") Map<String,Object> stats(){var all=cs.findAll();var result=new LinkedHashMap<String,Object>();result.put("totalCandidatures",all.size());result.put("candidaturesAcceptees",all.stream().filter(c->c.statut.equals("Accepte")).count());result.put("candidaturesRefusees",all.stream().filter(c->c.statut.equals("Refuse")).count());result.put("candidaturesEnAttente",all.stream().filter(c->c.statut.equals("EnAttente")).count());result.put("offresActives",os.findAll().stream().filter(o->o.statut.equals("Active")).count());result.put("offresExpirees",os.findAll().stream().filter(o->o.statut.equals("Expiree")).count());result.put("offresDesactivees",os.findAll().stream().filter(o->o.statut.equals("Desactivee")).count());result.put("statsParDomaine",List.of());result.put("topCandidats",List.of());result.put("faiblesScores",List.of());return result;}
 @GetMapping("/stats/emails-recents") List<?> recent(){return logs.findTop30ByOrderByDateEnvoiDesc();}
 @GetMapping("/stats/top-candidats") List<?> top(){return List.of();}
}

@RestController @RequestMapping("/api/emails")
@org.springframework.security.access.prepost.PreAuthorize("hasAnyRole('rh','admin')")
class EmailsApi {
 private final Candidatures cs;private final Offres os;private final Analyses as;private final EmailLogs logs;private final RestClient client=RestClient.create();private final String key,model,base,acceptUrl,refusUrl;
 EmailsApi(Candidatures c,Offres o,Analyses a,EmailLogs l,@Value("${app.groq.api-key}")String k,@Value("${app.groq.model}")String m,@Value("${app.groq.base-url}")String b,@Value("${app.n8n.acceptation-url}")String au,@Value("${app.n8n.refus-url}")String ru){cs=c;os=o;as=a;logs=l;key=k;model=m;base=b;acceptUrl=au;refusUrl=ru;}
 @PostMapping("/generer") Map<String,String> generate(@RequestBody Map<String,Object>b){var c=cs.findById(Objects.toString(b.get("candidatureId"),"")).orElseThrow();var o=os.findById(c.offreId).orElseThrow();String d=Objects.toString(b.get("decision"),"Refuse");String subject=(d.toLowerCase().contains("accept")?"Suite favorable - ":"Retour sur votre candidature - ")+o.titre;String body="Bonjour "+c.nomCandidat+",\n\nMerci pour votre candidature au poste de "+o.titre+". Après étude de votre profil, nous vous informons de notre décision : "+d+".\n\nCordialement,\nL'équipe RH SmartEmail";if(!key.isBlank())try{var payload=Map.of("model",model,"messages",List.of(Map.of("role","user","content","Rédige un email RH en français pour "+c.nomCandidat+", décision "+d+", poste "+o.titre+". Réponds en JSON {sujet,corps}.")));var response=client.post().uri(base+"/chat/completions").header("Authorization","Bearer "+key).body(payload).retrieve().body(Map.class);var choices=(List<Map>)response.get("choices");var content=(String)((Map)choices.get(0).get("message")).get("content");var parsed=new com.fasterxml.jackson.databind.ObjectMapper().readValue(content,Map.class);subject=Objects.toString(parsed.get("sujet"),subject);body=Objects.toString(parsed.get("corps"),body);}catch(Exception ignored){}return Map.of("sujet",subject,"corps",body);}
 @PostMapping("/envoyer") Map<String,Object> send(@RequestBody Map<String,Object>b){var c=cs.findById(Objects.toString(b.get("candidatureId"),"")).orElseThrow();var email=generate(b);String url=Objects.toString(b.get("decision"),"").toLowerCase().contains("accept")?acceptUrl:refusUrl;boolean ok=false;String error=null;Integer status=null;try{var response=client.post().uri(url).body(Map.of("candidatureId",c.id,"decision",Objects.toString(b.get("decision"),""),"emailDestinataire",c.emailCandidat,"sujet",b.getOrDefault("sujet",email.get("sujet")),"corpsEmail",b.getOrDefault("corps",email.get("corps")))).retrieve().toBodilessEntity();ok=response.getStatusCode().is2xxSuccessful();status=response.getStatusCode().value();}catch(Exception e){error=e.getMessage();}if(Boolean.TRUE.equals(b.getOrDefault("mettreAJourStatut",true))){c.statut=Objects.toString(b.get("decision"),"").toLowerCase().contains("accept")?"Accepte":"Refuse";c.emailReponseEnvoye=ok;cs.save(c);}var log=new EmailLogEntity();log.candidatureId=c.id;log.typeDecision=c.statut.equals("Accepte")?"acceptation":"refus";log.sujet=email.get("sujet");log.corps=email.get("corps");log.destinataire=c.emailCandidat;log.reussi=ok;log.erreur=error;logs.save(log);return Map.of("success",ok,"error",Objects.toString(error,""),"httpStatusCode",status==null?0:status);}
}

@Component class DemoData implements org.springframework.boot.CommandLineRunner {
 private final Users users;private final Offres offres;private final BCryptPasswordEncoder encoder; DemoData(Users u,Offres o,BCryptPasswordEncoder e){users=u;offres=o;encoder=e;}
 public void run(String...args){if(users.count()>0)return;var a=new UserEntity();a.id="10000000-0000-0000-0000-000000000001";a.nom="Admin SmartEmail";a.email="admin@smartemailhr.local";a.motDePasseHash=encoder.encode("Admin@123");a.role="admin";users.save(a);var r=new UserEntity();r.id="10000000-0000-0000-0000-000000000002";r.nom="Responsable RH Demo";r.email="rh@smartemailhr.local";r.motDePasseHash=encoder.encode("Rh@123456");r.role="rh";users.save(r);var o=new OffreEntity();o.id="20000000-0000-0000-0000-000000000001";o.titre="Développeur Frontend Angular";o.description="Concevoir des interfaces Angular performantes et maintenables.";o.competencesRequises="[\"Angular\",\"TypeScript\",\"HTML\",\"CSS\",\"RxJS\"]";o.niveauExperience="Junior";o.domaine="Développement Web";o.dateExpiration=LocalDate.now().plusDays(30).atStartOfDay();o.creePar=r.id;offres.save(o);}
}
