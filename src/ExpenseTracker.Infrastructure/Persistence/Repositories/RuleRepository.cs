using ExpenseTracker.Domain.Rules;
using ExpenseTracker.Services.Contracts;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using Rule = ExpenseTracker.Domain.Rules.Rule;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

/// <summary>
/// SQLite-backed repository for <see cref="Rule"/> persistence and queries.
/// </summary>
/// <remarks>
/// <para>
/// Composite conditions (AND/OR/NOT) are serialized as JSON in the <c>match_text</c> column
/// with <c>match_type = 1</c>. Simple v1 rules use <c>match_type = 0</c> (Contains).
/// </para>
/// </remarks>
public sealed class RuleRepository : IRuleRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates a new <see cref="RuleRepository"/>.
    /// </summary>
    public RuleRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task<Rule?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              SELECT id, name, match_type, match_text, category_id, priority, enabled
              FROM rules
              WHERE id = @id
              LIMIT 1;
             ";

        AddParam(cmd, "@id", id.ToString());

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return MapRule(reader);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Rule>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              SELECT id, name, match_type, match_text, category_id, priority, enabled
              FROM rules
              ORDER BY priority ASC;
             ";

        var results = new List<Rule>();

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(MapRule(reader));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Rule>> GetEnabledAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              SELECT id, name, match_type, match_text, category_id, priority, enabled
              FROM rules
              WHERE enabled = 1
              ORDER BY priority ASC;
             ";

        var results = new List<Rule>();

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(MapRule(reader));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(Rule rule, CancellationToken ct = default)
    {
        if (rule is null)
            throw new ArgumentNullException(nameof(rule));

        if (rule.Id == Guid.Empty)
            throw new ArgumentException("Rule id cannot be empty.", nameof(rule));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        var (matchType, matchText) = SerializeCondition(rule.Condition);

        cmd.CommandText =
            @"
              INSERT INTO rules (id, name, match_type, match_text, category_id, priority, enabled)
              VALUES (@id, @name, @match_type, @match_text, @category_id, @priority, @enabled)
              ON CONFLICT(id) DO UPDATE SET
                name        = excluded.name,
                match_type  = excluded.match_type,
                match_text  = excluded.match_text,
                category_id = excluded.category_id,
                priority    = excluded.priority,
                enabled     = excluded.enabled;
             ";

        AddParam(cmd, "@id", rule.Id.ToString());
        AddParam(cmd, "@name", rule.Name);
        AddParam(cmd, "@match_type", matchType);
        AddParam(cmd, "@match_text", matchText);
        AddParam(cmd, "@category_id", rule.CategoryId.ToString());
        AddParam(cmd, "@priority", rule.Priority);
        AddParam(cmd, "@enabled", rule.Enabled ? 1 : 0);

        await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"DELETE FROM rules WHERE id = @id;";
        AddParam(cmd, "@id", id.ToString());

        var affected = await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
        if (affected == 0)
            throw new KeyNotFoundException($"No rule exists with id '{id}'.");
    }

    /// <inheritdoc />
    public async Task DeleteByCategoryAsync(Guid categoryId, CancellationToken ct = default)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category id cannot be empty.", nameof(categoryId));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"DELETE FROM rules WHERE category_id = @category_id;";
        AddParam(cmd, "@category_id", categoryId.ToString());

        await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
    }

    // =====================================================
    // Condition Serialization
    // =====================================================

    private static (int matchType, string matchText) SerializeCondition(IRuleCondition condition)
    {
        // match_type 0 = simple Contains (v1 legacy)
        // match_type 1 = JSON-serialized composite condition
        var node = SerializeConditionNode(condition);
        var json = JsonSerializer.Serialize(node, JsonOptions);
        return (1, json);
    }

    private static ConditionNode SerializeConditionNode(IRuleCondition condition)
    {
        return condition switch
        {
            ContainsCondition c => new ConditionNode { Type = "Contains", Text = c.Fragment },
            StartsWithCondition c => new ConditionNode { Type = "StartsWith", Text = c.Prefix },
            EndsWithCondition c => new ConditionNode { Type = "EndsWith", Text = c.Suffix },
            AndCondition and => SerializeComposite("And", and),
            OrCondition or => SerializeComposite("Or", or),
            NotCondition not => SerializeNot(not),
            _ => throw new InvalidOperationException($"Unknown condition type: {condition.GetType().Name}")
        };
    }

    private static ConditionNode SerializeComposite(string type, IRuleCondition composite)
    {
        // Access private _conditions field via reflection since there's no public accessor
        var field = composite.GetType().GetField("_conditions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var conditions = (IReadOnlyList<IRuleCondition>)field!.GetValue(composite)!;

        return new ConditionNode
        {
            Type = type,
            Children = conditions.Select(SerializeConditionNode).ToList()
        };
    }

    private static ConditionNode SerializeNot(NotCondition not)
    {
        var field = not.GetType().GetField("_inner",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var inner = (IRuleCondition)field!.GetValue(not)!;

        return new ConditionNode
        {
            Type = "Not",
            Children = new List<ConditionNode> { SerializeConditionNode(inner) }
        };
    }

    private static IRuleCondition DeserializeCondition(int matchType, string matchText)
    {
        if (matchType == 0)
        {
            // Legacy v1: simple Contains
            return new ContainsCondition(matchText);
        }

        // match_type 1: JSON composite
        var node = JsonSerializer.Deserialize<ConditionNode>(matchText, JsonOptions)
            ?? throw new DataException("Failed to deserialize rule condition JSON.");

        return DeserializeConditionNode(node);
    }

    private static IRuleCondition DeserializeConditionNode(ConditionNode node)
    {
        return node.Type switch
        {
            "Contains" => new ContainsCondition(node.Text ?? ""),
            "StartsWith" => new StartsWithCondition(node.Text ?? ""),
            "EndsWith" => new EndsWithCondition(node.Text ?? ""),
            "And" => new AndCondition(node.Children?.Select(DeserializeConditionNode)
                ?? throw new DataException("AND node has no children.")),
            "Or" => new OrCondition(node.Children?.Select(DeserializeConditionNode)
                ?? throw new DataException("OR node has no children.")),
            "Not" => new NotCondition(DeserializeConditionNode(
                node.Children?.FirstOrDefault()
                ?? throw new DataException("NOT node has no child."))),
            _ => throw new DataException($"Unknown condition node type '{node.Type}'.")
        };
    }

    // =====================================================
    // Mapping
    // =====================================================

    private static Rule MapRule(IDataRecord record)
    {
        // SELECT order: 0 id, 1 name, 2 match_type, 3 match_text, 4 category_id, 5 priority, 6 enabled
        var id = Guid.Parse(record.GetString(0));
        var name = record.GetString(1);
        var matchType = record.GetInt32(2);
        var matchText = record.GetString(3);
        var categoryId = Guid.Parse(record.GetString(4));
        var priority = record.GetInt32(5);
        var enabled = ReadBool(record.GetValue(6));

        var condition = DeserializeCondition(matchType, matchText);
        return new Rule(id, name, condition, categoryId, priority, enabled);
    }

    private static bool ReadBool(object value)
    {
        return value switch
        {
            bool b => b,
            long l => l != 0,
            int i => i != 0,
            short s => s != 0,
            byte by => by != 0,
            string str when int.TryParse(str, out var i) => i != 0,
            _ => Convert.ToInt32(value) != 0
        };
    }

    // =====================================================
    // ADO helpers (same pattern as CategoryRepository)
    // =====================================================

    private static void AddParam(IDbCommand command, string name, object? value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        command.Parameters.Add(p);
    }

    private static Task<int> ExecuteNonQueryAsync(IDbCommand cmd, CancellationToken ct)
    {
        if (cmd is SqliteCommand sc)
            return sc.ExecuteNonQueryAsync(ct);

        ct.ThrowIfCancellationRequested();
        return Task.FromResult(cmd.ExecuteNonQuery());
    }

    private static Task<SqliteDataReader> ExecuteReaderAsync(IDbCommand cmd, CancellationToken ct)
    {
        if (cmd is SqliteCommand sc)
            return sc.ExecuteReaderAsync(ct);

        ct.ThrowIfCancellationRequested();
        return Task.FromResult<SqliteDataReader>(
            (SqliteDataReader)(((IDataReader)cmd.ExecuteReader()) as DbDataReader
                               ?? throw new InvalidOperationException("Command did not return a DbDataReader.")));
    }

    // =====================================================
    // JSON model for condition serialization
    // =====================================================

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private sealed class ConditionNode
    {
        public string Type { get; set; } = "";
        public string? Text { get; set; }
        public List<ConditionNode>? Children { get; set; }
    }
}
