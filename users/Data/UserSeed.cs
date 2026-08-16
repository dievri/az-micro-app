using AzMicroApp.Protos;

namespace AzMicroApp.Users.Data;

/// <summary>
/// Deterministic in-memory user directory. The Users service is intentionally
/// stateless / read-only in this lab — no database is required.
/// </summary>
public static class UserSeed
{
    public static readonly IReadOnlyDictionary<string, User> Users = new Dictionary<string, User>
    {
        ["u1"] = new User { Id = "u1", Name = "Alice Johnson",  Email = "alice@example.com" },
        ["u2"] = new User { Id = "u2", Name = "Bob Smith",      Email = "bob@example.com" },
        ["u3"] = new User { Id = "u3", Name = "Carla Méndez",   Email = "carla@example.com" },
    };
}
