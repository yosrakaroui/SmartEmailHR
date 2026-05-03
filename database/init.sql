-- SmartEmail HR - Script MySQL / phpMyAdmin compatible Entity Framework Core
-- Importez ce fichier dans phpMyAdmin ou executez-le dans l'onglet SQL.

CREATE DATABASE IF NOT EXISTS smartemailhr
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE smartemailhr;

CREATE TABLE IF NOT EXISTS Users (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    Nom VARCHAR(200) NOT NULL,
    Email VARCHAR(320) NOT NULL UNIQUE,
    MotDePasseHash VARCHAR(120) NOT NULL,
    Role VARCHAR(10) NOT NULL,
    DateCreation DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    Actif BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT CK_Users_Role CHECK (Role IN ('rh', 'admin'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Offres (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    Titre VARCHAR(200) NOT NULL,
    Description LONGTEXT NOT NULL,
    CompetencesRequises LONGTEXT NOT NULL,
    NiveauExperience VARCHAR(50) NOT NULL,
    Domaine VARCHAR(100) NOT NULL,
    DateExpiration DATETIME(6) NOT NULL,
    Statut VARCHAR(20) NOT NULL,
    DateCreation DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CreePar CHAR(36) NOT NULL,
    CONSTRAINT FK_Offres_Users_CreePar FOREIGN KEY (CreePar) REFERENCES Users(Id),
    CONSTRAINT CK_Offres_Statut CHECK (Statut IN ('Active', 'Expiree', 'Desactivee'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Candidatures (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    OffreId CHAR(36) NOT NULL,
    NomCandidat VARCHAR(200) NOT NULL,
    EmailCandidat VARCHAR(320) NOT NULL,
    ContenuCV LONGTEXT NOT NULL,
    ObjetEmail VARCHAR(260) NOT NULL,
    DateReception DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    Statut VARCHAR(20) NOT NULL,
    EmailReponseEnvoye BOOLEAN NOT NULL DEFAULT FALSE,
    CvUrl VARCHAR(1000) NULL,
    CONSTRAINT FK_Candidatures_Offres_OffreId FOREIGN KEY (OffreId) REFERENCES Offres(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Candidatures_Statut CHECK (Statut IN ('EnAttente', 'Accepte', 'Refuse'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AnalysesIA (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    CandidatureId CHAR(36) NOT NULL UNIQUE,
    Score INT NOT NULL,
    ResumeCompetences LONGTEXT NOT NULL,
    CompetencesDetectees LONGTEXT NOT NULL,
    Classification VARCHAR(100) NOT NULL,
    CoherencePoste BOOLEAN NOT NULL,
    DecisionSuggeree VARCHAR(20) NOT NULL,
    DateAnalyse DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT FK_AnalysesIA_Candidatures_CandidatureId FOREIGN KEY (CandidatureId) REFERENCES Candidatures(Id) ON DELETE CASCADE,
    CONSTRAINT CK_AnalysesIA_Score CHECK (Score >= 0 AND Score <= 100)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS EmailLogs (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    CandidatureId CHAR(36) NOT NULL,
    TypeDecision VARCHAR(20) NOT NULL,
    Sujet VARCHAR(260) NOT NULL,
    Corps LONGTEXT NOT NULL,
    Destinataire VARCHAR(320) NOT NULL,
    Reussi BOOLEAN NOT NULL,
    Erreur VARCHAR(2000) NULL,
    DateEnvoi DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT FK_EmailLogs_Candidatures_CandidatureId FOREIGN KEY (CandidatureId) REFERENCES Candidatures(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX IX_Offres_Domaine_Statut ON Offres(Domaine, Statut);
CREATE INDEX IX_Candidatures_OffreId_Statut ON Candidatures(OffreId, Statut);
CREATE INDEX IX_Candidatures_EmailCandidat ON Candidatures(EmailCandidat);
CREATE INDEX IX_AnalysesIA_Score ON AnalysesIA(Score);

