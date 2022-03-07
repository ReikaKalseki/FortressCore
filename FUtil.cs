using System;
using System.Reflection;

using System.Collections.Generic;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public static class FUtil {
		
		public static void log(string s) {
			Debug.Log(Assembly.GetExecutingAssembly().GetName().Name.ToUpperInvariant().Replace("PLUGIN_", "")+": "+s);
		}
		
		public static void dropItem(long x, long y, long z, string name) {
			if (ItemEntry.mEntriesByKey.ContainsKey(name)) {
		    	ItemBase item = ItemManager.SpawnItem(ItemEntry.mEntriesByKey[name].ItemID);
		    	DroppedItemData stack = ItemManager.instance.DropItem(item, x, y, z, Vector3.zero);
	    	}
	    	else {
	    		log("NO SUCH ITEM TO DROP: "+name);
	    	}
		}
		
		public static int removeFromPlayer(PlayerInventory inv, int lnItemId, int amt) {
			return removeFromPlayer(inv, (item) => item.mnItemID == lnItemId, amt);
		}
		
		public static int removeFromPlayer(PlayerInventory inv, ushort cube, ushort value, int amt) {
			return removeFromPlayer(inv, (item) => item.mType == ItemType.ItemCubeStack && ((ItemCubeStack)item).mCubeType == cube && ((ItemCubeStack)item).mCubeValue == value, amt);
		}
		
		public static int removeFromPlayer(PlayerInventory inv, Func<ItemBase, bool> match, int amt) {
			int found = 0;
			for (int x = 0; x < PlayerInventory.mnInventoryX && amt > 0; x++)
			{
				for (int y = 0; y < PlayerInventory.mnInventoryY && amt > 0; y++)
				{
					ItemBase item = inv.maItemInventory[x, y];
					if (item == null)
						continue;
					if (match(item))
					{
						if (item.mType == ItemType.ItemStack)
						{
							//reduce
							ItemStack itemStack = item as ItemStack;
							if (itemStack.mnAmount >= amt)//more than enough
							{
								itemStack.mnAmount -= amt;
								found += amt;
								amt = 0;
								if (itemStack.mnAmount == 0)
									inv.maItemInventory[x, y] = null;
							}
							else
							{
								if (itemStack.mnAmount > 0)
								{
									int rem = Math.Min(amt, itemStack.mnAmount);
									amt -= rem;
									itemStack.mnAmount -= rem;
									found += rem;
									
									if (itemStack.mnAmount <= 0)
										inv.maItemInventory[x, y] = null;
								}
							}
						}
						else //ItemSingle
						{
							inv.maItemInventory[x, y] = null;
							amt--;
							found++;
						}
					}
				}
			}
			return found;
		}
		
	}
}
