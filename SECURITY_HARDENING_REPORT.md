# War of Machines — Security Hardening Report

Дата: 2026-11-19  
Статус: локальні зміни Phase 2–5 реалізовані; production deployment не виконано.

## 1. Підтверджені вразливості

Під час Phase 1 та реалізації були підтверджені такі проблеми:

- Unity HTTP-клієнти використовували `AcceptAllCertificates`, а частина API-логіки мала HTTP/local fallback.
- У FishNet prefab було вимкнено transport encryption, а authenticator не був підключений.
- Username/password передавалися через FishNet RPC до Dedicated Server.
- Частина `ServerRpc` приймала client-provided identity, `clientId` або User ID; matchmaking і gameplay RPC не мали єдиного server-side auth gate.
- `ServerPlayerSessions` мав fallback до `userId = 0` для реального гравця.
- `/unity-server/status` був last-writer-wins in-memory registry без server credentials і без стійкості до перезапуску API.
- Match endpoints використовували звичайну player authorization без server-only policy, ownership та idempotent finalization.
- JWT не вимагав issuer/audience validation, мав hardcoded fallback key, тривалий lifetime і не мав session revocation/refresh rotation.
- В API були відсутні достатні DTO bounds, rate limiting, централізований ProblemDetails error handling і trusted forwarded-header policy.
- Password/credential logging та публічна доступність частини mutation/admin маршрутів створювали додатковий ризик.

## 2. Реалізовані виправлення

### TLS та API

- Видалено `AcceptAllCertificates.cs` і його `.meta`; з Unity-коду прибрано certificate bypass, `CertificateHandler` та `ServerCertificateValidationCallback`.
- `Assets/Game/Scripts/API/HttpLink.cs` дозволяє лише HTTPS, перевіряє абсолютний base URL і production allowlist; випадкові HTTP, localhost або сторонні endpoints не приймаються у production.
- `Assets/Game/Scripts/UI/Helpers/StartServerButtons.cs` та API managers більше не встановлюють bypass certificate handler.
- `Program.cs` API вимагає JWT key з конфігурації, issuer, audience, signature і lifetime validation, `RequireHttpsMetadata`, 30-секундний clock skew та `SaveToken = false`.
- Додано built-in fixed-window rate limiting для `auth`: 10 запитів з IP за хвилину, без черги.
- Додано DTO length/range validation, body limit 256 KB, `ApiExceptionHandler` з ProblemDetails і безпечні production error responses.
- CORS за замовчуванням порожній і приймає лише явно налаштовані origins; Swagger доступний тільки у Development або при явному прапорці.
- Forwarded headers приймаються лише від налаштованих `KnownProxies`; IP із довільного `X-Forwarded-For` не використовується як trusted identity.
- Vehicle-link mutation endpoints переведені на admin policy; player JWT не отримує адміністративних прав.
- З AuthController прибрано логування password/JWT/credential; login і register повертають access token, refresh token та expiry.

### Sessions і connection tickets

- Додано `Models/UserSession.cs`: active session, hashed refresh token, expiry, revoke time і optimistic concurrency version.
- `Controllers/AuthController.cs` реалізує access JWT, refresh-token rotation і logout/revocation.
- Додано `Controllers/ConnectionTicketsController.cs` з endpoint-ами `POST /connection-tickets` та `POST /connection-tickets/redeem`.
- Ticket генерується cryptographically secure random API, у PostgreSQL зберігається лише SHA-256 verification hash, ticket має короткий TTL, User ID, Session ID і Server ID.
- Redemption виконується тільки Dedicated Server policy, через execution strategy і транзакцію; version/concurrency check робить ticket одноразовим і захищає replay/concurrent redemption.
- Для server-side API operations використовується короткоживучий delegated token, а не player refresh token.

### Dedicated Server identity і registry

- Додано `Models/DedicatedServer.cs` зі стабільним `ServerId`, credential hash, region, address, UDP port, capacity, current players, active matches, availability і last heartbeat.
- Додано `Infrastructure/DedicatedServerAuthenticationHandler.cs` та `DedicatedServerCredentialValidator.cs` з окремою authentication scheme `DedicatedServer`.
- Credential передається через server-only headers, у БД зберігається verification hash, порівняння виконується constant-time; player JWT не може пройти цю policy.
- Захищені endpoints:
  - `POST /unity-server/register`
  - `PUT /unity-server/{serverId}/heartbeat`
  - `POST /unity-server/{serverId}/unregister`
