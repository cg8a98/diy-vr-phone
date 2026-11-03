# Founder Playbook: {{product name}}

{{date}} · For {{owner}} · Status: draft / agreed

## 1. How to use this playbook
*Two or three sentences: what this document is, that items marked **Blocking** must be done before coding starts, and that the owner says "let's start" when they are. Every task below uses the same format:*

> **Task name** - Blocking / Before launch / Ongoing
> What: … Why: … How: numbered steps … Time: … Cost: … (verify current price) · Done when: …

*Include every standard task below that applies, adapted to this project, plus any project-specific tasks. Remove a standard task only by marking it "Not applicable - reason".*

## 2. Before coding can start (Blocking)

### 2.1 Accounts and tools
*Standard tasks: GitHub account with two-factor authentication; Git for Windows installed (only needed to work on this PC); repository created from the owner's starter template; Claude GitHub App installed on that repository; Claude Code Project created with the standard instructions and settings (thread model Sonnet, medium effort); password manager; separate business email if the product will have customers.*

### 2.2 Validate before building
*Talk to 5-10 potential customers using a short script (included here). Collect real examples and test cases from reference customers. Optional: a one-page waitlist or pre-sale to test demand. Record what was learned in docs/decisions.md.*

### 2.3 Inputs only the owner can provide
*Domain knowledge, reference data and expected results for testing, priorities, branding preferences, sign-off on the v1 scope.*

### 2.4 Budget and time
*Set the pre-revenue spending cap and monthly cap. Use one card for all project costs. Block weekly time on the calendar (the hours from the interview).*

## 3. Before launch

### 3.1 Business and legal
*Choose a business structure for the owner's country/state (sole proprietor vs LLC or equivalent) - list options with pros/cons and say to confirm with an accountant or lawyer. Tax ID, business bank account. Check the employer agreement for IP/side-project clauses. Name check: company/trademark search, domain availability. Terms of service, privacy policy, end-user license agreement, and disclaimers (especially for calculation or advice tools). Professional liability insurance if results could cause losses. Any industry certification or standards review.*

### 3.2 Getting paid
*Pick a payment provider; prefer a "merchant of record" (they handle sales tax/VAT) for software sold internationally. Pricing page. Invoicing for business customers. Refund policy.*

### 3.3 Distribution
*Platform-specific: for Windows apps, a code-signing certificate (avoids security warnings) and an installer; Microsoft Store account if used. For mobile, Apple Developer and Google Play accounts. For web, domain, hosting, uptime monitoring. Licensing/activation if selling desktop licenses.*

### 3.4 Launch and sales
*Website/landing page, launch list (communities, associations, partners from the Market Research), first-10-customers plan, support channel (email/help page), simple analytics, feedback collection, testimonials.*

## 4. Ongoing routines

### Daily (10-20 min, on work days)
*Check the project's "Waiting on you" and ready-for-review items; answer questions; approve or reject merges the PM recommends; note blockers.*

### Weekly (about 1 hour, fixed day)
*Review progress against the Build Plan milestone; agree the next epic with the PM; test the latest build on reference cases; check Claude usage (/usage or the usage ring) against the weekly budget; one or more customer conversations; update HANDOFF.md decisions.*

### Monthly
*Costs vs revenue; key metrics (users, paying customers, churn, support load); security and dependency updates approved; backups checked; taxes/bookkeeping; the upgrade check (section 5).*

### Per release
*Reference-case test pass signed off by the owner; release notes; installer/store submission; announce to customers.*

## 5. When to upgrade the Claude plan
*Rule: stay on Pro until both are true for two consecutive months: (a) the project's revenue covers the price difference to the next plan, and (b) Pro limits are measurably delaying the roadmap (weeks where work stopped at the limit). Then move to Max 5x; move to Max 20x only when scope grows (more features, more parallel builders) and revenue covers it. Record the decision in docs/decisions.md.*

## 6. Decision rights
*What the owner decides (priorities, scope, price, merges of risky changes, anything legal or money) vs what the PM decides (ticket breakdown, model tier, technical approach within the agreed stack).*

## 7. Founder checklist summary
*A single table of every task above: task, category, Blocking / Before launch / Ongoing, time, cost, status (not started / done).*

| Task | Category | When | Time | Cost | Status |
|---|---|---|---|---|---|
