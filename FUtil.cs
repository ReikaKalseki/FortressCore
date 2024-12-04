using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using StackFrame = System.Diagnostics.StackFrame;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public static class FUtil {
		
		public static readonly Assembly fCoreDLL = Assembly.GetAssembly(typeof(FUtil));
	    public static readonly Assembly gameDLL = Assembly.GetAssembly(typeof(MachineEntity));
	    
	    public static string getTime() {
	    	return DateTime.Now.ToString("dd-MM-yyyy @ HH:mm:ss.FFF");
	    }
		
		internal static Assembly tryGetModDLL(bool acceptFCore = false) {
			StackFrame[] sf = new System.Diagnostics.StackTrace().GetFrames();
	        if (sf == null || sf.Length == 0)
	        	return Assembly.GetCallingAssembly();
	        foreach (StackFrame f in sf) {
	        	Assembly a = f.GetMethod().DeclaringType.Assembly;
	        	if ((a != fCoreDLL || acceptFCore) && a != gameDLL && a.Location.Contains("Mods"))
	                return a;
	        }
	        log("Could not find valid mod assembly: "+string.Join("\n", sf.Select<StackFrame, string>(s => s.GetMethod()+" in "+s.GetMethod().DeclaringType).ToArray()), fCoreDLL);
	        return Assembly.GetCallingAssembly();
		}
	    
		public static void log(string s, Assembly a = null, int indent = 0) {
			while (s.Length > 4096) {
				string part = s.Substring(0, 4096);
				log(part, a);
				s = s.Substring(4096);
			}
			string id = (a != null ? a : tryGetModDLL()).GetName().Name.ToUpperInvariant().Replace("PLUGIN_", "");
			if (indent > 0) {
				s = s.PadLeft(s.Length+indent, ' ');
			}
			Debug.Log(id+": "+s);
		}
		
		public static void dropItem(long x, long y, long z, ushort block, ushort metadata = 0) {
	    	ItemBase item = ItemManager.SpawnCubeStack(block, metadata, 1);
		    DroppedItemData stack = ItemManager.instance.DropItem(item, x, y, z, Vector3.zero);
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
	    
		public static string getFullHierarchyPath(this Transform current) { //just like in SN
			if (current.parent == null)
				return "Root:" + current.name;
			return current.parent.getFullHierarchyPath() + "/" + current.name;
		}
	    
	    public static string getFullHierarchyPath(this Component component) {
			return component.transform.getFullHierarchyPath() + "/" + component.GetType().ToString();
		}
	    
	    public static string getFullHierarchyPath(this GameObject go) {
	    	return getFullHierarchyPath(go.transform);
		}
	    
	    public static GameObject getRoot(this GameObject go) {
	    	Transform t = go.transform;
	    	while (t.parent != null)
	    		t = t.parent;
	    	return t.gameObject;
		}
	    
	    public static void dumpObjectData(this GameObject go) {
	    	go.dumpObjectData(0);
		}
	    
		private static void dumpObjectData(this GameObject go, int indent) {
			if (!go) {
				log("null object");
				return;
			}
			log("object "+go, fCoreDLL, indent);
			log("chain "+go.getFullHierarchyPath(), fCoreDLL, indent);
			log("components: "+string.Join(", ", go.GetComponents<Component>().Select(c => c.name+"["+c.GetType().Name+"]").ToArray()), fCoreDLL, indent);
			log("transform: "+go.transform, fCoreDLL, indent);
			if (go.transform != null) {
				log("position: "+go.transform.position, fCoreDLL, indent);
				log("transform object: "+go.transform.gameObject, fCoreDLL, indent);
				for (int i = 0; i < go.transform.childCount; i++) {
					log("child object #"+i+": ", fCoreDLL, indent);
					dumpObjectData(go.transform.GetChild(i).gameObject, indent+3);
				}
			}
		}
	    
	    public static string getBlockName(ushort id, ushort meta) {
	    	if (id > TerrainData.mEntries.Length || TerrainData.mEntries[id] == null)
	    		return "Unknown Block ID #"+id;
	    	TerrainDataEntry entry = TerrainData.mEntries[id];
	    	return entry.Values.Count > meta && entry.Values[meta] != null ? entry.Values[meta].Name : entry.Name;
	    }
	    
	    public static string getItemName(int id) {
	    	return ItemEntry.mEntriesById.ContainsKey(id) ? ItemEntry.mEntriesById[id].Name : "Unknown Item ID #"+id;
	    }
	    
	    public static uint getOrePerBar(bool basicSmelter = false) {
			int amt = (int)Mathf.Ceil(16*DifficultySettings.mrResourcesFactor);
			if (basicSmelter && !DifficultySettings.mbCasualResource)
				amt *= 4;
			return (uint)Math.Max(1, amt);
	    }
	    
	    public static void setMachineModel(MachineEntity e, SpawnableObjectEnum mdl) {
    		e.mObjectType = mdl;
			e.mWrapper = SpawnableObjectManagerScript.instance.SpawnObject(eGameObjectWrapperType.Entity, e.mObjectType, e.mnX, e.mnY, e.mnZ, e.mFlags, e);
	    }
	    
	    public static string blockToString(Segment s, long x, long y, long z) {
	    	int dx = (int)(x%16);
	    	int dy = (int)(y%16);
	    	int dz = (int)(z%16);
	    	ushort id;
	    	CubeData data;
	    	s.GetCubeDataNoChecking(dx, dy, dz, out id, out data);
	    	return "ID/value ["+id+"/"+data.mValue+"] = "+getBlockName(id, data.mValue);
	    }
	    
	    public static string machineToString(this SegmentEntity e) {
	    	return e.GetType().Name+" @ "+new Coordinate(e).ToString()+" in block "+blockToString(e.mSegment, e.mnX, e.mnY, e.mnZ);
	    }
	    
	    public static string terrainDataValueToString(this TerrainDataValueEntry e) {
	    	return e.Key+":"+e.Value+", '"+e.Name+"', icon="+e.IconName;
	    }
	    
	    public static MultiblockData registerMultiblock(ModRegistrationData data, string name, MultiblockData.VanillaMultiblockPrefab pfb) {
	    	return registerMultiblock(data, name, null, pfb);
	    }
	    
	    public static MultiblockData registerMultiblock(ModRegistrationData data, string name, Dimensions dd) {
	    	return registerMultiblock(data, name, dd, null);
	    }
	    
	    private static MultiblockData registerMultiblock(ModRegistrationData data, string name, Dimensions dd, MultiblockData.VanillaMultiblockPrefab pfb) {
	    	try {
				data.RegisterEntityHandler("ReikaKalseki."+name);
				data.RegisterEntityHandler("ReikaKalseki."+name+"Placement");
				data.RegisterEntityHandler("ReikaKalseki."+name+"Block");
				data.RegisterEntityHandler("ReikaKalseki."+name+"Center");
				data.RegisterEntityHandler("ReikaKalseki."+name+"CenterFlip");
				
				TerrainDataEntry terrainDataEntry;
				TerrainDataValueEntry terrainDataValueEntry;
				TerrainData.GetCubeByKey("ReikaKalseki."+name, out terrainDataEntry, out terrainDataValueEntry);
				
				ushort placerMeta = ModManager.mModMappings.CubesByKey["MachinePlacement"].ValuesByKey["ReikaKalseki."+name+"Placement"].Value;
				ushort bodyMeta = ModManager.mModMappings.CubesByKey["ReikaKalseki."+name].ValuesByKey["ReikaKalseki."+name+"Block"].Value;
				ushort centerMeta = ModManager.mModMappings.CubesByKey["ReikaKalseki."+name].ValuesByKey["ReikaKalseki."+name+"Center"].Value;
				ushort centerFlipMeta = pfb == null || !pfb.size.isAsymmetric ? centerMeta : ModManager.mModMappings.CubesByKey["ReikaKalseki."+name].ValuesByKey["ReikaKalseki."+name+"CenterFlip"].Value;
				
				FUtil.log("Registered multiblock "+name+" with ID "+terrainDataEntry.CubeType+" and values "+bodyMeta+"/"+centerMeta+"/"+centerFlipMeta+", placer value = "+placerMeta);
				
				if (pfb != null)
					return new MultiblockData(terrainDataEntry.CubeType, placerMeta, bodyMeta, centerMeta, centerFlipMeta, pfb);
				else
					return new MultiblockData(terrainDataEntry.CubeType, placerMeta, bodyMeta, centerMeta, centerFlipMeta, dd);
	    	}
	    	catch (NullReferenceException ex) {
	    		throw new Exception("Could not register multiblock '"+name+"', due to a missing terrain entry; is the XML data present and complete?", ex);
	    	}
	    }
		
	}
}
