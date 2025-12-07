# Privacy Policy - DevSecurityGuard

**Last Updated:** December 6, 2024  
**Version:** 2.0

## Our Commitment

DevSecurityGuard is committed to protecting your privacy. This document explains our data collection and usage practices.

## Data Collection

### What We DON'T Collect (By Default)

- ❌ **No Telemetry:** We do not send usage analytics
- ❌ **No Tracking:** We do not track your activity
- ❌ **No Personal Data:** We do not collect personal information
- ❌ **No Package Data:** We do not upload your package lists
- ❌ **No Source Code:** We never access your source code

### What is Stored Locally

- ✅ **Scan Results:** Stored in local SQLite database (`C:\ProgramData\DevSecurityGuard\devsecurity.db`)
- ✅ **Configuration:** Stored in your AppData folder
- ✅ **Cache:** Temporary scan cache for performance
- ✅ **Logs:** Local log files for debugging

## Optional Features (Opt-In Only)

### 1. Threat Intelligence Feed

**What:** Download community-curated threat signatures  
**Data Sent:** None (read-only API)  
**Enable:** Set `privacy.threatFeedEnabled = true` in config

### 2. Anonymous Statistics

**What:** Share anonymous threat counts to improve detection  
**Data Sent:** Threat type counts (no package names, no identifying info)  
**Enable:** Set `privacy.telemetryEnabled = true` in config

## Data Storage

### Local Database

Location: `%ProgramData%\DevSecurityGuard\devsecurity.db`

Contains:
- Detected threats
- Scan history
- Configuration settings

**Encryption:** Optional (set `privacy.encryptDatabase = true`)

### Log Files

Location: `logs/devsecurityguard-*.log`

Contains:
- Application events
- Error messages
- Performance metrics

**Retention:** 30 days (automatic rotation)

## Your Rights

You have the right to:

1. ✅ **Access:** View all data stored locally
2. ✅ **Delete:** Clear database at any time (`dsg config set clearData true`)
3. ✅ **Export:** Export data as JSON
4. ✅ **Opt-Out:** All cloud features are opt-in by default
5. ✅ **Offline Mode:** Run 100% offline

## Third-Party Services

### Package Registries

We query public package registries (npm, PyPI, crates.io, etc.) to:
- Verify package metadata
- Check for known vulnerabilities
- Detect typosquatting

**Data Sent:** Package names only (standard API calls)  
**Privacy:** Same as using `npm install` or `pip install`

### No Other Services

We do NOT use:
- ❌ Google Analytics
- ❌ Crash reporting services
- ❌ Marketing trackers
- ❌ Social media integrations

## Open Source

DevSecurityGuard is 100% open source:
- **Source Code:** github.com/devsecurityguard/devsecurityguard
- **Audit:** You can verify our privacy claims
- **Contribute:** Community-driven development

## Changes to This Policy

We will notify users of any privacy policy changes through:
- GitHub releases
- In-app notifications (local only)

## Contact

Questions? Open an issue on GitHub.

## Summary

**TL;DR:**
- 🔒 **100% Local by Default**
- 🚫 **Zero Telemetry**
- 🔓 **Opt-In for Cloud Features**
- 📖 **Open Source & Auditable**
- 🛡️ **Your Data Stays Yours**