- `GET /unity-server/status` залишено discovery endpoint-ом; він читає активні записи з PostgreSQL і виключає сервери після heartbeat timeout.
- Route `serverId` завжди звіряється з authenticated server claim; heartbeat чужого сервера відхиляється.
- `Assets/Game/Scripts/UI/Helpers/StartServerButtons.cs` оновлено для credential-authenticated registration, heartbeat, unregister і discovery.

### Match security

- `Controllers/MatchesController.cs` обмежує start/end server-only policy.
- Match зберігає `DedicatedServerId`; завершити його може лише той самий authenticated server.
- Start перевіряє існування і склад roster, унікальність позитивних User ID, ownership vehicle та match input bounds.
- End звіряє авторитетний roster, не приймає сторонніх учасників, виконує фіналізацію транзакційно і позначає `MatchParticipant.IsFinalized`.
- Повторний End повертає вже збережений результат без повторних нагород; concurrency version і DB unique indexes захищають від конкурентного дублювання.
- `Match.cs`, `MatchParticipant.cs` та `Data/AppDbContext.cs` містять server ownership/version/index constraints без зміни формул нагород.

### FishNet, DTLS і RPC

- `Assets/Game/Scripts/Networking/Authentication/ConnectionTicketAuthenticator.cs` фактично підключений до FishNet `NetworkManager` prefab. Він надсилає ticket після старту захищеного connection, виконує server-side redeem, має 10-секундний auth timeout і відхиляє invalid/expired/replayed ticket.
- `Assets/Game/Scripts/Networking/Authentication/SecureFishNetTransportConfiguration.cs` вмикає `UseEncryption` і використовує підтримані FishyUnityTransport `SetServerSecrets`/`SetClientSecrets`. Відсутні cert/key не перетворюються на insecure fallback: transport має fail-closed поведінку.
- `Assets/Game/Content/Networking/Prefabs/NetworkManager.prefab` тепер має `_authenticator` і encryption enabled.
- `ServerPlayerSessions` має єдиний `TryGetAuthenticated`; реальний гравець більше не отримує `userId = 0` fallback.
- Критичні RPC у `LobbyManager`, `GameplaySpawner`, `ProfileServer`, `DevelopmentTree`, `RobotView`, `BattleCountdownAuthority`, `NetworkWeaponShooter` і `VehicleInputController` перевіряють authenticated server session, використовують реальний FishNet sender і зберігають ownership/state checks.
- `GameplaySpawner` більше не приймає client-provided `clientId` для disconnect/scene-loaded; sender визначається з FishNet network context.
- `MatchesManager` і `MatchResultService` використовують dedicated-server match contracts; player JWT не використовується для mutation match operations.
- Production host-mode bypass вимкнено: auto-auth дозволено тільки в `UNITY_EDITOR`/`DEVELOPMENT_BUILD`; production host connection зупиняється без ticket.

## 3. Змінені файли

### Unity Client / Dedicated Server

- `Assets/Game/Content/Networking/Prefabs/NetworkManager.prefab`
- `Assets/Game/Scripts/API/ClientAuthenticationState.cs`
- `Assets/Game/Scripts/API/HttpLink.cs`
- `Assets/Game/Scripts/API/Models/TokenResponse.cs`
- `Assets/Game/Scripts/API/Endpoints/LeaderboardManager.cs`
- `Assets/Game/Scripts/API/Endpoints/MapsManager.cs`
- `Assets/Game/Scripts/API/Endpoints/MatchesManager.cs`
- `Assets/Game/Scripts/API/Endpoints/PlayersManager.cs`
- `Assets/Game/Scripts/API/Endpoints/RegisterManager.cs`
- `Assets/Game/Scripts/API/Endpoints/UserVehiclesManager.cs`
- `Assets/Game/Scripts/API/Endpoints/VehiclesManager.cs`
- `Assets/Game/Scripts/API/ServerManagers/ProfileServer.cs`
- `Assets/Game/Scripts/API/ServerManagers/RegisterServer.cs`
- `Assets/Game/Scripts/Networking/Authentication/ConnectionTicketAuthenticator.cs`
- `Assets/Game/Scripts/Networking/Authentication/SecureFishNetTransportConfiguration.cs`
- `Assets/Editor/LocalNetworkBuild.cs`
- `Assets/Game/Scripts/Networking/Lobby/GameplaySpawner.cs`
- `Assets/Game/Scripts/Networking/Lobby/LobbyManager.cs`
- `Assets/Game/Scripts/Networking/Lobby/MatchResultService.cs`
- `Assets/Game/Scripts/Networking/Sessions/ServerPlayerSession.cs`
- `Assets/Game/Scripts/Networking/Sessions/ServerPlayerSessions.cs`
- `Assets/Game/Scripts/Gameplay/Battle/BattleCountdownAuthority.cs`
- `Assets/Game/Scripts/Gameplay/Robots/NetworkWeaponShooter.cs`
- `Assets/Game/Scripts/Gameplay/Robots/VehicleInputController.cs`
- `Assets/Game/Scripts/UI/Helpers/StartServerButtons.cs`
- `Assets/Game/Scripts/UI/MainMenu/RobotView.cs`
- `Assets/Game/Scripts/UI/Tree/DevelopmentTree.cs`
- Видалено `Assets/Game/Scripts/API/Helpers/AcceptAllCertificates.cs` та `.meta`.

