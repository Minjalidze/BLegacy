using System.Collections.Generic;

namespace BClient.UserReferences;

public class Helper
{
	public static bool IsArmor(IInventoryItem inventoryItem, out int slot)
	{
		if (inventoryItem.datablock.name.Contains("Helmet"))
		{
			slot = 36;
			return true;
		}

		if (inventoryItem.datablock.name.Contains("Vest"))
		{
			slot = 37;
			return true;
		}

		if (inventoryItem.datablock.name.Contains("Pants"))
		{
			slot = 38;
			return true;
		}

		if (inventoryItem.datablock.name.Contains("Boots"))
		{
			slot = 39;
			return true;
		}

		slot = 0;
		return false;
	}

	public static bool IsBetter(IInventoryItem clickedArmor, IInventoryItem equipedArmor)
	{
		string key;
		if (clickedArmor.datablock.name.StartsWith("Cloth"))
		{
			key = "Cloth";
		}
		else if (clickedArmor.datablock.name.StartsWith("Leather"))
		{
			key = "Leather";
		}
		else if (clickedArmor.datablock.name.StartsWith("Rad"))
		{
			key = "Rad";
		}
		else
		{
			key = "Kevlar";
		}

		string key2;
		if (equipedArmor.datablock.name.StartsWith("Cloth"))
		{
			key2 = "Cloth";
		}
		else if (equipedArmor.datablock.name.StartsWith("Leather"))
		{
			key2 = "Leather";
		}
		else if (equipedArmor.datablock.name.StartsWith("Rad"))
		{
			key2 = "Rad";
		}
		else
		{
			key2 = "Kevlar";
		}

		return ArmorTypes[key] > ArmorTypes[key2];
	}

	public static int GetItemCount(ItemDataBlock dataBlock, Inventory inv)
	{
		var num = -1;
		for (var i = 0; i < inv.slotCount; i++)
		{
			if (inv.GetItem(i, out var inventoryItem) && inv.IsSlotOccupied(i) && inventoryItem.datablock == dataBlock)
			{
				num += inventoryItem.uses;
			}
		}

		return num;
	}

	public static bool IsStackable(IInventoryItem item, Inventory inv)
	{
		return GetItemCount(item.datablock, inv) != -1 && item.datablock.IsSplittable() &&
		       item.datablock.name != "Torch";
	}

	private static readonly Dictionary<string, int> ArmorTypes = new()
	{
		{
			"Cloth",
			0
		},
		{
			"Rad",
			1
		},
		{
			"Leather",
			2
		},
		{
			"Kevlar",
			3
		}
	};
}