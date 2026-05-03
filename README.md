# SmartEmail HR

Plateforme intelligente de gestion du recrutement basee sur le cahier des charges v3:

- Frontend: Angular 17+ (SPA RH/Admin)
- Backend: ASP.NET Core 8 Web API
- Base de donnees: MySQL/MariaDB via XAMPP + phpMyAdmin
- IA: Groq API, compatible OpenAI API, avec modele `llama-3.3-70b-versatile`
- Automatisation: n8n pour reception emails, extraction CV et envoi des reponses

## 1. Etat actuel configure sur cette machine

Le projet est maintenant configure pour XAMPP/MariaDB:

```json
"Database": {
  "Provider": "MariaDb"
},
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3309;Database=smartemailhr;User=root;Password=root;"
}
```

La base `smartemailhr` a ete creee/importee avec [database/init.sql](C:/Users/PS/Documents/Codex/2026-04-22-files-mentioned-by-the-user-cahierdescharges/database/init.sql).

Comptes de demo:

- Admin: `admin@smartemailhr.local` / `Admin@123`
- RH: `rh@smartemailhr.local` / `Rh@123456`

## 2. Lancer MySQL/phpMyAdmin

1. Ouvrir XAMPP Control Panel.
2. Demarrer `Apache`.
3. Demarrer `MySQL`.
4. Ouvrir phpMyAdmin: `http://localhost/phpmyadmin`
5. Verifier que la base `smartemailhr` existe.

Si la base n'existe pas, importer le script:

```text
database/init.sql
```

Sur cette installation XAMPP, MariaDB ecoute sur le port `3309`. Si votre XAMPP utilise `3306`, modifier `appsettings.Development.json`:

```json
"DefaultConnection": "Server=localhost;Port=3306;Database=smartemailhr;User=root;Password=root;"
```

## 3. Configurer Groq

Créer une cle API sur Groq Console, puis renseigner [appsettings.Development.json](C:/Users/PS/Documents/Codex/2026-04-22-files-mentioned-by-the-user-cahierdescharges/backend/SmartEmailHR.API/appsettings.Development.json):

```json
"Groq": {
  "ApiKey": "VOTRE_CLE_GROQ",
  "Model": "llama-3.3-70b-versatile",
  "BaseUrl": "https://api.groq.com/openai/v1"
}
```

Recommandation: garder `llama-3.3-70b-versatile` pour la qualite des analyses CV. Si vous voulez une option plus rapide/legere, utilisez un modele Groq plus petit disponible dans votre console Groq.

Sans cle Groq, le backend continue en mode fallback local pour permettre la demonstration.

## 4. Lancer le backend

```powershell
cd C:\Users\PS\Documents\Codex\2026-04-22-files-mentioned-by-the-user-cahierdescharges
dotnet restore backend/SmartEmailHR.API/SmartEmailHR.API.csproj
dotnet run --project backend/SmartEmailHR.API/SmartEmailHR.API.csproj
```

API:

- `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`

Test rapide admin:

```powershell
$login = Invoke-RestMethod -Method Post -Uri "http://localhost:5000/api/auth/login" -ContentType "application/json" -Body '{"email":"admin@smartemailhr.local","motDePasse":"Admin@123"}'
$headers = @{ Authorization = "Bearer $($login.token)" }
Invoke-RestMethod -Method Get -Uri "http://localhost:5000/api/stats/global" -Headers $headers
```

## 5. Lancer le frontend

```powershell
cd C:\Users\PS\Documents\Codex\2026-04-22-files-mentioned-by-the-user-cahierdescharges\frontend
npm install
npm start
```

Application:

- `http://localhost:4200`
- `http://127.0.0.1:4200`

Le frontend appelle le backend via [environment.ts](C:/Users/PS/Documents/Codex/2026-04-22-files-mentioned-by-the-user-cahierdescharges/frontend/src/environments/environment.ts):

```ts
apiBaseUrl: 'http://localhost:5000/api'
```

## 6. Routes frontend

- Login: `http://127.0.0.1:4200/login`
- Dashboard RH: `http://127.0.0.1:4200/rh/dashboard`
- Creation offre: `http://127.0.0.1:4200/rh/offres/new`
- Modification offre: `http://127.0.0.1:4200/rh/offres/{id}/edit`
- Detail candidat: `http://127.0.0.1:4200/rh/candidatures/{id}`
- Dashboard admin: `http://127.0.0.1:4200/admin/dashboard`

Les routes RH/Admin sont protegees par JWT. Connectez-vous d'abord via `/login`.

## 7. Endpoints backend principaux

- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/offres`
- `POST /api/offres`
- `GET /api/offres/{id}`
- `PATCH /api/offres/{id}`
- `DELETE /api/offres/{id}`
- `POST /api/candidatures/recevoir` avec header `X-N8N-Secret`
- `GET /api/candidatures`
- `GET /api/candidatures/{id}`
- `PATCH /api/candidatures/{id}/decision`
- `POST /api/emails/generer`
- `POST /api/emails/envoyer`
- `GET /api/stats/global`
- `GET /api/stats/emails-recents`
- `GET /api/stats/top-candidats`
- `GET /api/users`
- `POST /api/users`
- `PATCH /api/users/{id}/status`

## 8. Workflows n8n

Fichiers importables:

- [workflow-reception-email.json](C:/Users/PS/Documents/Codex/2026-04-22-files-mentioned-by-the-user-cahierdescharges/n8n/workflow-reception-email.json)
- [workflow-envoi-acceptation.json](C:/Users/PS/Documents/Codex/2026-04-22-files-mentioned-by-the-user-cahierdescharges/n8n/workflow-envoi-acceptation.json)
- [workflow-envoi-refus.json](C:/Users/PS/Documents/Codex/2026-04-22-files-mentioned-by-the-user-cahierdescharges/n8n/workflow-envoi-refus.json)
- [workflow-offre-expiree.json](C:/Users/PS/Documents/Codex/2026-04-22-files-mentioned-by-the-user-cahierdescharges/n8n/workflow-offre-expiree.json)

Regle importante du cahier des charges: n8n ne contacte jamais Groq. n8n transporte les emails/CV vers le backend, puis le backend appelle Groq.

## 9. Verification effectuee

- Backend compile avec MySQL/MariaDB + Groq: `dotnet build` OK.
- Frontend compile: `npm run build` OK.
- MariaDB/XAMPP detecte sur `localhost:3309`.
- Base `smartemailhr` creee.
- Seed de demo insere: `2` utilisateurs, `3` offres.
- Login admin teste via API.
- Endpoint admin `GET /api/stats/global` teste avec JWT.

## 10. Depannage

- Dashboard admin bloque sur chargement: verifier que `http://localhost:5000` repond et que MariaDB est demarre.
- Erreur MySQL `Access denied`: verifier `User=root;Password=root` dans `appsettings.Development.json`.
- Erreur MySQL `Unknown database smartemailhr`: importer [database/init.sql](C:/Users/PS/Documents/Codex/2026-04-22-files-mentioned-by-the-user-cahierdescharges/database/init.sql) dans phpMyAdmin.
- Erreur Groq: verifier la cle dans `Groq.ApiKey`; le fallback local reste disponible si la cle est vide.
- Frontend sans donnees: verifier `frontend/src/environments/environment.ts` et CORS dans le backend.

