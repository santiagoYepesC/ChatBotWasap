<!--
Sync Impact Report
Version change: 1.1.1 → 1.2.0
Modified principles: none
Added sections: Technology Baseline (.NET 10 LTS and ASP.NET Core 10 required for new projects)
Removed sections: none
Deferred items: none
-->

# WhatsAppBot Constitution

## Core Principles

### I. Mission-Driven Delivery
Every feature and automation must serve a clearly stated user or operational need.
The project must document why a change exists, which workflow it supports, and what
success looks like before implementation begins. This keeps scope bounded and prevents
low-value work from diluting the product's purpose.

### II. Secure by Default
All integrations, automation, and user data handling must minimize exposure,
validate input, and protect secrets from accidental disclosure.
Authentication, environment variables, API tokens, and outbound messages must use
least-privilege access and explicit approval boundaries. Security is not a later
review item; it is a design requirement.

### III. Test-First Quality Gates
Changes must be validated with automated tests or equivalent verification before merge.
When a bug is fixed or behavior is added, the relevant checks must fail before the fix
and pass after it. This standard prevents regressions and preserves trust in the project.

### IV. Observable Operations
The system must emit actionable logs, metrics, or status signals so operators can
understand what ran, why it ran, and whether it succeeded.
If behavior cannot be observed, it is not ready for production. This principle keeps
failures diagnosable and reduces downtime caused by silent automation failures.

### V. Simplicity and Change Control
The project must prefer the smallest correct solution and avoid unnecessary abstraction,
configuration sprawl, or duplicate logic.
Any breaking or high-risk change requires explicit documentation, migration guidance,
and review before adoption. Simple systems are easier to reason about, troubleshoot,
and evolve safely.

## Security & Data Handling

The project must treat message content, customer data, API credentials, and system state as
sensitive assets. Secrets must never be committed, logged in plain text, or embedded in
shared config or examples. Access to external systems must be scoped to the minimum required
privileges and reviewed when permissions change.

All integrations must validate payloads and reject malformed or unexpected inputs before
processing them. Failures must degrade safely and preserve operator visibility. Data retention,
backup, and audit requirements must be explicit whenever the project handles user-facing or
business-critical information.

## Technology Baseline

New WhatsAppBot projects MUST target .NET 10 LTS and ASP.NET Core 10, using the latest
supported servicing release. This baseline applies to the solution's application projects and
does not change the architectural layers, integration boundaries, or security requirements in
this constitution.

## Official WhatsApp Business Platform Integration

The system MUST integrate with WhatsApp using only Meta's official WhatsApp Business Platform
and Cloud API. Unofficial integrations, WhatsApp Web automation, scraping, and libraries that
simulate the WhatsApp client are prohibited.

The Admin Panel MUST provide a WhatsApp Business configuration module for connecting a
business number to the bot. Meta Embedded Signup MUST be studied and used as the preferred
onboarding and number-linking mechanism. Connection MUST follow Meta's official authorization,
business account selection or creation, business phone number selection or registration,
ownership verification, Cloud API registration, and webhook configuration processes. Entering
a phone number into a form alone MUST NOT be treated as connecting or activating it.

Meta integration logic MUST reside in the Integrations layer behind interfaces. The Business
layer MUST coordinate the use cases for connecting and disconnecting or reconfiguring the
integration, querying connection status, activating or deactivating the bot, processing inbound
messages, and sending replies. The Admin Panel MUST NOT call Graph API directly for sensitive
backend logic.

The system MUST store only the integration data needed to operate and identify the connection:
WABA ID, Phone Number ID, business phone number, applicable display name, connection state, bot
state, required webhook identifiers, and secure references to credentials or tokens. Meta
tokens, secrets, App Secret, and credentials MUST never be hardcoded or exposed to the frontend.

The initial proof of concept MAY support one active WhatsApp Business number, but its
architecture MUST allow multiple numbers or businesses in a future version. The demonstration
MUST visibly show that the business number is connected to the bot and provide a verifiable
path showing that messages sent to that number reach and are processed by the system.

## Delivery Workflow

All work must follow a transparent, reviewable lifecycle: define the problem, document the
expected behavior, implement with the least scope necessary, and verify it with focused checks.
Changes that alter external contracts, permissions, or operational behavior require explicit
review before deployment.

The project must maintain a single source of truth for requirements, implementation notes, and
release constraints. Any workflow that is difficult to onboard or difficult to verify is not
considered ready for broad use. Teams must prefer repeatable automation over manual memory.

## Governance

This constitution supersedes informal practices when they conflict with its requirements.
Amendments require documented rationale, review by the responsible project owner or maintainer,
and explicit version updates before the change becomes binding.

The project uses semantic versioning for governance changes:
- MAJOR: backward-incompatible governance or principle changes
- MINOR: new principle or materially expanded guidance
- PATCH: clarifications, wording fixes, and non-semantic improvements

Compliance reviews must confirm that changes align with the governing principles in this
constitution. Any exception must be recorded, justified, and accepted by the project owner
before implementation proceeds. The project must retain evidence of review for significant or
sensitive modifications.

**Version**: 1.2.0 | **Ratified**: 2026-09-29 | **Last Amended**: 2026-09-29
