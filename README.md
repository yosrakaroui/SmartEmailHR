# SmartEmail HR

SmartEmail HR est une application de gestion du recrutement. Elle permet de gérer des offres d'emploi et des candidatures, d'analyser les CV avec Groq, de suivre des statistiques et d'automatiser la réception et l'envoi de courriels avec n8n.

## Fonctionnalités

- Authentification avec jetons JWT et rôles RH / administrateur.
- Création, consultation, modification et désactivation d'offres.
- Réception et suivi des candidatures associées aux offres.
- Analyse des CV et génération de courriels avec Groq.
- Statistiques sur les candidatures, les courriels et les candidats.
- Automatisation des workflows de réception et de réponse par n8n.
- Documentation interactive de l'API avec Swagger UI.

## Technologies

- Interface : Angular 21
- API : Java 17 et Spring Boot 3
- Compilation du backend : Maven
- Persistance : Spring Data JPA / Hibernate
- Base de données : MySQL ou MariaDB
- Sécurité : Spring Security et JWT
- Documentation API : OpenAPI / Swagger
- Analyse assistée : API Groq
- Automatisation : n8n
- Vérifications backend : JUnit 5 / Mockito

## Organisation du dépôt

```text
SmartEmailHR/
├── backend/       API Spring Boot, configuration et tests
├── frontend/      Application web Angular
├── database/      Script SQL de création de la base
├── n8n/           Workflows n8n importables
└── README.md
```

## Prérequis sur Windows

Installez les outils suivants et vérifiez qu'ils sont accessibles depuis PowerShell :

- Java Development Kit (JDK) 17 ou supérieur (`java -version`).
- Maven (`mvn -version`).
- Node.js et npm (`node --version`, `npm --version`).
- MySQL ou MariaDB en cours d'exécution.
- n8n si vous souhaitez utiliser les automatisations de courriels.
- Une clé API Groq pour activer l'analyse des CV et la génération de contenu par IA.

Les étapes ci-dessous supposent que PowerShell est ouvert à la racine du dépôt.

## 1. Préparer MySQL ou MariaDB

Créez une base nommée `smartemailhr`. Vous pouvez importer [`database/init.sql`](database/init.sql) dans phpMyAdmin ou l'exécuter avec le client MySQL. L'application utilise Hibernate avec `ddl-auto=update` et crée ou met à jour ses tables au démarrage. Le script décrit le schéma MySQL/MariaDB avec les clés étrangères entre utilisateurs, offres, candidatures, analyses et journaux d'emails.

Par défaut, l'application se connecte à `localhost:3309`, utilisateur `root`, mot de passe `root`. Adaptez le port et les identifiants à votre installation. Par exemple, pour définir les paramètres dans la session PowerShell courante :

```powershell
$env:DB_URL = "jdbc:mysql://localhost:3306/smartemailhr?useUnicode=true&characterEncoding=utf8&serverTimezone=UTC"
$env:DB_USERNAME = "root"
$env:DB_PASSWORD = "votre-mot-de-passe"
```

Le port `3306` est un exemple courant ; utilisez le port configuré sur votre machine.

## 2. Configurer et démarrer l'API

Les valeurs par défaut et les paramètres de configuration se trouvent dans `backend/src/main/resources/application.properties`. Les variables d'environnement suivantes peuvent les remplacer. `JWT_SECRET` doit contenir au moins 32 caractères ; configurez-le ainsi que le secret n8n avant le premier usage.

| Variable | Usage | Valeur par défaut |
|---|---|---|
| `SERVER_PORT` | Port de l'API | `5000` |
| `DB_URL` | URL JDBC de MySQL/MariaDB | `jdbc:mysql://localhost:3309/smartemailhr?...` |
| `DB_USERNAME` | Utilisateur de la base | `root` |
| `DB_PASSWORD` | Mot de passe de la base | `root` |
| `JWT_SECRET` | Signature des jetons JWT, au moins 32 caractères | Valeur de développement à remplacer |
| `N8N_SHARED_SECRET` | Secret partagé avec n8n | Valeur de développement à remplacer |
| `GROQ_API_KEY` | Clé de l'API Groq | Vide |
| `GROQ_MODEL` | Modèle Groq utilisé | `llama-3.3-70b-versatile` |
| `GROQ_BASE_URL` | Adresse de l'API Groq | `https://api.groq.com/openai/v1` |
| `N8N_ACCEPTATION_URL` | Webhook n8n pour une acceptation | `http://localhost:5678/webhook/smartemailhr/acceptation` |
| `N8N_REFUS_URL` | Webhook n8n pour un refus | `http://localhost:5678/webhook/smartemailhr/refus` |

Exemple de paramètres à définir avant le lancement :

```powershell
$env:JWT_SECRET = "remplacez-par-une-cle-secrete-d-au-moins-32-caracteres"
$env:N8N_SHARED_SECRET = "choisissez-un-secret-partage"
$env:GROQ_API_KEY = "votre-cle-api-groq"
```

Sans clé Groq, l'API utilise une analyse du CV par correspondance avec les compétences de l'offre et un modèle d'email simple. Avec une clé, l'analyse et les emails sont générés avec le modèle configuré. Les URLs n8n sont nécessaires à l'envoi réel de réponses par workflow.

