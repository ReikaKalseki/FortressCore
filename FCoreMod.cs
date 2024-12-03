using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Harmony;

namespace ReikaKalseki.FortressCore
{
	public abstract class FCoreMod : FortressCraftMod {
		
		private static readonly List<FCoreMod> failedMods = new List<FCoreMod>();
		
		public readonly string modName;
		protected readonly HarmonyInstance harmony;
		protected readonly Assembly modDLL = FUtil.tryGetModDLL();
		
		protected FCoreMod(string n) {
			modName = n;
			FUtil.log("Initializing mod '"+n+"' in DLL "+modDLL.Location+" @ "+FUtil.getTime());
			
			harmony = HarmonyInstance.Create(modName);
			name = modName; //FortressCraftMod extends MonoBehaviour
	        HarmonyInstance.DEBUG = true;
	        //does not work with static version of harmony used in FCE FileLog.logPath = Path.Combine(Path.GetDirectoryName(modDLL.Location), "harmony-log.txt");
		}
		
	    public sealed override ModRegistrationData Register() {
			ModRegistrationData data = new ModRegistrationData();
			try {
				FUtil.log("Loading @ "+FUtil.getTime(), modDLL);
				loadMod(data);
			}
			catch (Exception ex) {
				FUtil.log("Mod threw an exception during boot: "+ex.ToString(), modDLL);
				failedMods.Add(this);
			}
			return data; //return the data, just in case, so can keep at least some registered data
	    }
		
		protected abstract void loadMod(ModRegistrationData data);
		
		protected void runHarmony() {
			if (harmony == null) {
	        	FUtil.log("Cannot run harmony - no harmony instance!");
				return;
			}
	        FileLog.Log("Started "+modName+" harmony (harmony log) @ "+FUtil.getTime());
	        FUtil.log("Started harmony @ "+FUtil.getTime());
	        
	        try {
				//InstructionHandlers.runPatchesIn(harmony, patchHolder);
				harmony.PatchAll(modDLL);
        		FUtil.log("Main harmony patches complete.");
	        }
	        catch (Exception e) {
				FileLog.Log("Caught exception when running patches!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
	        }
		}
	}
}
