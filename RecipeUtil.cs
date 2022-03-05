using System;

using System.Collections.Generic;

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
		
		public static CraftData addRecipe(string id, string item, int amt = 1, string cat = "Manufacturer") {
			CraftData rec = new CraftData();
			rec.RecipeSet = cat;
			rec.Key = "ReikaKalseki."+id;
			rec.CraftedKey = item;
			rec.CraftedAmount = amt;
			CraftData.mRecipesForSet[cat].Add(rec);
			link(rec);
			FUtil.log("Added new recipe "+recipeToString(rec, true, true));
			return rec;
		}
		
		public static CraftData copyRecipe(CraftData template) {
			string xml = XMLParser.SerializeObject(template, typeof(CraftData));
			return (CraftData)XMLParser.DeserializeObject(xml, typeof(CraftData));
		}
		
		private static void link(CraftData rec) {
			CraftData.LinkEntries(new List<CraftData>(new CraftData[]{rec}), rec.Category);
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
			string ret = "'"+rec.Category+"::"+rec.Key+"'="+rec.CraftedKey+"x"+rec.CraftedAmount+" from ";
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
