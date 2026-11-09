# SmartEmail HR

Plateforme de gestion du recrutement avec Angular, Spring Boot, MySQL/MariaDB, Groq et n8n.

## Backend Spring Boot

Prérequis : Java 17+, Maven et une base MySQL/MariaDB. Le backend écoute sur `http://localhost:5000`; Swagger UI est disponible sur `/swagger-ui/index.html`.

```powershell
cd backend
mvn spring-boot:run
```

Paramètres configurables par variables d'environnement (valeurs locales par défaut dans `backend/src/main/resources/application.properties`):

- `DB_URL` (par défaut `jdbc:mysql://localhost:3309/smartemailhr...`), `DB_USERNAME`, `DB_PASSWORD`
- `JWT_SECRET` (au moins 32 caractères), `N8N_SHARED_SECRET`
- `GROQ_API_KEY`, `GROQ_MODEL`, `GROQ_BASE_URL`
- `N8N_ACCEPTATION_URL`, `N8N_REFUS_URL`

Hibernate crée/met à jour les tables avec `ddl-auto=update`. Le premier démarrage ajoute un administrateur (`admin@smartemailhr.local` / `Admin@123`), un compte RH (`rh@smartemailhr.local` / `Rh@123456`) et une offre de démonstration si la table utilisateurs est vide. Changez les secrets et mots de passe de démonstration avant tout déploiement.

## Frontend

```powershell
cd frontend
npm install
npm start
```

Angular tourne sur `http://localhost:4200` et appelle l'API sur `http://localhost:5000/api`.

## API

- Authentification : `POST /api/auth/login`, `GET /api/auth/me`
- Offres : `GET/POST /api/offres`, `GET/PATCH/DELETE /api/offres/{id}`
- Candidatures : `POST /api/candidatures/recevoir` (secret `X-N8N-Secret`), `GET /api/candidatures`, `GET /api/candidatures/{id}`, `PATCH /api/candidatures/{id}/decision`
- Emails : `POST /api/emails/generer`, `POST /api/emails/envoyer`
- Statistiques : `GET /api/stats/global`, `/emails-recents`, `/top-candidats`
- Utilisateurs : `GET/POST /api/users`, `PATCH /api/users/{id}/status`

Les routes protégées utilisent `Authorization: Bearer <token>`. Les paramètres de connexion MariaDB par défaut sont adaptés à XAMPP sur le port `3309`; modifiez `DB_URL` si nécessaire. Le script de création de base existant est dans `database/init.sql`.

## Workflows n8n

Les workflows importables restent dans `n8n/`. n8n transmet les candidatures au backend et les réponses passent par les webhooks configurés pour acceptation et refus.
