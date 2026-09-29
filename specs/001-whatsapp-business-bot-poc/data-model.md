# Data Model: POC de Bot de WhatsApp Business

The model is SQL Server-oriented but describes logical data, not a migration or implementation.
All mutable business data is accessed through stored procedures in Persistence. Use UTC
timestamps. Every WhatsApp-owned entity carries `IntegrationId` directly or through its parent
so later multiple-number support does not require replacing global singleton relationships.

## Entities

### Administrator

| Field | Type / rule | Purpose |
|---|---|---|
| AdminId | `bigint`, PK | Administrator identity |
| Email | `nvarchar(320)`, unique, normalized | Login name |
| PasswordHash | `nvarchar(512)` | Adaptive password hash; never plaintext |
| IsActive | `bit` | Login enabled state |
| CreatedAtUtc | `datetime2` | Audit |
| LastLoginAtUtc | `datetime2`, nullable | Audit |

Authentication query returns the hash to Business for password verification. A missing,
inactive, or invalid account returns the same public authentication failure. Do not persist
Meta/API secrets here.

### WhatsAppIntegration

| Field | Type / rule | Purpose |
|---|---|---|
| IntegrationId | `uniqueidentifier`, PK | Connection boundary for all WhatsApp data |
| WabaId | `nvarchar(64)`, nullable until connected | Meta WhatsApp Business Account ID |
| PhoneNumberId | `nvarchar(64)`, nullable and unique while active | Meta Cloud API phone-number ID |
| BusinessPhoneNumber | `nvarchar(32)`, nullable until connected | Displayable E.164-format number |
| DisplayName | `nvarchar(256)`, nullable | Meta-provided display name |
| ConnectionState | enum/string | NotConnected, Configuring, Connected, Error |
| ConnectedAtUtc | `datetime2`, nullable | Successful connection timestamp |
| LastConnectionErrorCode | `nvarchar(100)`, nullable | Safe diagnostic identifier only |
| AccessTokenSecretRef | `nvarchar(512)`, nullable until connected | Opaque secret-store reference |
| MetaAppSecretRef | `nvarchar(512)` | Secret reference; app-wide value may be held in deployment configuration instead |
| WebhookVerifyTokenRef | `nvarchar(512)` | Secret reference; never sent to Admin |
| CreatedAtUtc / UpdatedAtUtc | `datetime2` | Audit |
| IsActive | `bit` | POC enforces at most one active connection |

The one-active integration constraint is an application/database invariant for this POC. Do
not model integration as a global singleton: future rows remain separated by `IntegrationId`.
Provision one unconnected `NotConnected` integration and its `FaqThenAi`/bot-disabled
configuration as part of first-time setup so Admin can configure FAQs and modes before Meta
signup; this is a POC connection slot, not a connected phone number.
Store the Meta App ID, Embedded Signup configuration ID, Graph API version and public webhook
URL as non-secret deployment configuration unless the application later needs per-integration
values.

### BotConfiguration

| Field | Type / rule | Purpose |
|---|---|---|
| IntegrationId | `uniqueidentifier`, PK/FK | Per-number configuration |
| IsBotEnabled | `bit` | Automatic processing switch; defaults false until connected/explicitly activated |
| ReplyMode | enum | `FaqOnly`, `FaqThenAi`, `AiOnly`; default `FaqThenAi` |
| CreatedAtUtc / UpdatedAtUtc | `datetime2` | Audit |

AI provider keys and model details are deployment configuration/secret references, not fields
returned in public DTOs.

### FrequentResponse

| Field | Type / rule | Purpose |
|---|---|---|
| FrequentResponseId | `bigint`, PK | Stable priority tie-break identity |
| IntegrationId | `uniqueidentifier`, FK | Configuration scope |
| QuestionOrIntent | `nvarchar(500)` | Admin-entered question, intent, or description |
| AnswerText | `nvarchar(max)` | Configured response content |
| Priority | `int` | Higher number wins |
| Category | `nvarchar(100)`, nullable | Optional user-defined category; no category table needed in POC |
| IsActive | `bit` | Search eligibility |
| CreatedAtUtc / ModifiedAtUtc | `datetime2` | Audit |

Require non-empty `QuestionOrIntent`, answer, at least one non-empty expression, and a defined
priority range (recommend 0-1000). Update `ModifiedAtUtc` on every successful edit or state
change; creation time is immutable.

### FrequentResponseExpression

| Field | Type / rule | Purpose |
|---|---|---|
| ExpressionId | `bigint`, PK | Expression identity |
| FrequentResponseId | `bigint`, FK | Parent response |
| ExpressionText | `nvarchar(500)` | Phrase/word to match |
| NormalizedExpression | computed by Business or stored normalized value | Case/spacing-normalized match value |

One frequent response has one or more expressions. Enforce unique normalized expressions per
response. Replace the set atomically during edit using a SQL table-valued parameter or a
transactional stored procedure.

### Contact

