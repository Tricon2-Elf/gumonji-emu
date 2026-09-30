using gumonji.Common.DAL.Entities;
using gumonji.Common.World;
using Microsoft.EntityFrameworkCore;

namespace gumonji.Common.DAL.Repositories;

public sealed record HarvestResult(InventoryItem? Item, Plant? Plant, string? Error);
public sealed record PlantSeedResult(Plant? Plant, int? UsedSlot, string? Error);

public interface IGameplayRepository
{
    Task AddConditionAsync(uint userId, uint walking, uint swimming, long seconds, CancellationToken ct = default);
    Task<List<InventoryItem>> GetInventoryAsync(uint userId, CancellationToken ct = default);
    Task<InventoryItem?> EnsureStarterVehicleAsync(uint userId, CancellationToken ct = default);
    Task<Plant?> GetPlantAsync(int zoneId, uint plantId, CancellationToken ct = default);
    Task<HarvestResult> HarvestAsync(uint userId, int zoneId, uint plantId, uint x, uint y, CancellationToken ct = default);
    Task<PlantSeedResult> PlantSeedAsync(uint userId, int zoneId, uint slot, ushort x, ushort y,
        uint playerX, uint playerY, CancellationToken ct = default);
    Task<List<Plant>> GetPlantsInChunkAsync(int zoneId, uint chunkX, uint chunkY,
        CancellationToken ct = default);
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

    public async Task<InventoryItem?> EnsureStarterVehicleAsync(uint userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var character = await db.Characters.SingleOrDefaultAsync(c => c.UserId == (long)userId, ct)
            ?? throw new InvalidDataException("vehicle grant without a saved character");
        var current = await db.InventoryItems.FirstOrDefaultAsync(i => i.CharacterId == character.Id && i.ItemType == ItemTemplateIds.ToyCar, ct);
        if (current is not null)
            return current;
        // Previous emulator builds gave every character a black truck as the
        // starter vehicle. Convert that exact starter item in place so existing
        // accounts receive the toy car without losing their inventory slot/id.
        var formerStarter = await db.InventoryItems.FirstOrDefaultAsync(i =>
            i.CharacterId == character.Id && i.ItemType == ItemTemplateIds.Truck &&
            i.Subtype == 0 && i.Color == 0 && i.Fertility == 1000, ct);
        if (formerStarter is not null)
        {
            formerStarter.ItemType = ItemTemplateIds.ToyCar;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return formerStarter;
        }
        var occupied = await db.InventoryItems.Where(i => i.CharacterId == character.Id)
            .Select(i => i.Slot).ToListAsync(ct);
        var slot = Enumerable.Range(0, 16).FirstOrDefault(i => !occupied.Contains(i), -1);
        if (slot < 0)
            return null;
        var vehicle = new InventoryItem
        {
            CharacterId = character.Id, Slot = slot, ItemType = ItemTemplateIds.ToyCar,
            Subtype = 0, Color = 0, Fertility = 1000,
        };
        db.InventoryItems.Add(vehicle);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return vehicle;
    }

    public async Task<Plant?> GetPlantAsync(int zoneId, uint plantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Plants.AsNoTracking()
            .SingleOrDefaultAsync(p => p.ZoneId == zoneId && p.Id == (int)plantId, ct);
    }

    public async Task<List<Plant>> GetPlantsInChunkAsync(int zoneId, uint chunkX, uint chunkY,
        CancellationToken ct = default)
    {
        var minX = checked((int)chunkX * PlantWorld.PageEdge);
        var minY = checked((int)chunkY * PlantWorld.PageEdge);
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Plants.AsNoTracking()
            .Where(p => p.ZoneId == zoneId && p.X >= minX && p.X < minX + PlantWorld.PageEdge &&
                p.Y >= minY && p.Y < minY + PlantWorld.PageEdge)
            .OrderBy(p => p.Id).ToListAsync(ct);
    }

