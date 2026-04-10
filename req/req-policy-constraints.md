# Policy Constraints (MCAPS)

- Azure AD-only authentication on SQL Server — no SQL auth (`azureADOnlyAuthentication: true`, no `administratorLogin`/`administratorLoginPassword`)
- Managed identities for all service-to-service auth — no plaintext credentials or secrets
- Connection strings use `Authentication=Active Directory Managed Identity`
- TLS 1.2 minimum, HTTPS-only on web apps

---

