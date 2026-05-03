# Frontend SmartEmail HR

## Lancement

```powershell
npm install
npm start
```

Application:

- `http://localhost:4200`
- `http://127.0.0.1:4200`

## Routes

- `/login`
- `/rh/dashboard`
- `/rh/offres/new`
- `/rh/offres/:id/edit`
- `/rh/candidatures/:id`
- `/admin/dashboard`

## Configuration API

Le frontend pointe vers le backend local:

```ts
apiBaseUrl: 'http://localhost:5000/api'
```

La documentation complete se trouve dans le README racine: `../README.md`.
