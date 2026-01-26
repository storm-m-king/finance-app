namespace ExpenseTracker.Domain;

public sealed class Category
{
    public Guid Id { get; }
    public string Name { get; }
    public bool IsSystemCategory { get; }
    public bool IsUserEditable => !IsSystemCategory;

    public Category(Guid id, string name, bool isSystemCategory)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        Id = id;
        Name = name.Trim();
        IsSystemCategory = isSystemCategory;
    }

    // rename only allowed for user categories
    public Category Rename(string newName)
    {
        if (!IsUserEditable)
            throw new InvalidOperationException("System categories cannot be renamed.");

        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be null or empty.", nameof(newName));

        return new Category(Id, newName.Trim(), isSystemCategory: false);
    }
}
