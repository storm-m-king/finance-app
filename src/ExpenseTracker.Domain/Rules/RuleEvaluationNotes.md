# Rule Evaluation Design Notes

## Responsibilities

- Rule:
    - Owns identity, priority, enabled state, and category mapping
    - Delegates matching logic to a condition

- RuleCondition:
    - Encapsulates matching semantics
    - Supports composition via AND / OR / NOT

- Rule Service:
    - Orders rules by priority
    - Applies evaluation policy (first match wins, etc.)
    - Does not implement matching logic

## Example

AND(StartsWith("AMZN"), EndsWith("MARKETPLACE"))

This structure allows:
- UI-driven rule builders
- JSON serialization
- Future expansion to amount/date/vendor conditions
