using System;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public static class RecipeUtil {
		
		public static CraftData getRecipeByKey(string key, string cat = "Manufacturer") {
			foreach (CraftData recipe in CraftData.GetRecipesForSet(cat)) {
				if (recipe.Key == key) {
					return recipe;
				}
			}
			return null;
		}
		
		public static List<CraftData> getRecipesFor(string output, string cat = "Manufacturer") {
			List<CraftData> li = new List<CraftData>();
			foreach (CraftData recipe in CraftData.GetRecipesForSet(cat)) {
				if (recipe.CraftedKey == output) {
					li.Add(recipe);
				}
			}
			return li;
		}
		
		public static void modifyIngredientCount(this CraftData rec, string item, uint newAmt) {
			foreach (CraftCost ing in rec.Costs) {
				if (ing.Key == item) {
					ing.Amount = newAmt;
					FUtil.log("Changed amount of "+item+" to "+newAmt+" in recipe "+recipeToString(rec, true));
				}
			}
		}
		
		public static CraftCost removeIngredient(this CraftData rec, string item) {
			for (int i = rec.Costs.Count-1; i >= 0; i--) {
				CraftCost ing = rec.Costs[i];
				if (ing.Key == item) {
					rec.Costs.RemoveAt(i);
					FUtil.log("Removed "+item+" from recipe "+recipeToString(rec, true));
					return ing;
				}
			}
			return null;
		}
		
		public static CraftCost addIngredient(this CraftData rec, string item, uint amt) {
			return addIngredient(rec, item, amt, true);
		}
		
		private static CraftCost addIngredient(this CraftData rec, string item, uint amt, bool checkAdd) {
			CraftCost cost = new CraftCost();
			cost.Amount = amt;
			cost.Key = item;
			rec.Costs.Add(cost);
			FUtil.log("Added "+amt+" of "+item+" to recipe "+recipeToString(rec, true));
			if (checkAdd && rec.Costs.Count > 5 && rec.RecipeSet == "Manufacturer")
				FUtil.log("WARNING: RECIPE NOW HAS "+rec.Costs.Count+" INGREDIENTS, BUT THE CRAFTING UI CAN ONLY DISPLAY FIVE!");
			link(rec);
			return cost;
		}
		
		public static CraftCost replaceIngredient(this CraftData rec, string item, string with, float scale = 1) {
			CraftCost rem = removeIngredient(rec, item);
			if (rem == null)
				return null;
			return addIngredient(rec, with, (uint)Mathf.Max(1, rem.Amount*scale), false);
		}
		
		public static CraftCost replaceIngredients(this CraftData rec, string with, uint forcedAmount = 0, params string[] replace) {
			uint use = 0;
			foreach (string s in replace) {
				CraftCost rem = removeIngredient(rec, s);
				if (rem == null)
					continue;
				use = Math.Max(use, rem.Amount);
			}
			if (forcedAmount > 0)
				use = forcedAmount;
			return addIngredient(rec, with, use, false);
		}
    
	    public static CraftCost addItemPerN(this CraftData rec, string add, float perN, uint amtPerN = 1) {
			float num = rec.CraftedAmount*perN;
			if (num-(int)num > 0.1)
				throw new Exception("Recipe '"+rec.Key+"' does not make an integer amount ["+num+"] when multiplied by "+perN);
	    	rec.CraftedAmount = (int)num;
	    	rec.Costs.ForEach(cc => cc.Amount = (uint)(cc.Amount*perN));
	    	rec.CraftTime *= perN;
	    	return rec.addIngredient(add, amtPerN);
	    }
		
		public static void scaleIOExcept(this CraftData rec, float scale, params string[] except) {
			scaleIOExcept(rec, scale, new HashSet<string>(except));
		}
    
		public static void scaleIOExcept(this CraftData rec, float scale, IEnumerable<string> except) {
			float num = rec.CraftedAmount*scale;
			if (num-(int)num > 0.1)
				throw new Exception("Recipe '"+rec.Key+"' does not make an integer amount ["+num+"] when multiplied by "+scale);
	    	rec.CraftedAmount = (int)num;
	    	rec.Costs.ForEach(cc => {if (!except.Contains(cc.Key)) {cc.Amount = (uint)(cc.Amount*scale);}});
	    	rec.CraftTime *= scale;
	    }
		
		public static CraftData createNewRecipe(string id) {
			CraftData ret = new CraftData();
			ret.Key = "ReikaKalseki."+id;
			ret.Costs = new List<CraftCost>();
			ret.ScanRequirements = new List<string>();
			ret.ResearchRequirements = new List<string>();
			ret.ResearchRequirementEntries = new List<ResearchDataEntry>();
			return ret;
		}
		
		public static CraftData addRecipe(string id, string item, string cat, int amt = 1, string set = "Manufacturer", Action<CraftData> init = null) {
			if (cat == "CraftingIngredient")
				cat = "craftingingredient"; //this is stupid but the XMLs are wrong
			bool isManu = set == "Manufacturer";
			if (isManu && !CraftData.mCraftCategoryDic.ContainsKey(cat)) {
				FUtil.log("Recipe '"+id+"' specifying a nonexistent crafting category: '"+cat+"'; categories = {"+string.Join(", ", CraftData.mCraftCategoryDic.Keys.ToArray())+"}/["+string.Join(", ", CraftData.mCraftCategories.Select(s => s.category).ToArray())+"]");
				CraftingCategory addCat = new CraftingCategory(cat, "NoIcon", cat);
				CraftData.mCraftCategories.Add(addCat);
				CraftData.mCraftCategoryDic.Add(cat, addCat);
			}
			CraftData rec = createNewRecipe(id);
			rec.RecipeSet = set;
			rec.Category = cat;
			rec.CraftedKey = item;
			rec.CraftedAmount = amt;
			if (init != null)
				init.Invoke(rec);
			addRecipe(rec);
			if (isManu)
				CraftData.mCraftCategoryDic[cat].recipes.Add(rec);
			return rec;
		}
		
		public static CraftData addRecipe(CraftData rec) {
			bool isManu = rec.RecipeSet == "Manufacturer";
			if (string.IsNullOrEmpty(rec.Key)) {
				FUtil.log("Invalid recipe missing key and cannot be crafted: "+recipeToString(rec));
				return rec;
			}
			if (string.IsNullOrEmpty(rec.Category) && isManu) {
				FUtil.log("Invalid recipe missing category and cannot be crafted: "+recipeToString(rec));
				return rec;
			}
			if (string.IsNullOrEmpty(rec.CraftedKey)) {
				FUtil.log("Invalid recipe missing output and cannot be crafted: "+recipeToString(rec));
				return rec;
			}
			if (string.IsNullOrEmpty(rec.RecipeSet)) {
				FUtil.log("Invalid recipe missing set and cannot be crafted: "+recipeToString(rec));
				return rec;
			}
			try {
				CraftData.mRecipesForSet[rec.RecipeSet].Add(rec);
				if (isManu)
					CraftData.maCraftData.Add(rec);
				link(rec);
				FUtil.log("Added new recipe "+recipeToString(rec, true, true));
			}
			catch (Exception e) {
				FUtil.log("Invalid recipe cannot be crafted: "+recipeToString(rec)+" -> "+e.ToString());
			}
			return rec;
		}
		
		public static void addUncrafting(string recipe, string name, string refundItem = null) {
	    	CraftData orig = RecipeUtil.getRecipeByKey(recipe);
	    	CraftCost refund = refundItem == null ? orig.Costs[0] : orig.Costs.First(ing => ing.Key == refundItem);
	    	RecipeUtil.addRecipe("Uncraft_"+recipe, refund.Key, orig.Category, (int)refund.Amount, init: rr => {
		       	rr.Tier = orig.Tier;
		        rr.CanCraftAnywhere = true;
		        rr.Description = name;
		        RecipeUtil.addIngredient(rr, orig.CraftedKey, (uint)orig.CraftedAmount);
		        rr.ScanRequirements.AddRange(orig.ScanRequirements);
		        foreach (string s in orig.ResearchRequirements)
		        	RecipeUtil.addResearch(rr, s);
		        rr.ResearchCost = 0;
			});
		}
		
		public static CraftData copyRecipe(CraftData template) {
			string xml = XMLParser.SerializeObject(template, typeof(CraftData));
			CraftData ret = (CraftData)XMLParser.DeserializeObject(xml, typeof(CraftData));
			ret.RecipeSet = template.RecipeSet;
			ret.Category = template.Category;
			return ret;
		}
		
		private static void link(CraftData rec) {
			try {
				CraftData.LinkEntries(new List<CraftData>(new CraftData[]{rec}), rec.RecipeSet);
			}
			catch (Exception e) {
				FUtil.log("Threw error attempting to 'link' recipe: "+e.ToString());
			}
		}
		
		public static void removeResearch(this CraftData rec, string key) {
			rec.ResearchRequirements.Remove(key);
        	ResearchDataEntry e = ResearchDataEntry.GetResearchDataEntry(key);
        	if (e != null) {
        		rec.ResearchRequirementEntries.Remove(e);
        		FUtil.log("Removed research '"+key+"' from recipe "+recipeToString(rec, false, true));
        	}
		}
		
		public static void addResearch(this CraftData rec, string key) {
			rec.ResearchRequirements.Add(key);
        	ResearchDataEntry e = ResearchDataEntry.GetResearchDataEntry(key);
        	if (e != null) {
        		rec.ResearchRequirementEntries.Add(e);
				FUtil.log("Added research '"+key+"' to recipe "+recipeToString(rec, false, true));
        	}
		}
		
		public static string ingredientToString(this CraftCost ing) {
			return ing.Key+" x "+ing.Amount+" ("+ing.Name+") ["+ing.CubeType+"/"+ing.CubeValue+"]/#"+ing.ItemType;
		}
		
		public static string recipeToString(this CraftData rec, bool fullIngredients = false, bool fullResearch = false) {
			string ret = "'"+rec.RecipeSet+"/"+rec.Category+"/"+rec.CanCraftAnywhere+"::"+rec.Key+"'="+rec.CraftedKey+"x"+rec.CraftedAmount+" from ";
			if (fullIngredients) {
				List<string> li = new List<string>();
				rec.Costs.ForEach(c => li.Add(ingredientToString(c)));
				ret += "I"+li.Count+"["+string.Join(", ", li.ToArray())+"]";
			}
			else {
				ret += rec.Costs.Count+" items";
			}
			ret += " & ";
			if (fullResearch) {
				ret += "T"+rec.ResearchRequirements.Count+"["+string.Join(", ", rec.ResearchRequirements.ToArray())+"]";
			}
			else {
				ret += rec.ResearchRequirements.Count+" techs";
			}
			return ret;
		}
		
		public static ProjectItemRequirement removeIngredient(this ResearchDataEntry rec, string item) {
			for (int i = rec.ProjectItemRequirements.Count-1; i >= 0; i--) {
				ProjectItemRequirement ing = rec.ProjectItemRequirements[i];
				if (ing.Key == item) {
					rec.ProjectItemRequirements.RemoveAt(i);
					FUtil.log("Removed "+item+" from research "+rec.Key);
					return ing;
				}
			}
			return null;
		}
		
		public static ProjectItemRequirement addIngredient(this ResearchDataEntry rec, string item, int amt) {
			ProjectItemRequirement cost = new ProjectItemRequirement();
			cost.Amount = amt;
			cost.Key = item;
			if (ItemEntry.mEntriesByKey.ContainsKey(item))
				cost.ItemID = ItemEntry.mEntriesByKey[item].ItemID;
			if (TerrainData.mEntriesByKey.ContainsKey(item))
				cost.CubeType = TerrainData.mEntriesByKey[item].CubeType;
			rec.ProjectItemRequirements.Add(cost);
			FUtil.log("Added "+amt+" of "+item+" to research "+rec.Key);
			return cost;
		}
		
		public static ProjectItemRequirement replaceIngredient(this ResearchDataEntry rec, string item, string with, float scale = 1) {
			ProjectItemRequirement rem = removeIngredient(rec, item);
			if (rem == null)
				return null;
			return addIngredient(rec, with, (int)Mathf.Max(1, rem.Amount*scale));
		}
		
	}
}
