using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RustExtended;

namespace Oxide.Plugins
{
	[Info("RLootSaver", "Romanchik34", "1.0.0")]
	class RLootSaver : RustLegacyPlugin
	{
		void OnKilled(TakeDamage damage, DamageEvent evt)
		{
			if (!(damage is HumanBodyTakeDamage) && !(damage is StructureComponentTakeDamage) && !(damage is ProtectionTakeDamage))
			{
				try
				{
					if (evt.attacker.client == null || evt.victim.client == null || evt.victim.character == null || evt.victim.idMain == null) return;

					var lootable = evt.victim.idMain.GetComponent<LootableObject>();
					if (lootable == null) return;

					var inventory = lootable._inventory;
					if (inventory.occupiedSlotCount > 0)
					{
						for (int i = 0; i < inventory.slotCount; i++)
						{
							IInventoryItem item;
							if (inventory.GetItem(i, out item))
							{
								DropItemFromLootable(evt.attacker.character, evt.victim.idMain.transform.position, item);
							}
						}
					}
				}
				catch (Exception ex)
				{
					Puts(ex.ToString());
				}
			}
		}

		private void DropItemFromLootable(Character attacker, Vector3 position, IInventoryItem item)
		{
			Vector3 forward = attacker.eyesAngles.forward;
			Vector3 arg = forward * UnityEngine.Random.Range(4f, 6f);
			Quaternion rotation = Quaternion.LookRotation(Vector3.forward);
			GameObject gameObject = global::NetCull.InstantiateDynamicWithArgs<Vector3>("GenericItemPickup", position, rotation, arg);
			ItemPickup dropItem = gameObject.GetComponent<ItemPickup>();
			dropItem.SetPickupItem(item);
		}
	}
}
