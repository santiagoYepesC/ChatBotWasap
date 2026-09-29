# Research: POC de Bot de WhatsApp Business

**Date**: 2026-09-29  
**Scope**: Resolve design choices for Meta onboarding, API processing, persistence, AI/media,
security, and runtime support. This document records planning decisions; it does not implement
the solution or change `spec.md`.

## Decisions

### 1. Meta onboarding: Embedded Signup v4 and server-side token exchange

**Decision**: Use the current Embedded Signup v4 flow with the Cloud API product only. The
browser launches Meta's Facebook Login for Business SDK/configuration and returns the
one-time authorization code plus WABA ID and Phone Number ID to `WhatsAppBot.Api`. Api
exchanges the code server-to-server, registers the selected number, and subscribes the app to
the WABA. Persist only IDs, states, and a secret-store reference.

**Rationale**: Meta says Embedded Signup gathers business information, creates/links assets,
and grants the app access. The successful flow returns WABA ID, Phone Number ID, and an
exchangeable code; the server exchanges the code for a business token, registers the number,
and subscribes webhooks. Meta currently warns that Embedded Signup v2 is deprecated on
2026-10-15, so v4 is the appropriate new implementation for this POC.

**Alternatives considered**: Manual phone-number entry is not authorization or verification
and is prohibited by the spec/constitution. Direct Meta calls from Admin would expose sensitive
operations and is prohibited. v2 is not appropriate for a new implementation given the
announced deprecation.

**Operational constraints**: Cloud API onboarding requires `whatsapp_business_management`
and `whatsapp_business_messaging`; customer onboarding may require advanced access/App Review.
Existing WhatsApp Business App numbers need the relevant coexistence flow. Whether this
organization is a Tech Provider, Solution Partner, or direct developer changes token,
review, onboarding, and credit-line requirements and must be confirmed before a real customer
demo. Do not assume partner credit-line features.

### 2. Webhook processing: verify, durably store, acknowledge, then process

**Decision**: Separate webhook transport acceptance from message processing. Validate the GET
challenge with the configured Verify Token. Validate POST `X-Hub-Signature-256` against the
raw request bytes with the App Secret. Persist the event idempotently, then acknowledge; a
worker processes stored events and writes outbound work to a SQL-backed outbox.

**Rationale**: Meta webhooks include both inbound messages and outbound status updates.
Media retrieval and AI calls are variable-latency work and should not run in the webhook
request. Durable inbox/outbox rows prevent acknowledging an event that was not saved and
reduce duplicate replies on event redelivery.

**Alternatives considered**: Synchronous webhook processing risks request timeouts and
duplicate processing. Adding a message broker would add operations and cost to a single-number
POC; SQL Server already provides the durable store and is sufficient at the planned scale.

### 3. Frequent response matching: normalized literal expression containment

**Decision**: For active rules only, normalize message text and each configured expression by
Unicode Form KC, trimming and collapsing whitespace, and invariant case-folding. A rule
matches when normalized message text contains any normalized non-empty configured expression.
Select the greatest priority; break ties by ascending stable response ID. Do not use fuzzy
matching, embeddings, custom NLP, or infer semantic intent from the description.

**Rationale**: The existing spec states expressions are matched by case-insensitive containment,
and priority decides multiple matches. This deterministic rule is simple to explain to
administrators and straightforward to test.

**Alternatives considered**: Regex requires syntax/safety rules; fuzzy or semantic matching
would add nondeterminism, model calls, and tuning beyond the POC request. The description field
is administrative documentation, not a hidden classifier prompt.

### 4. Bot response modes and message pipeline

**Decision**: Persist a per-integration mode with default `FaqThenAi`; use enumerated modes
`FaqOnly`, `FaqThenAi`, and `AiOnly`. In `FaqOnly`, a miss is stored without an automatic
reply. In `FaqThenAi`, a miss calls AI. In `AiOnly`, skip FAQ lookup. A hit in either FAQ mode
uses the configured answer and does not call AI. Apply the same mode rules to text and audio
transcripts: `FaqOnly` searches active FAQs and records a no-reply on a miss; `FaqThenAi`
searches first and calls AI only on a miss; `AiOnly` sends the text/transcript to AI without
using FAQs to generate the reply.

**Rationale**: This implements the three modes and default from the existing feature spec
without adding a fourth behavior.

**Alternatives considered**: Always call AI after a FAQ hit conflicts with the specified
configured-answer-first behavior and adds unnecessary cost/latency.

### 5. AI/media provider: OpenAI API for POC; provider interfaces remain replaceable

**Decision**: Use OpenAI for text/vision response generation and file transcription via
RestSharp behind separate `IAiReplyGenerator`, `IImageAnalysisService`, and
`IAudioTranscriptionService` ports. Configure model identifiers, timeout, and budget rather
than hard-coding a model. Convert incoming OGG/Opus voice notes to a supported audio format
using a pinned FFmpeg build when necessary; enforce size and duration limits and remove
temporary files after processing.

