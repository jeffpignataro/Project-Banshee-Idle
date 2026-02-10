namespace BansheeIdle.Core;

public class InventoryManager
{
    public const int MaxInventorySlots = 28;
    public const int MaxBankSlots = 400;
    public const int MaxStackSize = int.MaxValue;

    // --- Inventory operations ---

    public bool AddToInventory(GameState state, string itemId, int quantity)
    {
        var existing = state.Inventory.Find(i => i.ItemId == itemId);
        if (existing != null)
        {
            existing.Quantity += quantity;
            return true;
        }

        if (state.Inventory.Count >= MaxInventorySlots)
            return false;

        state.Inventory.Add(new InventoryItem { ItemId = itemId, Quantity = quantity });
        return true;
    }

    public bool RemoveFromInventory(GameState state, string itemId, int quantity)
    {
        var existing = state.Inventory.Find(i => i.ItemId == itemId);
        if (existing == null || existing.Quantity < quantity)
            return false;

        existing.Quantity -= quantity;
        if (existing.Quantity <= 0)
            state.Inventory.Remove(existing);
        return true;
    }

    public int GetInventoryCount(GameState state, string itemId)
    {
        var existing = state.Inventory.Find(i => i.ItemId == itemId);
        return existing?.Quantity ?? 0;
    }

    public bool IsInventoryFull(GameState state)
    {
        return state.Inventory.Count >= MaxInventorySlots;
    }

    // --- Bank operations ---

    public bool DepositToBank(GameState state, string itemId, int quantity)
    {
        if (!RemoveFromInventory(state, itemId, quantity))
            return false;

        var existing = state.Bank.Find(i => i.ItemId == itemId);
        if (existing != null)
        {
            existing.Quantity += quantity;
            return true;
        }

        if (state.Bank.Count >= MaxBankSlots)
        {
            // Refund to inventory since bank is full
            AddToInventory(state, itemId, quantity);
            return false;
        }

        state.Bank.Add(new InventoryItem { ItemId = itemId, Quantity = quantity });
        return true;
    }

    public bool WithdrawFromBank(GameState state, string itemId, int quantity)
    {
        var bankItem = state.Bank.Find(i => i.ItemId == itemId);
        if (bankItem == null || bankItem.Quantity < quantity)
            return false;

        // Check if inventory can hold it
        var invItem = state.Inventory.Find(i => i.ItemId == itemId);
        if (invItem == null && state.Inventory.Count >= MaxInventorySlots)
            return false;

        bankItem.Quantity -= quantity;
        if (bankItem.Quantity <= 0)
            state.Bank.Remove(bankItem);

        AddToInventory(state, itemId, quantity);
        return true;
    }

    public bool DepositAll(GameState state)
    {
        var items = new List<InventoryItem>(state.Inventory);
        bool allDeposited = true;
        foreach (var item in items)
        {
            if (!DepositToBank(state, item.ItemId, item.Quantity))
                allDeposited = false;
        }
        return allDeposited;
    }

    public int GetBankCount(GameState state, string itemId)
    {
        var existing = state.Bank.Find(i => i.ItemId == itemId);
        return existing?.Quantity ?? 0;
    }

    public void SellItem(GameState state, GameDatabase db, string itemId, int quantity)
    {
        var itemDef = db.GetItem(itemId);
        if (itemDef == null) return;

        if (RemoveFromInventory(state, itemId, quantity))
            state.Stats.Gold += (long)itemDef.Value * quantity;
    }
}