### Backend API

- `Program.cs`
- `Controllers/AuthController.cs`
- `Controllers/ConnectionTicketsController.cs`
- `Controllers/MatchesController.cs`
- `Controllers/UnityServerStatusController.cs`
- `Controllers/VehiclesController.cs`
- `Data/AppDbContext.cs`
- `Infrastructure/ApiExceptionHandler.cs`
- `Infrastructure/DedicatedServerAuthenticationHandler.cs`
- `Infrastructure/DedicatedServerCredentialValidator.cs`
- `Infrastructure/JwtTokenService.cs`
- `Models/ConnectionTicket.cs`
- `Models/DedicatedServer.cs`
- `Models/UserSession.cs`
- `Models/Match.cs`
- `Models/MatchParticipant.cs`
- `Migrations/20261119085129_SecurityHardeningFoundation.cs`
- `Migrations/20261119085129_SecurityHardeningFoundation.Designer.cs`
- `Migrations/AppDbContextModelSnapshot.cs`
- `tools/Initialize-LocalSecurityEnvironment.ps1`
- `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`, `.env.local.example`
- Оновлено відповідні документи в `docs/codex-app/`.
- `C:\Users\TorasPC\Desktop\Start War of Machines.bat` — локальний launcher; не є production-конфігурацією.

## 4. Фінальна архітектура авторизації

1. Unity Client виконує login/register через HTTPS Backend API.
2. API створює UserSession і повертає короткоживучий access JWT та refresh token.
3. Refresh token у БД зберігається тільки як hash; кожен refresh обертає token і збільшує session version.
4. Client запитує ticket для конкретного активного `ServerId` через HTTPS із player access JWT.
5. Client підключається до FishNet Dedicated Server із DTLS encryption і після встановлення connection надсилає одноразовий ticket.
6. Dedicated Server redeem-ить ticket через HTTPS API з власним server credential.
7. API атомарно перевіряє TTL, hash, User ID, Session ID, Server ID і usage state, після чого видає короткий delegated result.
8. FishNet Authenticator створює server-side player session; тільки після цього доступні захищені RPC.
9. Disconnect очищує session state; reconnect вимагає нового ticket.

Паролі, refresh tokens, player JWT і connection tickets не передаються через незашифрований UDP.

## 5. Захищена реєстрація Dedicated Servers

Server identity розділена з player identity. Початкове trusted provisioning виконується через `DedicatedServers:Provisioned` environment/configuration або через попередньо створений PostgreSQL record; анонімний запит не може сам видати собі credential.

API зберігає registry у PostgreSQL. `lastHeartbeatUtc` та configured timeout визначають availability для discovery. Розрахунок адреси не довіряє довільному forwarded header: public address надходить із validated server registration payload і deployment configuration.

Для кількох серверів використовуються окремі записи та credentials; перезапуск API не втрачає registry.

## 6. Зміни API

