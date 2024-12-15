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
	        	if ((a != fCoreDLL || acceptFCore) && a != gameDLL && a.Location.Contains("254200"))
	                return a;
	        }
	        log("Could not find valid mod assembly in call stack:\n"+string.Join("\n", sf.Select(getMethodLocationInfo).ToArray()), fCoreDLL);
	        return Assembly.GetCallingAssembly();
		}
	    
	    public static string getMethodLocationInfo(StackFrame sf) {
	    	return getMethodLocationInfo(sf.GetMethod())+" in file "+sf.GetFileName()+":"+sf.GetFileLineNumber();
	    }
	    
	    public static string getMethodLocationInfo(MethodBase m) {
	    	return m.Name+" in "+m.DeclaringType+" in DLL "+m.DeclaringType.Assembly.GetName()+" @ "+m.DeclaringType.Assembly.Location;
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
	    		dropItem(x, y, z, ItemEntry.mEntriesByKey[name].ItemID);
	    	}
	    	else {
	    		log("NO SUCH ITEM TO DROP: "+name);
	    	}
		}
		
		public static void dropItem(long x, long y, long z, int id) {
			if (id > 0) {
		    	ItemBase item = ItemManager.SpawnItem(id);
		    	DroppedItemData stack = ItemManager.instance.DropItem(item, x, y, z, Vector3.zero);
	    	}
	    	else {
	    		log("NO SUCH ITEM ID TO DROP: "+id);
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
	    	TerrainDataValueEntry value = entry.GetValue(meta);
	    	return value != null ? value.Name : entry.Name;
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
	    
	    public static string blockToString(Coordinate c, MachineEntity e) {
	    	return blockToString(c, e.AttemptGetSegment);
	    }
	    
	    public static string blockToString(Coordinate c, Func<long, long, long, Segment> segmentGetter) {
	    	return blockToString(c.getSegment(segmentGetter), c.xCoord, c.yCoord, c.zCoord);
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
	    	if (e == null)
	    		return "<NULL>";
	    	return e.GetType().Name+" @ "+new Coordinate(e).ToString()+" in block "+blockToString(e.mSegment, e.mnX, e.mnY, e.mnZ);
	    }
	    
	    public static string machineToStringNoWorld(this SegmentEntity e) {
	    	if (e == null)
	    		return "<NULL>";
	    	return e.GetType().Name+" @ "+new Coordinate(e).ToString()+" mdl="+(e is MachineEntity ? ((MachineEntity)e).mObjectType.ToString() : "none");
	    }
	    
	    public static string terrainDataToString(this TerrainDataEntry e) {
	    	if (e == null)
	    		return "<NULL>";
	    	return e.Key+" '"+e.Name+"', icon="+e.IconName+" mod="+(e.ModEntityHandler == null ? "None" : e.ModEntityHandler.GetType().Name)+" Values=\n"+string.Join("\n", e.Values.Select(s => s.terrainDataValueToString()).ToArray());
	    }
	    
	    public static string terrainDataValueToString(this TerrainDataValueEntry e) {
	    	if (e == null)
	    		return "<NULL>";
	    	return e.Key+":"+e.Value+", '"+e.Name+"', icon="+e.IconName+" mod="+(e.ModEntityHandler == null ? "None" : e.ModEntityHandler.GetType().Name);
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
    
		public static int removeFromInventory(int itemID, int amt, StorageMachineInterface hopper, StorageUserInterface user) {
	    	if (hopper == null)
	    		return 0;
	    	if (hopper is StorageHopper) {
	    		return ((StorageHopper)hopper).RemoveInventoryItem(itemID, amt);
	    	}
	    	else {
	    		ItemBase trash;
	    		return hopper.TryPartialExtractItems(user, itemID, amt, out trash);
	    	}
		}
    
		public static int getCount(int itemID, StorageMachineInterface hopper) {
	    	return hopper == null ? 0 : hopper.CountItems(itemID);
		}
    
		public static int getHoppersItemCount(int itemID, StorageMachineInterface[] hoppers) {
	    	if (hoppers == null || hoppers.Length == 0)
	    		return 0;
			int num = 0;
			for (int i = 0; i < hoppers.Length; i++)
				num += getCount(itemID, hoppers[i]);
			return num;
		}
	    
	    public static int getOreTier(ushort id) {
	    	switch(id) {
	    		case eCubeTypes.OreCoal:
	    		case eCubeTypes.OreTin:
	    		case eCubeTypes.OreCopper:
	    			return 0;
	    		case eCubeTypes.OreIron:
	    		case eCubeTypes.OreLithium:
	    			return 1;
	    		case eCubeTypes.OreNickel:
	    		case eCubeTypes.OreGold:
	    		case eCubeTypes.OreTitanium:
	    			return 2;
	    		case eCubeTypes.OreBioMass:
	    		case eCubeTypes.OreCrystal:
	    			return 3;
	    		case eCubeTypes.OreDiamond_T4_2: //molybdenum
	    		case eCubeTypes.OreEmerald_T4_1: //chromium
	    			return 4;
	    		case eCubeTypes.OreRuby_T5_1: //superhard rock
	    		case eCubeTypes.Magmacite:
	    			return 5;
	    		case eCubeTypes.Uranium_T7: //also used by nuclear reactor mod
	    			return 6;
	    	}
	    	return -1;
	    }
	    
	    public static IEnumerable<TerrainDataEntry> getOres() {
	    	return TerrainData.mEntries.Where(e => e != null && e.Category == MaterialCategories.Ore && e.Key != "EnrichedCoal" && e.Key != "InfusedCoal");
	    }
	    
	    public static IEnumerable<ushort> getOreIDs() {
	    	return getOres().Select(e => e.CubeType);
	    }
	    
	    public static void sortOreList(List<ushort> li) {
	    	li.Sort((t1, t2) => {
	    	    int tier1 = getOreTier(t1);
	    	    int tier2 = getOreTier(t2);
	    	    return tier1 == tier2 ? t1.CompareTo(t2) : tier1.CompareTo(tier2);
	    	});
	    }
	    
	    public static void sortOreList(List<TerrainDataEntry> li) {
	    	li.Sort((t1, t2) => {
	    	    int tier1 = getOreTier(t1.CubeType);
	    	    int tier2 = getOreTier(t2.CubeType);
	    	    return tier1 == tier2 ? t1.CubeType.CompareTo(t2.CubeType) : tier1.CompareTo(tier2);
	    	});
	    }
		
		public static bool isFFDefenceOffline() {
			return CentralPowerHub.Destroyed || !CCCCC.ActiveAndWorking || !WorldUtil.anyCryoExists();
		}
	    
	    public static ushort getPipeOrientation(Vector3 vec) {
	    	int idx = -1;
	    	if (vec.y > 0)
	    		return 2;
	    	else if (vec.x > 0)
	    		idx = 0;
	    	else if (vec.z > 0)
	    		idx = 1;
	    	else if (vec.x < 0)
	    		idx = 2;
	    	else if (vec.z < 0)
	    		idx = 3;
	    	else if (vec.y < 0)
	    		idx = 4;
	    	return EntityManager.pipeOrientations[idx];
	    }
		
	}
}
