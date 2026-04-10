# App Mod Booster

Use Copilot CLI to modernise legacy database application.

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

## Copilot CLI Prompt

Prompt :

```
 /fleet Modernise legacy database application.   Use requirements in @req\* .
```

---

Mark Harrison
