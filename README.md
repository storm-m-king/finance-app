# Expense Tracker (Desktop)

A **local-first desktop expense tracker** built with **C#**, **.NET**, and **Avalonia UI**, focused on **explicit financial semantics** and **manual review**.

---

## What This Is

A personal, single-user application for tracking expenses across checking and credit accounts using manual entry and CSV imports.  
All data is stored locally. No cloud services, bank syncing, or background automation.

The system favors **clarity and correctness** over convenience.

---

## Scope (v1)

- Multiple accounts (checking, credit)
- Manual entry and CSV import
- Explicit categorization with optional rules
- Review workflow for uncategorized or exceptional transactions
- Spending and income dashboards

Out of scope by design:
- Budgets
- Forecasting
- Bank APIs
- Mobile or multi-user support

---

## Design-First

The system is defined by an authoritative design document:
[Expense Tracker v1 Design](docs/design/expense-tracker-v1-design.md)

That document specifies domain semantics, invariants, architecture, and UML diagrams.  
Implementation is expected to conform to it.

---

## Repository Layout
```
├── docs/
│   ├── design/
│   │   └── expense-tracker-v1-design.md
│   └── diagrams/
│       ├── puml/
│       │   ├── 01-domain-class.puml
│       │   ├── 02-import-sequence.puml
│       │   └── ...
│       └── img/
│           ├── 01-domain-class.png
│           ├── 02-import-sequence.png
│           └── ...
└── README.md
```
