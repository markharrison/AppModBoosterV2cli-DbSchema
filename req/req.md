# Requirements

---

## Objectives

Modernise a legacy database application.

Additional requirements are located in `./requirements/*`.

## Workflow Steps Order

Execute in strict sequence — each step uses the output of the previous:

1. **Analyse** — Examine `./input/*`, produce Legacy Analysis document.
2. **Specify** — Only using the Legacy Analysis document for input, produce the new App Specification. Do NOT start until the Analyse step has completed.
3. **Build** — Only using the App Specification for input, implement: applications development, infrastructure templates / scripts, and testing. Do NOT start until the Specify step has completed.
4. **Validate** — Build solution, run ALL test suites (unit and E2E), verify endpoints, fix issues, complete documentation. No test suite may be skipped or deferred.

NEVER parallelise across workflow steps. Complete each step before starting the next phase.
Within a single phase, parallelise freely.

## Input

The legacy application to modernize is located in `./input/*`.

Input may include one or more of the following:

- database application file(s) such as:
  - Microsoft Access `.accdb` or `.mdb` file
  - FileMaker Pro `.fmp12` file
  - Visual FoxPro application, typically a number of files such as `.dbf`, `.dbc`, `.fpt`, `.cdx`
  - PowerBuilder `.pbl` file
  - dBASE `.dbf` file
  - Firebird `.fdb` file
  - other legacy database formats
- a folder containing any subset of natural language description of the application, UI screenshots, scripts, queries/stored procedures, reports, code, sample data, database schema, datamodel, ERD/relationship diagrams, and business-rule documentation

Identify the application type and confirm it is a supported legacy database app

## Outputs

All outputs should be in folders within the `./output/` folder.
One exception is GitHub workflow files - they should be in the `./.github/workflows/` folder.

Outputs to include:

- `./output/Src`
  - Application Source Code
  - Unit tests
  - Playwright end-to-end test
- `./output/Infra`
  - Iac
  - Scripts
- `./output/Docs`
  - Legacy Analysis document
  - Application Specification
  - Deployment documentation
  - Testing documentation
- `./output/Temp`
  - Temporary files e.g. for Publishing
- `./.github/workflows/`
  - GitHub Workflows

---
