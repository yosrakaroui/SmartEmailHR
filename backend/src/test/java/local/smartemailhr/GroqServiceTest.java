package local.smartemailhr;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

class GroqServiceTest {
    @Test
    void fallbackAnalysisScoresMatchedSkillsAndReturnsSuggestion() {
        var service = new GroqService(new ObjectMapper(), "", "model", "https://example.test");
        var offer = new OffreEntity();
        offer.titre = "Développeur Java";
        offer.domaine = "Développement";
        offer.competencesRequises = "[\"Java\",\"Spring\",\"SQL\"]";

        var result = service.fallback(offer, "Expérience en Java et Spring Boot.");

        assertEquals(66, result.score());
        assertEquals("Développement", result.classification());
        assertEquals("À examiner", result.suggestion());
        assertEquals(2, result.skills().size());
        assertTrue(result.resume().contains("Java"));
    }

    @Test
    void fallbackAnalysisHandlesOffersWithoutSkills() {
        var service = new GroqService(new ObjectMapper(), "", "model", "https://example.test");
        var offer = new OffreEntity();
        offer.titre = "Analyste";
        offer.domaine = "Data";
        offer.competencesRequises = "[]";

        var result = service.fallback(offer, "CV sans compétences listées");

        assertEquals(55, result.score());
        assertTrue(result.skills().isEmpty());
        assertEquals("À examiner", result.suggestion());
    }
}
