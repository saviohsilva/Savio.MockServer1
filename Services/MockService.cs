using Savio.MockServer.Data.Repositories;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;

namespace Savio.MockServer.Services;

public class MockService(IMockRepository repository, IMockGroupRepository groupRepository)
{
    private readonly IMockRepository _repository = repository;
    private readonly IMockGroupRepository _groupRepository = groupRepository;

    // ── Mocks ──────────────────────────────────────────────

    public async Task<List<MockEndpoint>> GetAllMocksAsync(string? userId = null)
    {
        var entities = await _repository.GetAllAsync(userId);
        return [.. entities.Select(EntityToModel)];
    }

    public async Task<List<MockEndpoint>> GetFilteredMocksAsync(MockFilter filter)
    {
        var entities = await _repository.GetFilteredAsync(filter);
        return [.. entities.Select(EntityToModel)];
    }

    public async Task<List<MockEndpoint>> GetStandaloneMocksAsync(MockFilter? filter = null)
    {
        var entities = await _repository.GetStandaloneMocksAsync(filter);
        return [.. entities.Select(EntityToModel)];
    }

    public async Task<List<MockEndpoint>> GetMocksByGroupIdAsync(int groupId)
    {
        var entities = await _repository.GetByGroupIdAsync(groupId);
        return [.. entities.Select(EntityToModel)];
    }

    public async Task<MockEndpoint?> GetMockByIdAsync(string id)
    {
        if (!int.TryParse(id, out int numericId))
            return null;

        var entity = await _repository.GetByIdAsync(numericId);
        return entity != null ? EntityToModel(entity) : null;
    }

    public async Task<(bool success, string? error)> AddMockAsync(MockEndpoint mock, string? userId = null)
    {
        var conflict = await CheckActiveConflictAsync(mock.Route, mock.Method, mock.IsActive, null, userId);
        if (conflict != null)
        {
            return (false, conflict);
        }

        var entity = ModelToEntity(mock);
        entity.UserId = userId;
        await _repository.AddAsync(entity);
        return (true, null);
    }

    public async Task<(bool success, string? error)> UpdateMockAsync(MockEndpoint mock, string? userId = null)
    {
        var excludeId = int.TryParse(mock.Id, out int numericId) ? numericId : (int?)null;
        var conflict = await CheckActiveConflictAsync(mock.Route, mock.Method, mock.IsActive, excludeId, userId);
        if (conflict != null)
        {
            return (false, conflict);
        }

        var entity = ModelToEntity(mock);
        entity.Id = numericId;
        entity.UserId = userId;
        await _repository.UpdateAsync(entity);
        return (true, null);
    }

    public async Task DeleteMockAsync(string id)
    {
        if (int.TryParse(id, out int numericId))
        {
            await _repository.DeleteAsync(numericId);
        }
    }

    public async Task<(bool success, string? error)> DuplicateMockAsync(string id)
    {
        if (!int.TryParse(id, out int numericId))
        {
            return (false, "ID inválido.");
        }

        var original = await _repository.GetByIdAsync(numericId);
        if (original == null)
        {
            return (false, "Mock não encontrado.");
        }

        var clone = new MockEndpointEntity
        {
            Route = original.Route,
            Method = original.Method,
            StatusCode = original.StatusCode,
            ResponseBodyJson = original.ResponseBodyJson,
            ResponseBodyRaw = original.ResponseBodyRaw,
            ResponseBinaryBlobId = original.ResponseBinaryBlobId,
            ResponseBodyBase64 = original.ResponseBodyBase64,
            ResponseBodyContentType = original.ResponseBodyContentType,
            ResponseBodyFileName = original.ResponseBodyFileName,
            ResponseMultipartJson = original.ResponseMultipartJson,
            DelayMs = original.DelayMs,
            Description = !string.IsNullOrWhiteSpace(original.Description)
                ? $"{original.Description} (cópia)"
                : "(cópia)",
            IsActive = false,
            CallCount = 0,
            LastCalledAt = null,
            CreatedAt = DateTime.UtcNow,
            MockGroupId = original.MockGroupId,
            UserId = original.UserId
        };

        clone.SetHeaders(original.GetHeaders());
        await _repository.AddAsync(clone);
        return (true, null);
    }

