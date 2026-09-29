# Quickstart de validación: POC de Bot de WhatsApp Business

This guide covers the implemented solution/bootstrap milestone and the local bot-configuration
and frequent-response milestone. Meta messaging, AI, multimedia, and conversation features
remain subsequent implementation work.

## Prerequisites

- .NET 10 LTS SDK (latest supported 10.0.x servicing patch).
- ASP.NET Core HTTPS development certificate trusted on the local machine
  (`dotnet dev-certs https --trust`) so Admin can call Api over HTTPS.
- SQL Server LocalDB and `sqlcmd` for local Development. Production uses its configured SQL
  Server environment; LocalDB is not a production requirement.
- Meta Developer app with WhatsApp Cloud API, Embedded Signup v4 configuration, HTTPS webhook,
  required permissions/advanced access, approved test number and permitted test recipients.
- OpenAI API account/key and configured text/vision and transcription model identifiers.
- FFmpeg available to the API worker for OGG/Opus normalization; private media storage.
- User Secrets locally; production secret-store adapter configured before any live customer
  data is processed.
- Owner approval for the proposed 30-day data retention, Meta opt-in/message policy, and AI
  provider data processing.

## Local setup and first administrator

1. Open PowerShell and verify the LocalDB utility and instance:

   ```powershell
   Get-Command sqllocaldb
   sqllocaldb info MSSQLLocalDB
   ```

   If `MSSQLLocalDB` is not listed, create the standard development instance; then ensure it
   is running:

   ```powershell
   sqllocaldb create MSSQLLocalDB -s
   sqllocaldb start MSSQLLocalDB
   sqllocaldb info MSSQLLocalDB
   ```

   Confirm `sqlcmd` is available with `Get-Command sqlcmd`. Use the Windows identity running
   the commands; no SQL username/password is needed.
2. The Development connection is configured under `ConnectionStrings:WhatsAppBot` in
   `src/WhatsAppBot.Api/appsettings.Development.json`. JSON requires the instance backslash
   to be escaped:

   ```json
   {
     "ConnectionStrings": {
       "WhatsAppBot": "Server=(localdb)\\MSSQLLocalDB;Database=WhatsAppBot;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

   The conceptual (non-JSON-escaped) value is
   `Server=(localdb)\MSSQLLocalDB;Database=WhatsAppBot;Trusted_Connection=True;TrustServerCertificate=True;`.
   This is Development-only. Do not put a connection string in code. Admin has no database
   setting and connects exclusively to Api.
3. Create/prepare the database from a clean LocalDB instance and apply the versioned database,
   tables, relationships, indexes, Stored Procedures, and required seed scripts in dependency
   order:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\sql\Initialize-DevelopmentDatabase.ps1 -ServerInstance '(localdb)\MSSQLLocalDB' -Database WhatsAppBot
   ```

   The initializer first creates `WhatsAppBot` using Windows authentication, then applies the
   ordered SQL scripts under `src/WhatsAppBot.Api/Persistence/StoredProcedures/`. It must be
   safe to rerun against the development database and must fail explicitly if any script fails.
4. Validate the instance, database, and initialized schema:

   ```powershell
   sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d WhatsAppBot -Q "SELECT DB_NAME() AS DatabaseName; SELECT COUNT(*) AS UserTableCount FROM sys.tables WHERE is_ms_shipped = 0; SELECT COUNT(*) AS StoredProcedureCount FROM sys.procedures WHERE is_ms_shipped = 0;"
   ```

   Confirm `DatabaseName` is `WhatsAppBot`, the user-table count is greater than zero, and the
   stored-procedure count includes the feature's required procedures. If validation fails,
   inspect the initializer's reported script and error, then rerun after correction.
5. Configure a local API signing key and the initial administrator email in the API project's
   User Secrets. Generate the signing key locally; do not copy it into the repository:

   ```powershell
   $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
   $bytes = New-Object byte[] 48
   $rng.GetBytes($bytes)
   $signingKey = [Convert]::ToBase64String($bytes)
   $rng.Dispose()
   dotnet user-secrets set "Authentication:SigningKey" $signingKey --project .\src\WhatsAppBot.Api
   Remove-Variable signingKey, bytes, rng
   dotnet user-secrets set "Bootstrap:AdminEmail" "admin@example.test" --project .\src\WhatsAppBot.Api
   ```

   Provision exactly one administrator with the one-time bootstrap command. It prompts for a
   unique initial password of at least 14 characters without echoing it. Never pass a password as a command-line
   argument or place it in source, screenshots, shell history, or logs.

   ```powershell
   dotnet run --project .\src\WhatsAppBot.Api -- admin bootstrap
   ```

   The command must hash the password with an adaptive password hasher, create the account only
   when none exists, report that an administrator already exists on later invocations, never
   print the password, and refuse to overwrite an existing administrator.
   For a non-interactive deployment, inject `BOOTSTRAP_ADMIN_PASSWORD` only through the
   approved protected environment/secret store for that one-time process, then remove/revoke it.
   Remove the local bootstrap email after success:

   ```powershell
   dotnet user-secrets remove "Bootstrap:AdminEmail" --project .\src\WhatsAppBot.Api
   ```

   Do not seed a default password or commit credentials.
