using System.Collections.Generic;
using UnityEngine;
using RustExtended;
using Oxide.Core.Plugins;
using Oxide.Plugins;

namespace Oxide.Plugins
{
    [Info("ItemMoveExt", "Sh1ne", "1.1.0")]
    class ItemMoveExt : RustLegacyPlugin
    {
        bool SlotFlagsIsEqual(Inventory.SlotFlags invSlotFlag, Inventory.SlotFlags itemSlotFlag, Inventory.SlotFlags slotType)
        {
            if ((invSlotFlag & slotType) == slotType)
                if ((itemSlotFlag & slotType) != slotType)
                    return false;
            return true;
        }

        [HookMethod("OnItemMoved")]
        object OnItemMoved(PlayerClient player, Inventory fromInventory, int fromSlot, Inventory moveInventory, int moveSlot, Inventory.SlotOperationsInfo info)
        {
            IInventoryItem firstItem;
            fromInventory.GetItem(fromSlot, out firstItem);

            IInventoryItem secondItem;
            moveInventory.GetItem(moveSlot, out secondItem);

            if (firstItem != null && secondItem != null)
            {
                if (firstItem.datablock is ItemModDataBlock)
                    return null;

                if (firstItem.datablock is ResearchToolDataBlock)
                    return null;

                if (firstItem.datablock.uniqueID == secondItem.datablock.uniqueID)
                    return null;

                // Check all for armor slots

                if (!SlotFlagsIsEqual(fromInventory.GetSlotFlags(fromSlot), secondItem.datablock._itemFlags, Inventory.SlotFlags.Head)
                 || !SlotFlagsIsEqual(fromInventory.GetSlotFlags(fromSlot), secondItem.datablock._itemFlags, Inventory.SlotFlags.Chest)
                 || !SlotFlagsIsEqual(fromInventory.GetSlotFlags(fromSlot), secondItem.datablock._itemFlags, Inventory.SlotFlags.Legs)
                 || !SlotFlagsIsEqual(fromInventory.GetSlotFlags(fromSlot), secondItem.datablock._itemFlags, Inventory.SlotFlags.Feet))
                    return false;

                if (!SlotFlagsIsEqual(moveInventory.GetSlotFlags(moveSlot), firstItem.datablock._itemFlags, Inventory.SlotFlags.Head)
                 || !SlotFlagsIsEqual(moveInventory.GetSlotFlags(moveSlot), firstItem.datablock._itemFlags, Inventory.SlotFlags.Chest)
                 || !SlotFlagsIsEqual(moveInventory.GetSlotFlags(moveSlot), firstItem.datablock._itemFlags, Inventory.SlotFlags.Legs)
                 || !SlotFlagsIsEqual(moveInventory.GetSlotFlags(moveSlot), firstItem.datablock._itemFlags, Inventory.SlotFlags.Feet))
                    return false;

                // Check all for Camp Fire

                if ((fromInventory.GetSlotFlags(fromSlot) & Inventory.SlotFlags.Cooked) == Inventory.SlotFlags.Cooked
                 || (moveInventory.GetSlotFlags(moveSlot) & Inventory.SlotFlags.Cooked) == Inventory.SlotFlags.Cooked)
                    return false;

                if ((fromInventory.GetSlotFlags(fromSlot) & Inventory.SlotFlags.Debris) == Inventory.SlotFlags.Debris
                 || (moveInventory.GetSlotFlags(moveSlot) & Inventory.SlotFlags.Debris) == Inventory.SlotFlags.Debris)
                    return false;

                if (!SlotFlagsIsEqual(fromInventory.GetSlotFlags(fromSlot), secondItem.datablock._itemFlags, Inventory.SlotFlags.FuelBasic)
                 || !SlotFlagsIsEqual(fromInventory.GetSlotFlags(fromSlot), secondItem.datablock._itemFlags, Inventory.SlotFlags.Raw))
                    return false;

                if (!SlotFlagsIsEqual(moveInventory.GetSlotFlags(moveSlot), firstItem.datablock._itemFlags, Inventory.SlotFlags.FuelBasic)
                 || !SlotFlagsIsEqual(moveInventory.GetSlotFlags(moveSlot), firstItem.datablock._itemFlags, Inventory.SlotFlags.Raw))
                    return false;

                fromInventory.RemoveItem(fromSlot);
                moveInventory.RemoveItem(moveSlot);

                IInventoryItem newFirstItem = moveInventory.AddItem(firstItem.datablock, moveSlot, firstItem.uses);
                IInventoryItem newSecondItem = fromInventory.AddItem(secondItem.datablock, fromSlot, secondItem.uses);

                newFirstItem.SetCondition(firstItem.condition);
                newFirstItem.SetUses(firstItem.uses);

                if (firstItem.datablock is WeaponDataBlock)
                {
                    var heldItem = firstItem as IHeldItem;
                    var newHeldItem = newFirstItem as IHeldItem;

                    newHeldItem.SetTotalModSlotCount(heldItem.totalModSlots);
                    foreach (var mod in heldItem.itemMods)
                        newHeldItem.AddMod(mod);
                }

                newSecondItem.SetCondition(secondItem.condition);
                newSecondItem.SetUses(secondItem.uses);

                if (secondItem.datablock is WeaponDataBlock)
                {
                    var heldItem = secondItem as IHeldItem;
                    var newHeldItem = newSecondItem as IHeldItem;

                    newHeldItem.SetTotalModSlotCount(heldItem.totalModSlots);
                    foreach (var mod in heldItem.itemMods)
                        newHeldItem.AddMod(mod);
                }

                return true;
            }
            return null;
        }
    }
}