    public async Task<(bool success, string? error)> DuplicateGroupAsync(int groupId)
    {
        var group = await _groupRepository.GetByIdWithMocksAsync(groupId);
        if (group == null)
        {
            return (false, "Agrupamento não encontrado.");
        }

        var newName = $"{group.Name} (cópia)";
        var suffix = 2;
        while (await _groupRepository.ExistsByNameAsync(newName, null, group.UserId))
        {
            newName = $"{group.Name} (cópia {suffix})";
            suffix++;
        }

        var newGroup = new MockGroupEntity
        {
            Name = newName,
            Description = group.Description,
            UserId = group.UserId
        };

        await _groupRepository.AddAsync(newGroup);

        foreach (var mock in group.MockEndpoints)
        {
            var clone = new MockEndpointEntity
            {
                Route = mock.Route,
                Method = mock.Method,
                StatusCode = mock.StatusCode,
                ResponseBodyJson = mock.ResponseBodyJson,
                ResponseBodyRaw = mock.ResponseBodyRaw,
                ResponseBinaryBlobId = mock.ResponseBinaryBlobId,
                ResponseBodyBase64 = mock.ResponseBodyBase64,
                ResponseBodyContentType = mock.ResponseBodyContentType,
                ResponseBodyFileName = mock.ResponseBodyFileName,
                ResponseMultipartJson = mock.ResponseMultipartJson,
                DelayMs = mock.DelayMs,
                Description = mock.Description,
                IsActive = false,
                CallCount = 0,
                LastCalledAt = null,
                CreatedAt = DateTime.UtcNow,
                MockGroupId = newGroup.Id,
                UserId = mock.UserId
            };

            clone.SetHeaders(mock.GetHeaders());
            await _repository.AddAsync(clone);
        }

        return (true, null);
    }

    public async Task RecordCallAsync(string route, string method, string? userId = null)
    {
        var entity = await _repository.GetActiveByRouteAndMethodAsync(route, method, null, userId);
        if (entity != null)
        {
            await _repository.IncrementCallCountAsync(entity.Id);
        }
    }

    public async Task<(bool success, string? error)> SetMockActiveAsync(string id, bool isActive)
    {
        if (!int.TryParse(id, out int numericId))
        {
            return (false, "ID inválido.");
        }

        var entity = await _repository.GetByIdAsync(numericId);
        if (entity == null)
        {
            return (false, "Mock não encontrado.");
        }

        if (isActive)
        {
            var conflict = await CheckActiveConflictAsync(entity.Route, entity.Method, true, numericId, entity.UserId);
            if (conflict != null)
            {
                return (false, conflict);
            }
        }

        await _repository.SetActiveBulkAsync([numericId], isActive);
        return (true, null);
    }

    public async Task<string?> CheckActiveConflictAsync(string route, string method, bool isActive, int? excludeId, string? userId = null)
    {
        if (!isActive)
        {
            return null;
        }

        var existing = await _repository.GetActiveByRouteAndMethodAsync(route, method, excludeId, userId);
        if (existing != null)
        {
            var desc = !string.IsNullOrWhiteSpace(existing.Description) ? existing.Description : $"{existing.Method} {existing.Route}";
            var groupInfo = existing.MockGroupId.HasValue ? " (em um agrupamento)" : " (isolado)";
            return $"Já existe um mock ativo para [{method}] {route}: \"{desc}\"{groupInfo}. Desative-o antes de ativar este.";
        }

        return null;
    }

    // ── Grupos ──────────────────────────────────────────────

    public async Task<List<MockGroup>> GetAllGroupsAsync(string? userId = null)
    {
        var entities = await _groupRepository.GetAllWithMocksAsync(userId);
        return [.. entities.Select(GroupEntityToModel)];
    }

    public async Task<MockGroup?> GetGroupByIdAsync(int id)
    {
        var entity = await _groupRepository.GetByIdWithMocksAsync(id);
        return entity != null ? GroupEntityToModel(entity) : null;
    }

    public async Task<(bool success, string? error)> AddGroupAsync(string name, string? description, string? color = null, string? userId = null)
    {
        if (await _groupRepository.ExistsByNameAsync(name, null, userId))
        {
            return (false, $"Já existe um agrupamento com o nome \"{name}\".");
        }

        var entity = new MockGroupEntity
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
            UserId = userId
        };

