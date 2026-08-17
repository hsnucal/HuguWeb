# ADR-007: Authentication Strategy

## Status

Accepted

Accepted by Product Owner and CTO on 2026-08-17 (Sprint 0.3A Architecture Freeze).

---

## Context

HuGuWeb is an authenticated ERP. Users are hotel employees and managers. Needs over time may include username/password, password reset, lockout, invitations, employee turnover (disable quickly), MFA, future mobile sign-in, and possible SSO for hotel groups.

Authentication (who the user is) is distinct from authorization (what they may do). Authorization is covered in ADR-008 and should remain in the HuGuWeb domain.

HuGuWeb must **not** implement custom cryptography or custom password hashing.

No identity vendor is selected. There is no customer evidence that an external IdP is required for the first independent-hotel pilots.

---

## Problem

Should HuGuWeb authenticate users with **ASP.NET Core Identity hosted inside the product**, or with an **external OIDC / OAuth2 identity provider**?

Should authentication be externalized while authorization stays in-domain?

---

## Decision

We will:

1. **Authenticate with ASP.NET Core Identity implemented inside HuGuWeb** for the initial product.
2. Use Identity’s built-in password hashing and security features only — **no custom cryptography** and **no custom password hashing**.
3. Keep authentication at the **Host / Identity boundary**. Domain modules consume a user identifier and claims/permissions, not Identity UI types.
4. Keep **authorization in the HuGuWeb domain** (ADR-008).
5. Keep authentication infrastructure **replaceable**. Design the login/session integration so **future external OIDC / SSO does not require rewriting business modules**.

This is **not** a vendor selection. Auth0, Entra ID, Keycloak, Duende, and similar products are explicitly **not** chosen.

Web SPA (ADR-003): prefer **cookie authentication** (HTTP-only, secure, SameSite) over storing access tokens in browser storage. Future mobile: add token-based (OAuth2/OIDC) auth when that client exists.

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| A — ASP.NET Core Identity inside HuGuWeb | Accepted | Covers local accounts, reset, lockout, MFA hooks, invitations, and disable-on-turnover without extra infrastructure. Fits independent mid-size hotels. |
| B — External OIDC/OAuth2 IdP from day one | Rejected for bootstrap | Correct for enterprise SSO and multi-app identity. Adds vendor cost or self-hosted IdP operations before any hotel is live. No evidence a pilot requires it. |
| Custom user table + homemade hashing | Forbidden | Security anti-pattern. |
| IdentityServer / Duende as first step | Rejected for bootstrap | Valuable when HuGuWeb *is* an authorization server for many clients. One SPA does not justify it yet. |

### Needs vs options

| Need | Identity in-app | External OIDC |
|------|-----------------|---------------|
| Username/password | Built-in | Via IdP |
| Password reset | Built-in | Via IdP |
| MFA later | Supported (TOTP/providers) | Usually stronger IdP feature sets |
| Account lockout | Built-in | Via IdP |
| Employee turnover | Disable user in HuGuWeb | Disable in IdP + sync |
| Invitations | Implement as product workflow on Identity users | Invite in IdP |
| Hotel/property access | **Authorization** in HuGuWeb, not Identity | Same; IdP groups can *feed* roles |
| Role/permission management | Store assignments in HuGuWeb (ADR-008) | IdP groups are coarse; fine-grained perms still in-app |
| Security updates | Follow ASP.NET patches | Follow vendor/IdP patches |
| Operational burden | Low (one app) | Higher (IdP availability, upgrades, tenant config) |
| Future mobile | Add token endpoints or OIDC later | Already token-oriented |
| SSO later | Add external login / federation | Native |

### Should authentication be externalized?

**Not at bootstrap.** Externalizing identity is justified when:

- a hotel group demands corporate SSO
- multiple HuGuWeb applications must share one login
- compliance requires a dedicated IdP

Until then, an external IdP is operational cost without product value.

### Should authorization stay in HuGuWeb?

**Yes.** Even with an external IdP later, the IdP should assert *identity* (and maybe coarse groups). Property-aware permissions, department duties, and workflow actions are hotel-domain rules. They belong in HuGuWeb.

---

## Consequences

### Positive

- Fast path to a secure local-account ERP.
- No IdP to run or pay for during discovery and first pilots.
- Standard hashing, lockout, and reset flows.
- A later OIDC add-on is a Host change, not a domain rewrite — if boundaries are kept clean.

### Negative

- Identity data lives in HuGuWeb’s database; migrating users to an IdP later takes an explicit project.
- MFA, SSO, and advanced threat protection will not match a dedicated IdP until we add one.
- Cookie auth for SPA requires correct CORS/site deployment (same site or documented BFF).

---

## Risks

| Risk | Mitigation |
|------|------------|
| Auth logic leaking into every module | Only Host/Identity issues cookies and loads the permission set. Modules receive `ICurrentUser` (or equivalent). |
| Painful SSO migration | Store a stable user id; avoid scattering password checks; prefer standard claims (`sub`, email). |
| Tokens in localStorage | Do not. Cookies for web. |
| Building a full IAM product | Identity is a means to sign in. Do not build a competitor to enterprise IAM. |
| Invitations/turnover half-implemented | Treat disable/invite as product requirements when the first users exist; still use Identity APIs, not custom auth. |

---

## Revisit Conditions

- A pilot or customer requires SSO/SAML/OIDC with an existing corporate directory.
- Mobile app development starts and cookie-only auth is insufficient.
- Running Identity in-app becomes a compliance problem (evidence required).
- Multiple HuGuWeb surfaces need a shared authorization server.

---

## Date

2026-08-17

---

## Related Documents

- [ADR-002 Backend Platform](ADR-002-Backend-Platform.md)
- [ADR-003 Frontend Architecture](ADR-003-Frontend-Architecture.md)
- [ADR-008 Authorization Strategy](ADR-008-Authorization-Strategy.md)
- [Future Scope](../../product/FUTURE_SCOPE.md)
