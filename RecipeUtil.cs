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
		
		public static void modifyIngredientCount(CraftData rec, string item, uint newAmt) {
			foreach (CraftCost ing in rec.Costs) {
				if (ing.Key == item) {
					ing.Amount = newAmt;
					FUtil.log("Changed amount of "+item+" to "+newAmt+" in recipe "+recipeToString(rec, true));
				}
			}
		}
		
		public static CraftCost removeIngredient(CraftData rec, string item) {
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
		
		public static CraftCost addIngredient(CraftData rec, string item, uint amt) {
			CraftCost cost = new CraftCost();
			cost.Amount = amt;
			cost.Key = item;
			rec.Costs.Add(cost);
			FUtil.log("Added "+amt+" of "+item+" to recipe "+recipeToString(rec, true));
			link(rec);
			return cost;
		}
		
		private static CraftData createNewRecipe() {
			CraftData ret = new CraftData();
			ret.Costs = new List<CraftCost>();
			ret.ScanRequirements = new List<string>();
			ret.ResearchRequirements = new List<string>();
			ret.ResearchRequirementEntries = new List<ResearchDataEntry>();
			return ret;
		}
		
		public static void addRecipe(string id, string item, string cat, int amt = 1, string set = "Manufacturer", Action<CraftData> init = null) {
			if (cat == "CraftingIngredient")
				cat = "craftingingredient"; //this is stupid but the XMLs are wrong
			if (!CraftData.mCraftCategoryDic.ContainsKey(cat)) {
				FUtil.log("Recipe '"+id+"' specifying a nonexistent crafting category: '"+cat+"'; categories = {"+string.Join(", ", CraftData.mCraftCategoryDic.Keys.ToArray())+"}/["+string.Join(", ", CraftData.mCraftCategories.Select(s => s.category).ToArray())+"]");
				CraftingCategory addCat = new CraftingCategory(cat, "NoIcon", cat);
				CraftData.mCraftCategories.Add(addCat);
				CraftData.mCraftCategoryDic.Add(cat, addCat);
			}
			CraftData rec = createNewRecipe();
			rec.RecipeSet = set;
			rec.Category = cat;
			rec.Key = "ReikaKalseki."+id;
			rec.CraftedKey = item;
			rec.CraftedAmount = amt;
			if (init != null)
				init.Invoke(rec);
			addRecipe(rec);
			CraftData.mCraftCategoryDic[cat].recipes.Add(rec);
		}
		
		public static CraftData addRecipe(CraftData rec) {
			if (string.IsNullOrEmpty(rec.Key)) {
				FUtil.log("Invalid recipe missing key and cannot be crafted: "+recipeToString(rec));
				return rec;
			}
			if (string.IsNullOrEmpty(rec.Category)) {
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
		
		public static void removeResearch(CraftData rec, string key) {
			rec.ResearchRequirements.Remove(key);
        	ResearchDataEntry e = ResearchDataEntry.GetResearchDataEntry(key);
        	if (e != null) {
        		rec.ResearchRequirementEntries.Remove(e);
        		FUtil.log("Removed research '"+key+"' from recipe "+recipeToString(rec, false, true));
        	}
		}
		
		public static void addResearch(CraftData rec, string key) {
			rec.ResearchRequirements.Add(key);
        	ResearchDataEntry e = ResearchDataEntry.GetResearchDataEntry(key);
        	if (e != null) {
        		rec.ResearchRequirementEntries.Add(e);
				FUtil.log("Added research '"+key+"' to recipe "+recipeToString(rec, false, true));
        	}
		}
		
		public static string ingredientToString(CraftCost ing) {
			return ing.Key+" x "+ing.Amount+" ("+ing.Name+")";
		}
		
		public static string recipeToString(CraftData rec, bool fullIngredients = false, bool fullResearch = false) {
			string ret = "'"+rec.RecipeSet+"/"+rec.Category+"/"+rec.CanCraftAnywhere+"::"+rec.Key+"'="+rec.CraftedKey+"x"+rec.CraftedAmount+" from ";
			if (fullIngredients) {
				List<string> li = new List<string>();
				rec.Costs.ForEach(c => li.Add(ingredientToString(c)));
				ret += "I["+string.Join(", ", li.ToArray())+"]";
			}
			else {
				ret += rec.Costs.Count+" items";
			}
			ret += " & ";
			if (fullResearch) {
				ret += "T["+string.Join(", ", rec.ResearchRequirements.ToArray())+"]";
			}
			else {
				ret += rec.ResearchRequirements.Count+" techs";
			}
			return ret;
		}
		
	}
}