        await _groupRepository.AddAsync(entity);
        return (true, null);
    }

    public async Task<(bool success, string? error)> UpdateGroupAsync(int id, string name, string? description, string? color = null, string? userId = null)
    {
        if (await _groupRepository.ExistsByNameAsync(name, id, userId))
        {
            return (false, $"Já existe um agrupamento com o nome \"{name}\".");
        }

        var entity = await _groupRepository.GetByIdAsync(id);
        if (entity == null)
        {
            return (false, "Agrupamento não encontrado.");
        }

        entity.Name = name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        entity.Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        await _groupRepository.UpdateAsync(entity);
        return (true, null);
    }

    public async Task DeleteGroupAsync(int id)
    {
        await _groupRepository.DeleteAsync(id);
    }

    public async Task AddMockToGroupAsync(string mockId, int groupId)
    {
        if (!int.TryParse(mockId, out int numericId))
        {
            return;
        }

        var entity = await _repository.GetByIdAsync(numericId);
        if (entity != null)
        {
            entity.MockGroupId = groupId;
            await _repository.UpdateAsync(entity);
        }
    }

    public async Task RemoveMockFromGroupAsync(string mockId)
    {
        if (!int.TryParse(mockId, out int numericId))
        {
            return;
        }

        var entity = await _repository.GetByIdAsync(numericId);
        if (entity != null)
        {
            entity.MockGroupId = null;
            await _repository.UpdateAsync(entity);
        }
    }

    public async Task<(bool success, string? error, List<string> conflicts)> ActivateGroupMocksAsync(int groupId)
    {
        var mocks = await _repository.GetByGroupIdAsync(groupId);
        var conflicts = new List<string>();

        foreach (var mock in mocks)
        {
            var existing = await _repository.GetActiveByRouteAndMethodAsync(mock.Route, mock.Method, mock.Id);

            if (existing != null)
            {
                var desc = !string.IsNullOrWhiteSpace(existing.Description) ? existing.Description : $"{existing.Method} {existing.Route}";
                conflicts.Add($"[{mock.Method}] {mock.Route} → conflito com \"{desc}\"");
            }
        }

        if (conflicts.Count > 0)
        {
            return (false, "Não é possível ativar todos os mocks do grupo. Os seguintes mocks já estão ativos em outro local:", conflicts);
        }

        var ids = mocks.Select(m => m.Id);
        await _repository.SetActiveBulkAsync(ids, true);
        return (true, null, conflicts);
    }

    public async Task DeactivateGroupMocksAsync(int groupId)
    {
        var mocks = await _repository.GetByGroupIdAsync(groupId);
        var ids = mocks.Select(m => m.Id);
        await _repository.SetActiveBulkAsync(ids, false);
    }

    // ── Mapeamentos ──────────────────────────────────────────

    private static MockEndpoint EntityToModel(MockEndpointEntity entity)
    {
        return new MockEndpoint
        {
            Id = entity.Id.ToString(),
            Route = entity.Route,
            Method = entity.Method,
            StatusCode = entity.StatusCode,
            Headers = entity.GetHeaders(),
            ResponseBodyJson = entity.ResponseBodyJson ?? string.Empty,
            ResponseBodyRaw = entity.ResponseBodyRaw ?? string.Empty,
            ResponseBinaryBlobId = entity.ResponseBinaryBlobId,
            ResponseBodyBase64 = entity.ResponseBodyBase64 ?? string.Empty,
            ResponseBodyContentType = entity.ResponseBodyContentType ?? string.Empty,
            ResponseBodyFileName = entity.ResponseBodyFileName ?? string.Empty,
            ResponseMultipartJson = entity.ResponseMultipartJson ?? string.Empty,
            DelayMs = entity.DelayMs,
            Description = entity.Description ?? string.Empty,
            IsActive = entity.IsActive,
            CallCount = entity.CallCount,
            LastCalledAt = entity.LastCalledAt,
            CreatedAt = entity.CreatedAt,
            MockGroupId = entity.MockGroupId,
            MockGroupName = entity.MockGroup?.Name,
            MockGroupColor = entity.MockGroup?.Color
        };
    }

    private static MockEndpointEntity ModelToEntity(MockEndpoint model)
    {
        var entity = new MockEndpointEntity
        {
            Route = model.Route,
            Method = model.Method,
            StatusCode = model.StatusCode,
            ResponseBodyJson = model.ResponseBodyJson,
            ResponseBodyRaw = model.ResponseBodyRaw,
            ResponseBinaryBlobId = model.ResponseBinaryBlobId,
            ResponseBodyBase64 = string.IsNullOrWhiteSpace(model.ResponseBodyBase64) ? null : model.ResponseBodyBase64,
            ResponseBodyContentType = string.IsNullOrWhiteSpace(model.ResponseBodyContentType) ? null : model.ResponseBodyContentType,
            ResponseBodyFileName = string.IsNullOrWhiteSpace(model.ResponseBodyFileName) ? null : model.ResponseBodyFileName,
            ResponseMultipartJson = string.IsNullOrWhiteSpace(model.ResponseMultipartJson) ? null : model.ResponseMultipartJson,
            DelayMs = model.DelayMs,
            Description = model.Description,
            IsActive = model.IsActive,
            CallCount = model.CallCount,
            LastCalledAt = model.LastCalledAt,
            CreatedAt = model.CreatedAt,
            MockGroupId = model.MockGroupId
        };

        entity.SetHeaders(model.Headers);
        return entity;
    }

    private static MockGroup GroupEntityToModel(MockGroupEntity entity)
    {
        return new MockGroup
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description ?? string.Empty,
            Color = entity.Color,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            MockEndpoints = [.. entity.MockEndpoints.Select(EntityToModel)]
        };
    }
}
