# TaskBoard

Application de gestion de tâches type Kanban en ASP.NET Core 10, avec authentification JWT, temps réel SignalR et frontend HTML/JS intégré.

---

## Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [dotnet-ef](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (outil CLI Entity Framework)
- SQLite (inclus via le package NuGet)
- Git

---

## Installation

### 1. Cloner le repo

```bash
git clone https://github.com/thomaspeyr31/Csharp.git
cd Csharp
```

### 2. Restaurer les packages NuGet

```bash
dotnet restore
```

### 3. Installer l'outil EF Core (si pas déjà fait)

```bash
dotnet tool install --global dotnet-ef
```

---

## Configuration

### Clé JWT (obligatoire)

L'application refuse de démarrer sans une clé JWT configurée. Utilisez les User Secrets (jamais dans appsettings.json) :

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "une-cle-secrete-de-32-caracteres-minimum-obligatoire"
```

La clé doit faire **au moins 32 caractères**. Les autres paramètres JWT sont déjà dans `appsettings.json` :

```json
{
  "Jwt": {
    "Issuer": "TaskBoard",
    "Audience": "TaskBoardClient"
  }
}
```

---

## Migration de la base de données

Appliquer toutes les migrations pour créer le fichier `taskboard.db` :

```bash
dotnet ef database update
```

Pour repartir de zéro (supprimer et recréer la BDD) :

```bash
rm taskboard.db
dotnet ef database update
```

---

## Lancement

```bash
dotnet run
```

L'application démarre sur :
- **HTTP** : http://localhost:5020
- **HTTPS** : https://localhost:7263

### Accès

| URL | Description |
|-----|-------------|
| http://localhost:5020 | Interface frontend (Kanban) |
| http://localhost:5020/swagger | Documentation API interactive |

---

## Créer un premier compte

Une fois l'application lancée, dans un nouveau terminal :

```bash
curl -X POST http://localhost:5020/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"username":"thomas","email":"thomas@demo.fr","password":"secret123"}'
```

Puis connectez-vous sur http://localhost:5020 avec ces identifiants.

---

## Tests

```bash
dotnet test ./TaskBoard.slnx
```

Les tests couvrent :
- Authentification et JWT (rôle, durée, audience)
- Refresh token (rotation, révocation, expiration)
- Contrôle d'ownership (403 si accès à une ressource d'un autre utilisateur)
- Validation des DTOs (400 si champs invalides)
- Partage de workspace (invitation, rôles Owner/Member)
- Champs enrichis des cartes (labels, membres, commentaires)
- SignalR (broadcast temps réel)
- Fichiers statiques (wwwroot)

---

## Structure du projet

```
Csharp/
├── Controllers/          # Points d'entrée HTTP (thin controllers)
│   ├── AuthController.cs
│   ├── UserController.cs
│   ├── WorkspaceController.cs
│   ├── BoardController.cs
│   ├── ListController.cs
│   ├── CardController.cs
│   ├── CommentController.cs
│   └── LabelController.cs
├── Services/             # Logique métier
│   ├── AuthService.cs
│   ├── WorkspaceService.cs
│   ├── BoardService.cs
│   ├── CardService.cs
│   ├── ListService.cs
│   ├── LabelService.cs
│   ├── CommentService.cs
│   ├── UserService.cs
│   ├── MembershipService.cs
│   └── BoardLookup.cs
├── Models/               # Entités EF Core
│   ├── User.cs
│   ├── Workspace.cs
│   ├── WorkspaceMember.cs
│   ├── Board.cs
│   ├── BoardList.cs
│   ├── Card.cs
│   ├── CardLabel.cs
│   ├── CardMember.cs
│   ├── Comment.cs
│   ├── Label.cs
│   ├── RefreshToken.cs
│   └── Dto/              # DTOs d'entrée/sortie
├── Data/
│   └── AppDbContext.cs   # Contexte EF Core + configuration FluentAPI
├── Hubs/
│   └── BoardHub.cs       # Hub SignalR temps réel
├── Common/
│   ├── ServiceResult.cs  # Pattern résultat typé (Ok/NotFound/Forbidden/BadRequest)
│   └── ServiceResultExtensions.cs
├── Security/
│   └── CurrentUserExtensions.cs  # Helper pour récupérer l'userId depuis le JWT
├── Migrations/           # Migrations EF Core auto-générées
├── wwwroot/              # Frontend statique
│   ├── index.html
│   ├── css/styles.css
│   └── js/
│       ├── api.js        # Wrapper fetch + refresh automatique
│       ├── board.js      # Vue board + drag & drop
│       ├── views.js      # Routing des écrans
│       ├── realtime.js   # Client SignalR
│       └── main.js       # Point d'entrée DOM
├── Tests/                # Tests xUnit
├── Program.cs
├── TaskBoard.csproj
└── TaskBoard.slnx
```

---

## Architecture

### Couches applicatives

```
Frontend (wwwroot/)
       ↓ HTTP / WebSocket
Controllers (thin — mapping DTO ↔ service)
       ↓
Services (logique métier, ownership, broadcast SignalR)
       ↓
AppDbContext → SQLite (EF Core)
```

### Sécurité

- **JWT** : access token 15 min, signé HMAC-SHA256, audience `TaskBoardClient`, claim `sub` + `role`
- **Refresh token** : opaque, hashé SHA-256 en BDD, durée 7 jours, rotation à chaque refresh
- **Ownership** : chaque action vérifie la chaîne `Card → List → Board → Workspace → WorkspaceMember(userId)` via `MembershipService`
- **Clé secrète** : jamais dans le code source, stockée dans User Secrets ou variable d'environnement `Jwt__Key`

### Temps réel (SignalR)

- Le client rejoint un groupe `board-{boardId}` via `JoinBoard(boardId)`
- À chaque création/modification/suppression d'une carte, liste ou commentaire, le service broadcast l'événement à tous les clients du groupe
- Le JWT est transmis via query string `?access_token=...` (WebSockets ne supportent pas les headers)

---

## Variables d'environnement

| Variable | Description | Exemple |
|----------|-------------|---------|
| `Jwt__Key` | Clé de signature JWT | `ma-super-cle-secrete-32-chars` |
| `Jwt__Issuer` | Émetteur du token | `TaskBoard` |
| `Jwt__Audience` | Audience du token | `TaskBoardClient` |

---

## Accès depuis un autre appareil (réseau local)

Pour partager l'application sur le réseau local (démo temps réel) :

```bash
# Trouver son IP locale (Mac)
ipconfig getifaddr en0

# Trouver son IP locale (Windows)
ipconfig
```

Puis lancer avec l'URL explicite :

```bash
dotnet run --urls "http://0.0.0.0:5020"
```

Les autres appareils accèdent via `http://VOTRE_IP:5020`.

---

## Contributeurs

| Développeur | Commits |
|-------------|---------|
| thomas | Baseline, migrations EF, sécurité JWT, refresh token, ownership |
| lorenzo | Services layer, DTOs, champs carte enrichis, SignalR, frontend, docs |