6. Configure `AdminApi:BaseUrl` in `src/WhatsAppBot.Admin/appsettings.json` only if the API is
   hosted at a different URL. The local defaults are HTTPS `https://localhost:7001` for Api
   and `https://localhost:7002` for Admin; Admin stores its API access token in server-side
   session storage and never sends database credentials to the browser.

7. Later, when the WhatsApp/AI milestone is implemented, configure Meta and OpenAI secrets in
   API User Secrets:

   ```powershell
   dotnet user-secrets set "Meta:AppId" "<app-id>" --project .\src\WhatsAppBot.Api
   dotnet user-secrets set "Meta:AppSecret" "<secret>" --project .\src\WhatsAppBot.Api
   dotnet user-secrets set "Meta:VerifyToken" "<random-secret>" --project .\src\WhatsAppBot.Api
   dotnet user-secrets set "Meta:EmbeddedSignupConfigId" "<config-id>" --project .\src\WhatsAppBot.Api
   dotnet user-secrets set "Meta:GraphApiVersion" "<approved-version>" --project .\src\WhatsAppBot.Api
   dotnet user-secrets set "OpenAI:ApiKey" "<provider-key>" --project .\src\WhatsAppBot.Api
   ```

   Replace placeholders only in the local secret store. Never paste values into source,
   screenshots, logs, or this guide.
8. Restore and build:

   ```powershell
   dotnet restore .\WhatsAppBot.sln
   dotnet build .\WhatsAppBot.sln --configuration Release
   ```

9. Run Api and Admin in separate terminals:

   ```powershell
   dotnet run --project .\src\WhatsAppBot.Api
   dotnet run --project .\src\WhatsAppBot.Admin
   ```

   Confirm Swagger is available at `https://localhost:7001/swagger` only in Development, and
   Admin loads over HTTPS. The Dashboard is a basic status summary; bot settings and frequent
   responses are managed from the Configuration navigation.

10. Use Configuración → Bot to activate/deactivate the bot and select `FaqOnly`, `FaqThenAi`,
    or `AiOnly`. The default remains `FaqThenAi`; no AI provider is called in this milestone.
11. Use Configuración → Respuestas Frecuentes to create, edit, activate/deactivate, and delete
    FAQ responses. Put one literal word or phrase per line. The matcher applies Unicode Form KC,
    trims and collapses whitespace, compares invariant case-insensitive literal containment,
    selects the highest priority, and breaks ties by ascending stable response ID. This local
    configuration slice requires neither Meta nor OpenAI credentials.
12. Run automated checks. Persistence tests are guarded by an explicit Development LocalDB
    connection string and reject non-LocalDB targets:

    ```powershell
    $env:WHATSAPPBOT_TEST_CONNECTION_STRING = 'Server=(localdb)\MSSQLLocalDB;Database=WhatsAppBot;Trusted_Connection=True;TrustServerCertificate=True;'
    dotnet test .\WhatsAppBot.sln --configuration Release
    Remove-Item Env:WHATSAPPBOT_TEST_CONNECTION_STRING
    ```

## Validation scenarios

### 1. Administrator login and API boundary

- Sign in through Admin with the provisioned administrator.
- Verify an invalid/inactive user gets a generic authentication error.
- Verify Admin makes API requests only; no SQL connection or SQL credentials exist in Admin.
- Confirm API errors use `ResponseE<T>` and include a trace/correlation ID without stack traces,
  secrets, or SQL details.
- Before configuring Meta, open the local bot settings and verify the default is
  `FaqThenAi`; save `FaqOnly` and `AiOnly` and confirm each setting persists after reload.
- Create a local FAQ response, verify edit/activate/deactivate/delete and matching with the
  configured mode. This local slice requires SQL Server but no Meta or OpenAI account.

### 2. Meta Embedded Signup and number state