- `POST /auth/register`, `POST /auth/login` — access/refresh session response; DTO limits і rate limiting.
- `POST /auth/refresh` — rotation із revocation/concurrency protection.
- `POST /auth/logout` — revoke поточної session.
- `POST /connection-tickets` — player-authenticated issue для активного target server.
- `POST /connection-tickets/redeem` — dedicated-server-only atomic redemption.
- `POST /unity-server/register`, `PUT /unity-server/{serverId}/heartbeat`, `POST /unity-server/{serverId}/unregister` — dedicated-server-only.
- `GET /unity-server/status` — PostgreSQL-backed discovery із timeout filtering.
- `POST /matches/start`, `POST /matches/{id}/end` — dedicated-server-only, server ownership, roster validation, idempotent end.
- Vehicle-link mutation routes — admin-only.

Сумісні read-only profile/vehicle/map контракти збережені. Небезпечні player-authenticated mutation contracts замінені на server-only там, де це потрібно для безпеки.

## 7. PostgreSQL та міграції

Додано additive EF Core migration:

`Migrations/20261119085129_SecurityHardeningFoundation.cs`

Вона додає server registry, user sessions, connection tickets, match server ownership/version та необхідні unique/index constraints, зокрема unique `(MatchId, UserId)` для учасників.

Міграцію застосовано тільки до ізольованої локальної Docker PostgreSQL БД `war_of_machines_local` на `127.0.0.1:5433`. Production database не змінювалася. Startup auto-migration за замовчуванням вимкнено; production migration виконується окремою погодженою процедурою.

## 8. Configuration та environment variables

Реальні значення не додаються до репозиторію.

### Backend API

- `DATABASE_URL` або `ConnectionStrings__DefaultConnection` — PostgreSQL connection string.
- `Jwt__Key` — випадковий signing key довжиною не менше 32 символів; не commit-ити.
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenMinutes`.
- `Authentication__RefreshTokenLifetimeDays`.
- `Authentication__ConnectionTicketLifetimeSeconds`.
- `Authentication__ServerDelegatedTokenMinutes`.
- `DedicatedServers__Provisioned__N__ServerId` і `DedicatedServers__Provisioned__N__Credential` для trusted bootstrap.
- `DedicatedServers__HeartbeatTimeoutSeconds`.
- `Cors__AllowedOrigins__N` — тільки фактичні browser origins, якщо вони потрібні.
- `ForwardedHeaders__KnownProxies__N` — тільки довірені reverse proxy IP.
- `Database__ApplyMigrationsOnStartup` і `Database__RunSeedOnStartup` — залишаються `false` у production.

### Dedicated Server / Unity

- `WOM_API_BASE` — allowlisted HTTPS API base.
- `WOM_SERVER_ID`, `WOM_SERVER_CREDENTIAL`, `WOM_SERVER_REGION`.
- `WOM_SERVER_ADDRESS`, `WOM_SERVER_PORT` — validated public server metadata.
- `WOM_UTP_SERVER_CERTIFICATE`, `WOM_UTP_SERVER_PRIVATE_KEY` — тільки Dedicated Server secret material.
- `WOM_UTP_SERVER_COMMON_NAME`, `WOM_UTP_CA_CERTIFICATE` — client-side DTLS identity/trust configuration.

JWT signing key, server credentials, DTLS private key і refresh/ticket values не логуються та не повинні зберігатися в Unity Assets, PlayerPrefs або Git.

## 9. Build та тестування

Виконано:

- `dotnet build WarOfMachinesAPI.csproj --no-restore` — успішно, 0 warnings, 0 errors.
- `dotnet ef database update` ізольованої локальної PostgreSQL — успішно до `20261119085129_SecurityHardeningFoundation`.
- API local smoke: register, profile, server register/discovery, ticket issue/redeem/replay rejection, concurrent ticket redemption, spoofed/foreign heartbeat rejection, player JWT denial, server match start/end, idempotent end, refresh rotation, old refresh rejection, logout revocation, unregister — усі перевірені сценарії пройшли.
- Auth rate-limit smoke — `401,401,401,401,401,401,401,401,401,429,429`; перевірено відсікання після ліміту.
- Поточні Unity sources скомпільовано через Unity Roslyn response files — exit code 0; залишилися тільки два pre-existing FishNet obsolete warnings у `UpgradeFromMirrorMenu.cs`.
- Static scan Unity/API не знайшов `AcceptAllCertificates`, `CertificateHandler`, `ServerCertificateValidationCallback` або небезпечний HTTP fallback у захищених API/auth шляхах.
- Повний `C:\Users\TorasPC\Desktop\Start War of Machines.bat` завершився з exit code 0: API слухав `7216`, Dedicated Server слухав UDP `7770`, `/unity-server/status` показав `local-server-1` online, Client build і public `client-transport.env` були скопійовані на Desktop.

Не виконано:

- Не виконано live two-client + Dedicated Server gameplay test: login, ticket admission, scene load, synchronization, стрільбу/пошкодження, match result і reconnect ще потребують окремої runtime regression перевірки.
- У репозиторії не було готового автоматизованого API/Unity test project; перевірки виконані локальними smoke/static/build сценаріями.

## 10. Невирішені ризики та обмеження

- Production JWT key, credentials і DTLS certificates мають бути ротовані/видані операційно. Секрет, який був у Git history, не ротувався автоматично.
- Перед публічним тестуванням потрібно перевірити реальний DTLS certificate format, hostname/SAN і deployment secrets на всіх Dedicated Servers.
- `DedicatedServers:Provisioned` є bootstrap-механізмом; для масштабного production provisioning потрібен контрольований secret manager/операційний процес.
- `HttpLink` навмисно має allowlist production host. Staging або інший production hostname потрібно додавати свідомою зміною конфігурації/коду, а не runtime fallback.
- Rate limiting є IP-based; account-level lockout/notification не додавався, щоб не змінювати існуючі auth contracts.
- Повну готовність gameplay runtime і reconnect ще потрібно підтвердити після відкриття Unity Editor project без lock.

## 11. Локальний запуск

1. Запустити ізольований PostgreSQL stack проєкту.
2. В API repository задати `DATABASE_URL`, `Jwt__Key`, `Jwt__Issuer` та `Jwt__Audience` у `.env.local` із `.env.local.example`.
3. Один раз виконати `dotnet ef database update` для локальної БД; не вмикати це автоматично для production.
4. Запустити `C:\Users\TorasPC\Desktop\Start War of Machines.bat`. Він створює локальний PEM certificate, server credential і process-scoped env поза репозиторіями.
5. Launcher стартує API на `https://localhost:7216`, будує Development-only Client/Dedicated Server, передає server identity/credential та DTLS secrets і копіює public client transport config до Desktop build.
6. Запустити `WarOfMachinesClient.exe` з Desktop build; виконати HTTPS login, server discovery і ticket request.
7. Editor/Development host-mode допускається для локальної сумісності; production host без ticket відхиляється.

