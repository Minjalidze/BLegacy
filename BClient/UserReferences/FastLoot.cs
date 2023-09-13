using System;
using System.Linq;
using UnityEngine;

namespace BClient.UserReferences;

public class FastLoot : MonoBehaviour
{
    internal static void OnSlotClick(int slot, bool myInv)
    {
        try
        {
            var component = PlayerClient.GetLocalPlayer().controllable.character.GetComponent<Inventory>();
			var localLoot = FastLoot.GetLocalLoot();
			var netEntityID = NetEntityID.Get(component);
			if (localLoot == null)
			{
				if (component.GetItem(slot, out var inventoryItem) && Helper.IsArmor(inventoryItem, out var num))
				{
					if (slot == num)
					{
						if (FastLoot.GetVacantSlot(component) == -1)
						{
							return;
						}
						FastLoot.DoMove(component, netEntityID, slot, FastLoot.GetVacantSlot(component));
						return;
					}
					else
					{
						if (component.IsSlotFree(num))
						{
							FastLoot.DoMove(component, netEntityID, slot, num);
							return;
						}

						if (component.GetItem(num, out var equipedArmor))
						{
							if (Helper.IsBetter(inventoryItem, equipedArmor))
							{
								if (FastLoot.GetVacantSlot(component) == -1)
								{
									return;
								}
								FastLoot.DoMove(component, netEntityID, num, FastLoot.GetVacantSlot(component));
								FastLoot.DoMove(component, netEntityID, slot, num);
								return;
							}
							else
							{
								if (FastLoot.GetVacantSlot(component) == -1)
								{
									return;
								}
								FastLoot.DoMove(component, netEntityID, slot, FastLoot.GetVacantSlot(component));
								return;
							}
						}
					}
				}
			}
			else
			{
				var inventory = localLoot._inventory;
				var netEntityID2 = NetEntityID.Get(inventory);
				if (myInv)
				{
					if (FastLoot.GetVacantSlot(inventory) == -1)
					{
						return;
					}

					if (component.GetItem(slot, out var inventoryItem2) && Helper.IsStackable(inventoryItem2, component))
					{
						if (inventoryItem2.datablock._maxUses % Helper.GetItemCount(inventoryItem2.datablock, component) == 0)
						{
							FastLoot.DoMove(component, netEntityID2, slot, FastLoot.GetVacantSlot(inventory));
							return;
						}
						for (var i = 0; i < inventory.slotCount; i++)
						{
							if (inventory.IsSlotOccupied(i) && inventory.GetItem(i, out var inventoryItem3) && inventoryItem2.datablock == inventoryItem3.datablock)
							{
								component.ItemMergePredicted(netEntityID2, slot, i);
								if (Helper.GetItemCount(inventoryItem2.datablock, inventory) == -1)
								{
									break;
								}
							}
						}
					}
					FastLoot.DoMove(component, netEntityID2, slot, FastLoot.GetVacantSlot(inventory));
					return;
				}
				else if (inventory.GetItem(slot, out var inventoryItem4))
				{
					if (Helper.IsArmor(inventoryItem4, out var num2))
					{
						if (component.IsSlotFree(num2))
						{
							FastLoot.DoMove(inventory, netEntityID, slot, num2);
							return;
						}

						if (component.GetItem(num2, out var equipedArmor2))
						{
							if (Helper.IsBetter(inventoryItem4, equipedArmor2))
							{
								if (FastLoot.GetVacantSlot(component) == -1)
								{
									return;
								}
								FastLoot.DoMove(component, netEntityID, num2, FastLoot.GetVacantSlot(component));
								FastLoot.DoMove(inventory, netEntityID, slot, num2);
								return;
							}
							else
							{
								if (FastLoot.GetVacantSlot(component) == -1)
								{
									return;
								}
								FastLoot.DoMove(inventory, netEntityID, slot, FastLoot.GetVacantSlot(component));
								return;
							}
						}
					}
					else
					{
						if (FastLoot.GetVacantSlot(component) == -1)
						{
							return;
						}
						if (Helper.IsStackable(inventoryItem4, inventory))
						{
							if (inventoryItem4.datablock._maxUses % Helper.GetItemCount(inventoryItem4.datablock, inventory) == 0)
							{
								FastLoot.DoMove(inventory, netEntityID, slot, FastLoot.GetVacantSlot(component));
								return;
							}
							for (var j = 0; j < component.slotCount; j++)
							{
								if (component.IsSlotOccupied(j) && component.GetItem(j, out var inventoryItem5) && inventoryItem4.datablock == inventoryItem5.datablock)
								{
									inventory.ItemMergePredicted(netEntityID, slot, j);
									if (Helper.GetItemCount(inventoryItem4.datablock, component) == -1)
									{
										break;
									}
								}
							}
						}
						FastLoot.DoMove(inventory, netEntityID, slot, FastLoot.GetVacantSlot(component));
					}
				}
			}
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static int GetEmptyBeltSlot(Inventory inv)
    {
        for (var i = 30; i < inv.slotCount; i++)
            if (inv.IsSlotFree(i))
                return i;
        return -1;
    }

    private static int GetVacantSlot(Inventory inv)
    {
        var result = -1;
        for (var i = inv.slotCount - 1; i >= 0; i--)
            if (inv.IsSlotVacant(i))
                result = i;
        return result;
    }

    private static Inventory.SlotOperationResult DoMove(Inventory inv, NetEntityID entityID, int invSlot,
        int entitySlot)
    {
        return inv.ItemMovePredicted(entityID, invSlot, entitySlot);
    }

    private static LootableObject GetLocalLoot()
    {
        return FindObjectsOfType<LootableObject>().ToList().Find(f => f != null && f.IsLocalLooting()) ?? null;
    }
}