using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data;
using System.Text.RegularExpressions;

namespace Savio.MockServer.Services;

public class AliasService
{
    private readonly MockDbContext _context;

    public AliasService(MockDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsAliasAvailableAsync(string alias, string? excludeUserId = null)
    {
        return !await _context.Users
            .AnyAsync(u => u.Alias == alias && (excludeUserId == null || u.Id != excludeUserId));
    }

    public bool IsValidAliasFormat(string alias)
    {
        return Regex.IsMatch(alias, @"^[a-z0-9][a-z0-9_-]{1,48}[a-z0-9]$");
    }
}