## 12. Порядок production deployment

1. Погодити та виконати ротацію JWT key і всіх раніше exposed server credentials; старі значення відкликати.
2. Створити backup і прогнати additive migration на staging; перевірити rollback/restore procedure.
3. Налаштувати API secrets, `KnownProxies`, CORS origins, public HTTPS certificate і `AllowedHosts`.
4. Перевірити API rate limiting, ProblemDetails, Swagger disabled і server-only policies у staging.
5. Видати окремі credentials і DTLS certificate/key кожному Dedicated Server через захищений канал.
6. Оновити API, Dedicated Server і Unity Client узгодженими версіями; стару password-over-UDP схему не залишати як production fallback.
7. Запустити smoke/security tests для login, ticket replay, heartbeat spoofing, match ownership і idempotent end.
8. Лише після staging acceptance увімкнути public testing; production migration/deployment/secret rotation виконуються вручну з окремим погодженням.

## 13. Checklist готовності до публічного тестування

- [x] TLS bypass видалено з перевірених Unity/API auth шляхів.
- [x] Player JWT і Dedicated Server credentials розділені.
- [x] Server registry зберігається в PostgreSQL і має heartbeat expiration.
- [x] Match start/end server-only та прив'язані до server identity.
- [x] Connection ticket короткоживучий, hashed, одноразовий і replay-resistant.
- [x] FishNet Authenticator і transport encryption підключені до prefab/code path.
- [x] Critical RPC мають server-side authentication gate і sender-derived identity.
- [x] API build і локальні security smoke перевірки пройшли.
- [ ] Production secrets rotated and provisioned.
- [ ] DTLS certificates/keys installed and hostname-verified on staging servers.
- [ ] EF migration applied and verified on staging/production by approved operator.
- [ ] Full Unity runtime two-client regression, reconnect і gameplay test виконано.
- [ ] Public testing sign-off після перевірки deployment checklist.

Висновок: кодова основа для безпечного локального/staging тестування реалізована, але проєкт **ще не слід оголошувати готовим до публічного production-тестування**, доки не виконані ручні secret/infrastructure steps та повна runtime multiplayer regression перевірка.
