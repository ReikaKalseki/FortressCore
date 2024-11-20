using System;

using System.Collections.Generic;
using System.Collections;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public static class SuitUtil {
		
		private static readonly Dictionary<string, SuitItemCheck> suitChecks = new Dictionary<string, SuitItemCheck>();
		
	    public static bool isSuitItemPresent(Player ep, string id, bool includeMain = true) {
			if (!suitChecks.ContainsKey(id))
				suitChecks[id] = new SuitItemCheck(id);
			if (suitChecks[id].includeMainInventory != includeMain || suitChecks[id].player != ep || suitChecks[id].checkAge())
				suitChecks[id].invalid = true;
			suitChecks[id].includeMainInventory = includeMain;
			suitChecks[id].player = ep;
			return suitChecks[id].value;
	    }
		
		class SuitItemCheck {
			
			public readonly string itemID;
			
			internal Player player;
			internal bool includeMainInventory;
			internal bool invalid;
			
			private float cacheTime;
			private bool cachedValue;
			
			internal bool value {
				get {
					if (invalid)
						doCheck();
					return cachedValue;
				}
			}
			
			internal SuitItemCheck(string id) {
				itemID = id;
				invalid = true;
			}
			
			internal bool checkAge() {
				return Time.time-cacheTime >= 1;
			}
			
			private void doCheck() {
				PlayerInventory inv = player.mInventory;
		    	int id = ItemEntry.GetIDFromKey(itemID, true);
		    	cachedValue = id > 0 && (includeMainInventory ? inv.GetSuitAndInventoryItemCount(id) : inv.GetSuitItemCount(id)) > 0;
				cacheTime = Time.time;
				invalid = false;
			}
			
		}
		
	}
}