- Start the Embedded Signup v4 flow from Admin using a Meta test business.
- Authorize, select/create WABA, register/verify the business phone number and complete the
  official Cloud API setup.
- Verify the one-time code is exchanged only by Api and WABA ID, Phone Number ID, number,
  display name and connection state appear in Admin.
- Confirm that neither the browser nor Admin DTOs contain App Secret, Verify Token, or a
  reusable access token.
- Exercise cancel, missing permission, failed registration, and webhook subscription failure;
  verify the UI never reports these as Connected.

### 3. Webhook integrity, idempotency, and status

- Configure Meta's webhook URL and verify token; a matching GET returns the challenge and a
  non-matching value is rejected.
- Send a valid signed test POST and verify the event is durably recorded before a 200 response.
- Alter the body/signature and verify rejection; replay the same inbound `wamid` and confirm
  only one message and one reply are created.
- Deliver sent/delivered/read/failed status events, including out-of-order events, and verify
  the conversation reflects only known provider statuses.

### 4. Frequent response modes and Admin management

- In Configuración → Respuestas Frecuentes, create, edit, activate, deactivate and delete a
  response with multiple expressions. Verify timestamps and category/priority.
- Use consented test text with different casing/spacing and confirm normalized expression
  containment. Create multiple applicable rules and confirm highest priority wins, then stable
  response-ID tie-break.
- In `FaqOnly`, confirm a hit uses the configured answer and a miss is recorded with `NoReply`
  without calling AI; in default `FaqThenAi`, confirm a hit uses the configured answer without
  calling AI and a miss calls AI; in `AiOnly`, confirm FAQ lookup is skipped and AI processes
  the message.
- Inspect the conversation timeline and verify configured answers are labeled
  `FrequentResponse`, separate from Meta template messages.

### 5. Text, audio, image, and outbound delivery

- From a customer-initiated test conversation still inside Meta's permitted customer-service
  window, send a text message and verify persisted inbound content, mode-correct FAQ/AI answer,
  provider message ID, and delivery status lifecycle.
- Expire/age a test conversation beyond the permitted free-form messaging window or simulate a
  policy response requiring an official template. Verify FAQ and AI free-form replies are both
  blocked, the inbound result has `NoReply` with `MessagingWindowClosed` or `TemplateRequired`,
  and the Admin never labels an unsent answer as sent. Do not configure a template to bypass
  this POC limit.
- Send a consented OGG/Opus voice note; verify safe download, bounded conversion, transcription,
  all three response modes against the transcript, fallback only in `FaqThenAi`, and transcript
  display.
- Send a consented photo; verify private storage, AI analysis, generated response, authorized
  image viewing and no public media URL.
- Cause provider timeout, unsupported/oversized media, transcription failure, image-analysis
  failure, and outbound send failure. Verify visible failure states and that a failed/generated
  response is not presented as sent.
- Confirm disabled bot stores inbound messages but sends no automatic response.

### 6. Retention and release readiness

- Verify raw audio/photo objects and conversation content expire according to the owner-approved
  retention policy (proposed POC default: 30 days), with no orphaned public links.
- Search application logs and browser responses for App Secret, Verify Token, Meta business
  token, OpenAI API key, full webhook bodies and raw media; none may appear.
- Run end-to-end validation only against Meta test assets and consented recipients until Meta
  review, opt-in policy, AI data terms, and current .NET 10 LTS servicing are verified.

### 7. Reproducible SC-006 timed check

- Seed or select one connected integration with a known bot state and at least one recent
  conversation. Start a stopwatch when the authenticated administrator opens the Admin home
  page. Stop it when the administrator correctly identifies the WhatsApp connection state,
  bot enabled/disabled state, and latest conversation activity time.
- Repeat the same check five times with the same seeded state and test account. Record each
  elapsed time and correctness result in the demo validation evidence. SC-006 passes only when
  all five attempts are correct and each takes less than 60 seconds.

## Demo dependency matrix

| Demo step | External real service required |
|---|---|
| Admin UI, FAQs, modes, matching, API/database contract | No Meta/AI account; local SQL Server required |
| Meta Embedded Signup and real number connection | Meta app/configuration, eligible WABA/number and required permissions |
| Inbound and outbound WhatsApp | Public HTTPS webhook, Meta Cloud API and test/customer WhatsApp account |
| Audio transcription and image analysis | Meta media retrieval, private storage, FFmpeg, OpenAI |
| Delivery/read status display | Meta status webhooks; exact status depends on provider events |