Dans une première fenêtre PowerShell, démarrez le backend :

```powershell
Set-Location .\backend
mvn spring-boot:run
```

L'API est disponible à l'adresse `http://localhost:5000`. La documentation Swagger est à [`http://localhost:5000/swagger-ui/index.html`](http://localhost:5000/swagger-ui/index.html).

Au premier démarrage, si la table des utilisateurs est vide, l'application ajoute des comptes de démonstration et une offre. Les identifiants initiaux sont `admin@smartemailhr.local` / `Admin@123` pour l'administrateur et `rh@smartemailhr.local` / `Rh@123456` pour le compte RH. Remplacez ces identifiants et secrets avant tout usage réel.

## 3. Installer et démarrer Angular

Ouvrez une seconde fenêtre PowerShell à la racine du dépôt :

```powershell
Set-Location .\frontend
npm install
npm start
```

L'interface est disponible sur [`http://localhost:4200`](http://localhost:4200). L'adresse de l'API appelée par le frontend est configurée dans `frontend/src/environments/environment.ts` et pointe par défaut vers `http://localhost:5000/api`.

## 4. Configurer n8n

Les workflows à importer dans n8n sont dans le dossier `n8n/` :

- `workflow-reception-email.json` : réception et transmission des candidatures à l'API.
- `workflow-envoi-acceptation.json` : automatisation des courriels d'acceptation.
- `workflow-envoi-refus.json` : automatisation des courriels de refus.
- `workflow-offre-expiree.json` : traitement des offres expirées.

Importez les fichiers concernés dans votre instance n8n, configurez les accès et paramètres requis par les nœuds, puis activez les workflows. L'adresse du backend et le secret partagé doivent correspondre aux valeurs configurées côté API. Les URLs de webhooks d'acceptation et de refus sont réglables avec `N8N_ACCEPTATION_URL` et `N8N_REFUS_URL`.

## API

Les routes principales sont les suivantes. Swagger UI fournit le détail des paramètres et des schémas de requête.

| Domaine | Routes principales |
|---|---|
| Authentification | `POST /api/auth/login`, `GET /api/auth/me` |
| Offres | `/api/offres` : `GET`, `POST`, ainsi que `GET`, `PATCH`, `DELETE /{id}` |
| Candidatures | `POST /api/candidatures/recevoir`, `GET /api/candidatures`, `GET /api/candidatures/{id}`, `PATCH /api/candidatures/{id}/decision` |
| Courriels | `POST /api/emails/generer`, `POST /api/emails/envoyer` |
| Statistiques (administrateur) | `GET /api/stats/global`, `GET /api/stats/emails-recents`, `GET /api/stats/top-candidats` |
| Utilisateurs (administrateur) | `GET /api/users`, `POST /api/users`, `PATCH /api/users/{id}/status` |

Les routes protégées attendent l'en-tête `Authorization: Bearer <token>`. Les offres et les candidatures sont accessibles aux comptes RH et administrateur selon leurs droits ; les statistiques et la gestion des comptes sont réservées à l'administrateur. Les routes n8n de réception et de confirmation utilisent le secret partagé dans `X-N8N-Secret`. La route de décision modifie le statut ; l'envoi réel d'un email passe par `/api/emails/envoyer`.

## Workflows n8n

Les exports JSON de [`n8n/`](n8n/) sont fournis désactivés afin de configurer les connexions avant activation. Configurez les identifiants IMAP et SMTP/Gmail, puis définissez `SMARTEMAILHR_N8N_SECRET` dans l'environnement n8n avec la même valeur que `N8N_SHARED_SECRET` dans l'API. Le workflow de réception transmet l'objet du mail, le nom, l'adresse email et le texte extrait du CV. Les workflows d'acceptation et de refus envoient le message puis confirment le statut via l'API. Configurez l'accès réseau de n8n vers `http://localhost:5000` ou remplacez cette adresse selon votre installation.

## Dépannage rapide

- Si le backend ne démarre pas, vérifiez `java -version`, `mvn -version`, les identifiants MySQL et le port indiqué dans `DB_URL`.
- Si Angular ne joint pas l'API, vérifiez que les deux serveurs sont actifs et que `frontend/src/environments/environment.ts` contient l'URL du backend.
- Si l'analyse IA utilise le résultat simple, vérifiez `GROQ_API_KEY` et l'accès réseau à Groq.
- Si un email n'est pas envoyé, vérifiez que n8n est actif, que le workflow est activé, que ses identifiants sont valides et que les secrets correspondent.

## Vérifier l'installation

Pour compiler le backend :

```powershell
Set-Location .\backend
mvn clean package
```

Pour compiler le frontend :

```powershell
Set-Location .\frontend
npm run build
```

Pour démarrer l'ensemble, gardez le backend et le frontend actifs dans deux fenêtres PowerShell distinctes, et assurez-vous que le serveur MySQL/MariaDB est démarré. Les intégrations Groq et n8n doivent également être configurées et disponibles pour tester leurs fonctions.
