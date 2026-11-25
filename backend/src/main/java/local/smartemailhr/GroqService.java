package local.smartemailhr;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestClient;
import java.util.*;

@Component
class GroqService {
    private final RestClient client; private final ObjectMapper mapper; private final String key, model, base;
    GroqService(ObjectMapper mapper, @Value("${app.groq.api-key}") String key, @Value("${app.groq.model}") String model, @Value("${app.groq.base-url}") String base) { this.mapper = mapper; this.key = key; this.model = model; this.base = base; this.client = RestClient.builder().build(); }

    AnalysisData analyze(OffreEntity offer, String cv) {
        if (key.isBlank()) return fallback(offer, cv);
        String prompt = "Analyse ce CV pour l'offre " + offer.titre + ". Description: " + offer.description + ". Compétences demandées: " + offer.competencesRequises + ". CV: " + cv + ". Réponds en JSON: score entier, resume_competences, competences_detectees tableau, classification, coherence_poste booléen, decision_suggeree.";
        try {
            var parsed = chat(prompt, true); return new AnalysisData(Math.max(0, Math.min(100, ((Number) parsed.getOrDefault("score", 0)).intValue())), Objects.toString(parsed.get("resume_competences"), "Analyse non disponible."), stringList(parsed.get("competences_detectees")), Objects.toString(parsed.get("classification"), offer.domaine), Boolean.TRUE.equals(parsed.get("coherence_poste")), normalizeSuggestion(Objects.toString(parsed.get("decision_suggeree"), "")));
        } catch (Exception ignored) { return fallback(offer, cv); }
    }

    EmailData generateEmail(String decision, String candidate, String title, String summary) {
        String subject = decision.equals("Accepte") ? "Suite favorable - " + title : "Retour sur votre candidature - " + title;
        String body = "Bonjour " + candidate + ",\n\nMerci pour votre candidature au poste de " + title + ". Après étude de votre profil, nous vous informons de notre décision : " + decision + ".\n\n" + summary + "\n\nCordialement,\nL'équipe RH SmartEmail";
        if (key.isBlank()) return new EmailData(subject, body);
        try { var result = chat("Rédige un courriel RH professionnel en français pour " + candidate + ", décision " + decision + ", poste " + title + ", évaluation " + summary + ". Réponds en JSON avec sujet et corps.", true); return new EmailData(Objects.toString(result.get("sujet"), subject), Objects.toString(result.get("corps"), body)); }
        catch (Exception ignored) { return new EmailData(subject, body); }
    }

    AnalysisData fallback(OffreEntity offer, String cv) {
        List<String> skills = parseSkills(offer.competencesRequises); String normalized = Objects.toString(cv, "").toLowerCase(Locale.ROOT);
        var matched = skills.stream().filter(skill -> normalized.contains(skill.toLowerCase(Locale.ROOT))).toList(); int score = skills.isEmpty() ? 55 : matched.size() * 100 / skills.size();
        String summary = matched.isEmpty() ? "Le CV contient peu d'éléments alignés sur les compétences demandées." : "Compétences pertinentes : " + String.join(", ", matched);
        return new AnalysisData(score, summary, matched, offer.domaine, score >= 45, score >= 75 ? "Accepté" : score >= 50 ? "À examiner" : "Refusé");
    }

    private Map<String, Object> chat(String prompt, boolean json) throws Exception {
        var payload = new LinkedHashMap<String, Object>(); payload.put("model", model); payload.put("temperature", 0.2); if (json) payload.put("response_format", Map.of("type", "json_object"));
        payload.put("messages", List.of(Map.of("role", "user", "content", prompt)));
        var response = client.post().uri(base + "/chat/completions").header("Authorization", "Bearer " + key).body(payload).retrieve().body(Map.class);
        var choices = (List<Map<String, Object>>) response.get("choices"); String content = Objects.toString(((Map<?, ?>) choices.get(0).get("message")).get("content"), "{}");
        int start = content.indexOf('{'), end = content.lastIndexOf('}'); if (start >= 0 && end > start) content = content.substring(start, end + 1);
        return mapper.readValue(content, Map.class);
    }

    private List<String> stringList(Object value) { if (value instanceof Collection<?> collection) return collection.stream().map(Object::toString).toList(); return List.of(); }
    private List<String> parseSkills(String value) { try { return stringList(mapper.readValue(value, List.class)); } catch (Exception ignored) { return Arrays.stream(Objects.toString(value, "").split(",")).filter(s -> !s.isBlank()).toList(); } }
    private String normalizeSuggestion(String raw) { String v = raw.toLowerCase(Locale.ROOT); if (v.contains("accept")) return "Accepté"; if (v.contains("refus") || v.contains("reject")) return "Refusé"; return "À examiner"; }
}

record AnalysisData(int score, String resume, List<String> skills, String classification, boolean coherent, String suggestion) {}
record EmailData(String subject, String body) {}