**Rationale**: OpenAI documents both image-input analysis and a dedicated recorded-file
transcription flow. The transcription guide documents a 25 MB file limit and supports mp3,
mp4, mpeg, mpga, m4a, wav, and webm; OGG is not in that list. A narrow codec-normalization
adapter avoids assuming that Meta media is directly accepted. Keeping interfaces separate
allows a different approved provider without changing Business use cases.

**Alternatives considered**: A custom NLP or multimodal model is unnecessary. Selecting
separate vendors for transcription and vision adds operational configuration. Provider and
data-processing terms must be approved before sending customer content.

**POC data handling**: Transmit only the message/media needed for the requested operation.
Keep raw media private and propose a configurable 30-day retention period for conversation
content and attachments, subject to project-owner approval before real customer data is used.

### 6. SQL Server access: stored procedures only

**Decision**: `Microsoft.Data.SqlClient` calls stored procedures with typed parameters from
Persistence. Business receives repository ports and never constructs SQL; Controllers,
Webhooks, and Admin never query SQL. FAQ expressions are one-to-many rows and are replaced
atomically through a table-valued parameter or equivalent transactional stored procedure.

**Rationale**: This directly follows the user's explicit data-access rule and constitution's
layer separation.

**Alternatives considered**: Inline SQL, ORM-generated SQL, or Admin-to-database access are
outside the requested architecture. No ORM is needed for this stored-procedure-first POC.

### 7. Admin web experience: Razor Pages as thin API client

**Decision**: Use ASP.NET Core Razor Pages in `WhatsAppBot.Admin`. It calls only the Api over
HTTPS through a typed HTTP client; login/session and page rendering are presentation
responsibilities. Api owns password verification, authorization decisions, and use cases.

**Rationale**: A server-rendered .NET application is sufficient for the requested admin
screens and avoids introducing a separate frontend framework. Keeping the API client
server-side prevents Meta and AI secrets, and API bearer tokens, from being exposed to
browser JavaScript.

**Alternatives considered**: A separate SPA requires additional toolchain and browser-token
handling not needed for a POC.

### 8. Runtime support: use requested .NET 10 LTS

**Decision**: Target .NET 10 LTS for the new solution projects and use the latest supported
10.0.x servicing patch at build and deployment time. Recheck Microsoft's lifecycle and servicing
status before release.

**Rationale**: The POC is starting now; .NET 10 LTS avoids a near-term runtime migration and
provides a longer support horizon. Microsoft's lifecycle table lists .NET 10 LTS through
2028-11-14 and .NET 9 support through 2026-11-10.

**Alternatives considered**: .NET 9 is near end of support and would create immediate migration
pressure; it is not selected. Use a newer runtime only after a deliberate platform review.

### 9. Meta message policy and template distinction

**Decision**: Treat a configured FAQ reply as ordinary bot content. It is not a Meta-approved
message template. Before sending, the Meta integration must enforce current customer-care
window and template rules; outside an eligible free-form messaging window, a configured FAQ or
AI answer cannot be sent as a substitute for a required approved template.

**Rationale**: Prevents a product-level response mode from bypassing Meta messaging policy.
For this POC, the live demonstration is limited to conversations initiated by the customer and
responses sent within the currently permitted customer-service window. If the window has
expired or Meta requires a template, do not send free-form FAQ/AI content; record a safe,
visible no-reply outcome such as `MessagingWindowClosed` or `TemplateRequired`. Do not add
template authoring/sending to this POC.

## Official references

- Meta, [Embedded Signup overview](https://developers.facebook.com/documentation/business-messaging/whatsapp/embedded-signup/overview) — flow, returned assets/code, token exchange, registration, webhook subscription, and v2 deprecation notice.
- Meta, [Embedded Signup v4](https://developers.facebook.com/documentation/business-messaging/whatsapp/embedded-signup/version-4) — v4 configuration, Cloud API product, and permissions.
- Meta, [Webhooks overview](https://developers.facebook.com/documentation/business-messaging/whatsapp/webhooks/overview) — inbound messages, status events, and permission needs.
- Meta, [Webhook endpoint setup](https://developers.facebook.com/documentation/business-messaging/whatsapp/webhooks/create-webhook-endpoint) — endpoint verification and webhook setup.
- Meta, [Cloud API send messages](https://developers.facebook.com/documentation/business-messaging/whatsapp/cloud-api/guides/send-messages) — outbound message rules/behavior.
- Meta, [WhatsApp Business Messaging Policy](https://business.whatsapp.com/policy) — opt-in and messaging policy.
- OpenAI, [Audio and voice](https://developers.openai.com/api/docs/guides/audio) and [file transcription](https://developers.openai.com/api/docs/guides/speech-to-text) — recorded transcription, documented file formats and size limit.
- OpenAI, [Images and vision](https://developers.openai.com/api/docs/guides/images-vision) — image-input analysis.
- Microsoft, [.NET release lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) — .NET 9 and .NET 10 support dates; page observed 2026-09-29.

Documentation and account prerequisites change; re-check Meta permissions, flow version,
messaging policy, model limits, and lifecycle dates immediately before implementation and demo.