    public async Task<PlantSeedResult> PlantSeedAsync(uint userId, int zoneId, uint slot, ushort x, ushort y,
        uint playerX, uint playerY, CancellationToken ct = default)
    {
        if (slot >= 16 || x >= TerrainWorld.MapEdge || y >= TerrainWorld.MapEdge)
            return new(null, null, "Invalid planting slot or tile.");
        var dx = (double)playerX - x * 1000.0;
        var dy = (double)playerY - y * 1000.0;
        if (dx * dx + dy * dy > 4000.0 * 4000.0)
            return new(null, null, "Move closer to the planting tile.");
        if (TerrainWorld.Cell(x, y).WaterLevel != 0)
            return new(null, null, "Seeds cannot be planted in water.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var character = await db.Characters.SingleOrDefaultAsync(c => c.UserId == (long)userId, ct)
            ?? throw new InvalidDataException("seed planting without a saved character");
        var seed = await db.InventoryItems.SingleOrDefaultAsync(i => i.CharacterId == character.Id && i.Slot == (int)slot, ct);
        if (seed is null || seed.ItemType != TreeSeeds.ItemType ||
            !TreeSeeds.IsSupported(seed.Subtype, seed.Color))
            return new(null, null, "That inventory slot does not contain a supported tree seed.");
        if (await db.Plants.AnyAsync(p => p.ZoneId == zoneId && p.X == x && p.Y == y, ct))
            return new(null, null, "That tile already contains a plant.");

        var nextId = await db.Plants.Where(p => p.ZoneId == zoneId && p.Id >= (int)PlantWorld.PlantedIdBase)
            .Select(p => (int?)p.Id).MaxAsync(ct) ?? (int)PlantWorld.PlantedIdBase - 1;
        var plant = new Plant
        {
            ZoneId = zoneId, Id = checked(nextId + 1), CharacterId = character.Id,
            Type = 3, X = x, Y = y,
            Subtype = seed.Subtype, Color = seed.Color, Fertility = seed.Fertility, Stage = 0,
        };
        db.Plants.Add(plant);
        db.InventoryItems.Remove(seed);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(plant, (int)slot, null);
    }

    public async Task<HarvestResult> HarvestAsync(uint userId, int zoneId, uint plantId, uint x, uint y,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        // SQLite's immediate transaction serializes the ripe check and slot allocation.
        // Award and tree depletion commit together, including across connections.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var character = await db.Characters.SingleOrDefaultAsync(c => c.UserId == (long)userId, ct)
            ?? throw new InvalidDataException("harvest without a saved character");
        var plant = await db.Plants.FindAsync([zoneId, checked((int)plantId)], ct);
        if (plant is null || plant.Type != 3)
            return new(null, null, "There is no harvestable tree here.");
        // Position reports use thousandths of a cell; retail checks a four-cell reach.
        var dx = (double)x - plant.X * 1000;
        var dy = (double)y - plant.Y * 1000;
        if (dx * dx + dy * dy > 4000.0 * 4000.0)
            return new(null, null, "Move closer to the tree to harvest it.");
        if (plant.Stage != 4 || plant.Fertility < 200)
            return new(null, plant, "This tree is not ready to harvest.");
        if (!TreeSeeds.IsSupported(plant.Subtype, plant.Color))
            return new(null, plant, "This tree does not have a supported seed.");
        var occupied = await db.InventoryItems.Where(i => i.CharacterId == character.Id)
            .Select(i => i.Slot).ToListAsync(ct);
        var slot = Enumerable.Range(0, 16).FirstOrDefault(i => !occupied.Contains(i), -1);
        if (slot < 0)
            return new(null, plant, "Your inventory is full.");

        // plant.tmpl tree subtypes 1-5 match item.tmpl tree_seed subtypes 1-5.
        // Keep the current deterministic reward; retail's random loot table is separate.
        var item = new InventoryItem
        {
            CharacterId = character.Id, Slot = slot, ItemType = TreeSeeds.ItemType,
            Subtype = plant.Subtype, Color = plant.Color, Fertility = 200,
        };
        db.InventoryItems.Add(item);
        plant.Fertility -= item.Fertility;
        plant.Stage = 0; // retail harvest clears the ripe stage; regrowth is a separate simulation
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(item, plant, null);
    }
}
