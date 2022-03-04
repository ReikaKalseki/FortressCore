using System;

using System.Collections.Generic;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public static class FUtil {
		
		public static void log(string s) {
			Debug.Log("FORTRESSTWEAKS: "+s);
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
		
	}
}