| Field | Type / rule | Purpose |
|---|---|---|
| ContactId | `bigint`, PK | Contact identity |
| IntegrationId | `uniqueidentifier`, FK | WhatsApp number scope |
| WhatsAppUserId | `nvarchar(64)` | WhatsApp `wa_id`; unique with IntegrationId |
| DisplayName | `nvarchar(256)`, nullable | Profile name when supplied |
| CreatedAtUtc / UpdatedAtUtc | `datetime2` | Audit |

### Conversation

| Field | Type / rule | Purpose |
|---|---|---|
| ConversationId | `bigint`, PK | Conversation identity |
| IntegrationId | `uniqueidentifier`, FK | WhatsApp number scope |
| ContactId | `bigint`, FK | Customer contact |
| BasicStatus | enum/string | Open, Closed (minimal POC state) |
| CreatedAtUtc | `datetime2` | First interaction |
| LastMessageAtUtc | `datetime2` | Admin list sort |

Unique active conversation per `(IntegrationId, ContactId)` unless product behavior later
introduces explicit conversation sessions.

### Message

| Field | Type / rule | Purpose |
|---|---|---|
| MessageId | `bigint`, PK | Internal identity |
| IntegrationId | `uniqueidentifier`, FK | Scope and idempotency key boundary |
| ConversationId | `bigint`, FK | Conversation |
| ProviderMessageId | `nvarchar(256)`, nullable, unique with IntegrationId | Meta inbound/outbound `wamid` |
| Direction | enum | Inbound or Outbound |
| MessageType | enum | Text, Audio, Image, Unsupported |
| ContentText | `nvarchar(max)`, nullable | Text/caption or sent response |
| ReplySource | enum, nullable | FrequentResponse, AI, Admin |
| FrequentResponseId | `bigint`, nullable FK | Rule used for response |
| ProcessingState | enum | Received, Processing, Completed, Failed, NoReply |
| DeliveryState | enum, nullable | Pending, Sent, Delivered, Read, Failed |
| ProviderTimestampUtc | `datetime2`, nullable | Meta event timestamp |
| CreatedAtUtc / UpdatedAtUtc | `datetime2` | Local audit |
| FailureCode | `nvarchar(100)`, nullable | Safe diagnostic only; includes policy outcomes such as `MessagingWindowClosed` or `TemplateRequired` when no outbound message is sent |

Inbound Meta message ID is unique per integration to prevent replay duplicates. Keep
`ProcessingState` separate from `DeliveryState`: generated does not imply sent, and sent does
not imply delivered/read. A policy-blocked reply remains a no-reply processing result with a
safe `FailureCode`; do not create a successful outbound delivery record for content that Meta
requires to be sent as an approved template. The Admin `MessageDTO.outcomeCode` is a safe
allowlisted projection of this field, not the raw provider error.

### MediaAttachment

| Field | Type / rule | Purpose |
|---|---|---|
| MediaAttachmentId | `bigint`, PK | Attachment |
| MessageId | `bigint`, FK, unique when one attachment per message | Message association |
| MetaMediaId | `nvarchar(128)`, nullable | Meta media identifier |
| MediaKind | enum | Audio or Image |
| MimeType | `nvarchar(128)`, nullable | Validated content type |
| SizeBytes | `bigint`, nullable | Validated limit |
| StorageReference | `nvarchar(512)`, nullable | Private object-store reference; never a public URL |
| AvailabilityState | enum | Pending, Available, Expired, Failed, Deleted |
| CreatedAtUtc / ExpiresAtUtc | `datetime2` | Retention |

Store original files outside SQL Server. Only authorized Admin requests may stream media; use
short-lived access or an API proxy and enforce authorization by conversation. Configured
retention cleanup removes the object and marks or deletes metadata consistently.

### Transcript

| Field | Type / rule | Purpose |
|---|---|---|
| MessageId | `bigint`, PK/FK | Audio message being transcribed |
| TranscriptText | `nvarchar(max)`, nullable | Transcript when successful |
| State | enum | Pending, Completed, Failed |
| CreatedAtUtc / UpdatedAtUtc | `datetime2` | Audit |

### WebhookInboxEvent

| Field | Type / rule | Purpose |
|---|---|---|
| WebhookInboxEventId | `bigint`, PK | Durable delivery record |
| IntegrationId | `uniqueidentifier`, nullable FK | Resolved integration scope |
| EventKey | `nvarchar(256)`, unique | Provider message/status identifier or stable event hash |
| EventType | `nvarchar(80)` | Message, status, account notification |
| NormalizedEventJson | `nvarchar(max)` | Allowlisted event fields required for processing; no raw webhook body or secrets |
| State | enum | Received, Processing, Completed, Failed |
| AttemptCount | `int` | Retry control |
| ReceivedAtUtc / ProcessedAtUtc | `datetime2`, nullable | Audit |
| LastFailureCode | `nvarchar(100)`, nullable | Safe diagnostic |

Validate Meta signature against raw bytes before parsing/storing. Do not retain the complete
raw webhook body; store only the normalized fields needed by the worker, with size limits and
short retention before the event is marked complete. If Meta does not provide an event ID for an event, derive an
idempotency key from stable provider IDs and event type, not a random request ID.

