using AzMicroApp.Protos;

namespace AzMicroApp.Hotels.Data;

/// <summary>
/// Deterministic in-memory hotel catalogue. Read-only for this lab.
/// </summary>
public static class HotelSeed
{
    public static readonly IReadOnlyDictionary<string, Hotel> Hotels = new Dictionary<string, Hotel>
    {
        ["h1"] = new Hotel { Id = "h1", Name = "Grand Riverside",  City = "Kyiv",     Country = "Ukraine" },
        ["h2"] = new Hotel { Id = "h2", Name = "Alpine Lodge",     City = "Zurich",   Country = "Switzerland" },
        ["h3"] = new Hotel { Id = "h3", Name = "Seaside Resort",   City = "Barcelona", Country = "Spain" },
    };
}
