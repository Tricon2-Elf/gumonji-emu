using gumonji.Common.DAL.Entities;
using gumonji.Common.World;
using Microsoft.EntityFrameworkCore;

namespace gumonji.Common.DAL.Repositories;

public sealed record HarvestResult(InventoryItem? Item, PlantState? Plant, string? Error);

public interface IGameplayRepository
{
    Task AddConditionAsync(uint userId, uint walking, uint swimming, long seconds, CancellationToken ct = default);
    Task<List<InventoryItem>> GetInventoryAsync(uint userId, CancellationToken ct = default);
    Task<PlantState?> GetPlantAsync(int zoneId, uint plantId, CancellationToken ct = default);
    Task<HarvestResult> HarvestAsync(uint userId, int zoneId, uint plantId, uint x, uint y, CancellationToken ct = default);
}

public sealed class GameplayRepository(IDbContextFactory<MainContext> factory) : IGameplayRepository
{
    public async Task AddConditionAsync(uint userId, uint walking, uint swimming, long seconds, CancellationToken ct = default)
    {
        if (seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        await using var db = await factory.CreateDbContextAsync(ct);
        // Atomic increments: separate sessions must not overwrite one another's totals.
        await db.Characters.Where(c => c.UserId == (long)userId).ExecuteUpdateAsync(s => s
            .SetProperty(c => c.PlayedSeconds, c => c.PlayedSeconds + seconds)
            .SetProperty(c => c.WalkingDistance, c => c.WalkingDistance + walking)
            .SetProperty(c => c.SwimmingDistance, c => c.SwimmingDistance + swimming), ct);
    }

    public async Task<List<InventoryItem>> GetInventoryAsync(uint userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.InventoryItems.AsNoTracking()
            .Where(i => db.Characters.Any(c => c.Id == i.CharacterId && c.UserId == (long)userId))
            .OrderBy(i => i.Slot).ToListAsync(ct);
    }

    public async Task<PlantState?> GetPlantAsync(int zoneId, uint plantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.PlantStates.AsNoTracking()
            .SingleOrDefaultAsync(p => p.ZoneId == zoneId && p.PlantId == (long)plantId, ct);
    }

    public async Task<HarvestResult> HarvestAsync(uint userId, int zoneId, uint plantId, uint x, uint y,
        CancellationToken ct = default)
    {
        var tree = SpawnTrees.All.SingleOrDefault(t => t.Id == plantId);
        if (tree.Id == 0)
            return new(null, null, "There is no harvestable tree here.");
        // Position reports use thousandths of a cell; retail checks a four-cell reach.
        var dx = (double)x - tree.X * 1000;
        var dy = (double)y - tree.Y * 1000;
        if (dx * dx + dy * dy > 4000.0 * 4000.0)
            return new(null, null, "Move closer to the tree to harvest it.");

        await using var db = await factory.CreateDbContextAsync(ct);
        // SQLite's immediate transaction serializes the ripe check and slot allocation.
        // Award and tree depletion commit together, including across connections.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var character = await db.Characters.SingleOrDefaultAsync(c => c.UserId == (long)userId, ct)
            ?? throw new InvalidDataException("harvest without a saved character");
        var plant = await db.PlantStates.FindAsync([zoneId, checked((int)plantId)], ct);
        if (plant is null)
        {
            plant = new PlantState { ZoneId = zoneId, PlantId = (int)plantId, Fertility = (int)tree.Fertility };
            db.PlantStates.Add(plant);
        }
        if (plant.Stage != 4 || plant.Fertility < 200)
            return new(null, plant, "This tree is not ready to harvest.");
        var occupied = await db.InventoryItems.Where(i => i.CharacterId == character.Id)
            .Select(i => i.Slot).ToListAsync(ct);
        var slot = Enumerable.Range(0, 16).FirstOrDefault(i => !occupied.Contains(i), -1);
        if (slot < 0)
            return new(null, plant, "Your inventory is full.");

        // Initial green-tree reward: bamboo_tree_seed from item.tmpl.
        // A valid member of retail's green-tree loot pool, not its full random table.
        var item = new InventoryItem
        {
            CharacterId = character.Id, Slot = slot, ItemType = 92, Subtype = 5, Color = 8, Fertility = 200,
        };
        db.InventoryItems.Add(item);
        plant.Fertility -= item.Fertility;
        plant.Stage = 0; // retail harvest clears the ripe stage; regrowth is a separate simulation
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(item, plant, null);
    }
}
