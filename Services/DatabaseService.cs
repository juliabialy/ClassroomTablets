using SQLite;
using ClassroomTablets.Models;

namespace ClassroomTablets.Services;


public class DatabaseService
{
    private SQLiteAsyncConnection? _polaczenie;
    private readonly string _sciezkaBazy;

    public DatabaseService()
    {
        _sciezkaBazy = Path.Combine(FileSystem.AppDataDirectory, "tablety_archiwum.db3");
    }

    private async Task<SQLiteAsyncConnection> PobierzPolaczenieAsync()
    {
        if (_polaczenie is not null)
            return _polaczenie;

        _polaczenie = new SQLiteAsyncConnection(_sciezkaBazy);
        await _polaczenie.CreateTableAsync<ArchiwalnaSesja>();
        return _polaczenie;
    }

   
    public async Task ZapiszSesjeAsync(ArchiwalnaSesja sesja)
    {
        var polaczenie = await PobierzPolaczenieAsync();
        await polaczenie.InsertAsync(sesja);
    }

   
    public async Task ZapiszSesjeAsync(IEnumerable<ArchiwalnaSesja> sesje)
    {
        var polaczenie = await PobierzPolaczenieAsync();
        await polaczenie.InsertAllAsync(sesje);
    }

    
    public async Task<List<ArchiwalnaSesja>> PobierzWszystkieAsync()
    {
        var polaczenie = await PobierzPolaczenieAsync();
        return await polaczenie.Table<ArchiwalnaSesja>()
            .OrderByDescending(s => s.DataRozlaczenia)
            .ToListAsync();
    }


    public async Task<List<GrupaArchiwum>> PobierzPogrupowaneAsync(string? klasa = null, string? uczen = null)
    {
        var wszystkie = await PobierzWszystkieAsync();

        var przefiltrowane = wszystkie.Where(s =>
            (string.IsNullOrWhiteSpace(klasa) || s.ClassName == klasa) &&
            (string.IsNullOrWhiteSpace(uczen) || s.StudentName == uczen));

        return przefiltrowane
            .GroupBy(s => new { s.Sala, s.ClassName, s.Dzien })
            .OrderByDescending(g => g.Key.Dzien)
            .Select(g => new GrupaArchiwum
            {
                Naglowek = $"Sala {g.Key.Sala} Klasa {g.Key.ClassName} {g.Key.Dzien:dd.MM.yyyy}",
                Sesje = g.OrderBy(s => s.DataPolaczenia).ToList()
            })
            .ToList();
    }


    public async Task<List<string>> PobierzUnikalneKlasyAsync()
    {
        var wszystkie = await PobierzWszystkieAsync();
        return wszystkie.Select(s => s.ClassName)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct()
            .OrderBy(k => k)
            .ToList();
    }

   
    public async Task<List<string>> PobierzUnikalnychUczniowAsync()
    {
        var wszystkie = await PobierzWszystkieAsync();
        return wszystkie.Select(s => s.StudentName)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct()
            .OrderBy(u => u)
            .ToList();
    }

   
    public async Task UsunWszystkoAsync()
    {
        var polaczenie = await PobierzPolaczenieAsync();
        await polaczenie.DeleteAllAsync<ArchiwalnaSesja>();
    }
}