### MessageOutbox

| Field | Type / rule | Purpose |
|---|---|---|
| MessageId | `bigint`, PK/FK | Pending outbound message |
| IntegrationId | `uniqueidentifier`, FK | Sending number |
| State | enum | Pending, Sending, Sent, Failed |
| AttemptCount | `int` | Bounded retries |
| NextAttemptAtUtc | `datetime2` | Backoff scheduling |
| CreatedAtUtc / UpdatedAtUtc | `datetime2` | Audit |

Use a claim/lease procedure to prevent concurrent workers from sending the same message.
Retry only failures known to be transient and preserve the Meta message ID once accepted.

## Relationships and Indexes

- `WhatsAppIntegration 1—1 BotConfiguration`
- `WhatsAppIntegration 1—* FrequentResponse 1—* FrequentResponseExpression`
- `WhatsAppIntegration 1—* Contact 1—* Conversation 1—* Message`
- `Message 1—0..1 MediaAttachment`, `Message 1—0..1 Transcript`,
  `Message 1—0..1 MessageOutbox`
- `WhatsAppIntegration 1—* WebhookInboxEvent`
- Unique: `(IntegrationId, WhatsAppUserId)`, `(IntegrationId, ProviderMessageId)`,
  `(IntegrationId, PhoneNumberId)` when active, `EventKey`, and
  `(FrequentResponseId, NormalizedExpression)`.
- Query indexes: `(IntegrationId, IsActive, Priority DESC, FrequentResponseId)` for FAQ
  candidates; `(IntegrationId, LastMessageAtUtc DESC)` for conversation listing;
  `(ConversationId, CreatedAtUtc, MessageId)` for message history; `(State, NextAttemptAtUtc)`
  for inbox/outbox workers.

## Stored Procedure Inventory

Persistence must call only stored procedures, with typed parameters and explicit transactions
where an operation spans parent/child rows.

| Stored procedure | Responsibility |
|---|---|
| `sp_Admin_GetByEmailForAuthentication` | Retrieve normalized email, password hash and active state for Business password verification |
| `sp_Admin_UpdateLastLogin` | Update successful login audit |
| `sp_Admin_CreateFirstAdministrator` | One-time insert for initial administrator bootstrap; accept only normalized email and adaptive password hash |
| `sp_WhatsAppIntegration_GetCurrent` | Load current integration and secret references |
| `sp_WhatsAppIntegration_SaveSignupResult` | Persist WABA/phone IDs and secret reference after server-side exchange |
| `sp_WhatsAppIntegration_SetConnectionState` | Set Configuring/Connected/Error and safe error code |
| `sp_WhatsAppIntegration_Disconnect` | Disable connection/bot and clear active state without exposing provider secrets |
| `sp_BotConfiguration_Get` | Read enable switch and response mode |
| `sp_BotConfiguration_Upsert` | Save enable switch/mode |
| `sp_FrequentResponse_List` | Paginated FAQ list |
| `sp_FrequentResponse_GetById` | Load response plus expressions |
| `sp_FrequentResponse_Create` | Atomically insert response and expression TVP |
| `sp_FrequentResponse_Update` | Atomically edit response and replace expressions |
| `sp_FrequentResponse_SetActive` | Activate/deactivate with modified timestamp |
| `sp_FrequentResponse_Delete` | Delete response and expressions atomically |
| `sp_FrequentResponse_ListActiveCandidates` | Return active responses and expressions ordered by priority for Business matching |
| `sp_Contact_UpsertWhatsApp` | Insert/update contact by integration and WhatsApp user ID |
| `sp_Conversation_GetOrCreate` | Get or create conversation for integration/contact |
| `sp_Conversation_List` | Paginate conversation list sorted by last message |
| `sp_Conversation_GetDetail` | Return authorized conversation metadata and paginated messages |
| `sp_Message_InsertInbound` | Idempotently record inbound provider message |
| `sp_Message_CreateOutboundPending` | Persist outbound response and outbox item atomically |
| `sp_Message_UpdateOutboundProviderResult` | Store provider message ID / send failure and delivery state |
| `sp_Message_ApplyDeliveryStatus` | Reconcile sent/delivered/read/failed webhook status |
| `sp_MediaAttachment_Upsert` | Store media metadata and private storage reference |
| `sp_Transcript_Upsert` | Save transcript state and transcript text |
| `sp_WebhookInbox_Insert` | Persist webhook event idempotently before 200 acknowledgement |
| `sp_WebhookInbox_ClaimBatch` | Lease pending events for a worker |
| `sp_WebhookInbox_SetState` | Complete/fail/retry a processed event |
| `sp_MessageOutbox_ClaimBatch` | Lease outbound items for a worker |
| `sp_MessageOutbox_SetState` | Complete/fail/retry outbound item with bounded attempts |

If refresh tokens are selected instead of short-lived API tokens, add dedicated hashed refresh
token procedures; the POC plan otherwise uses short-lived API access tokens and a protected
Admin server-side session.
