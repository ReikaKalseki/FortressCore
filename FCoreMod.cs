using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Harmony;

namespace ReikaKalseki.FortressCore
{
	public class FCoreMod : FortressCraftMod
	{
		public readonly string modName;
		protected readonly HarmonyInstance harmony;
		protected readonly Assembly modDLL = FUtil.tryGetModDLL();
		
		protected FCoreMod(string n)
		{
			modName = n;
			FUtil.log("Initializing mod '"+n+"' in DLL "+modDLL.Location);
			
			harmony = HarmonyInstance.Create(modName);
	        HarmonyInstance.DEBUG = true;
	        //does not work with static version of harmony used in FCE FileLog.logPath = Path.Combine(Path.GetDirectoryName(modDLL.Location), "harmony-log.txt");
		}
		
		protected void runHarmony() {
			if (harmony == null) {
	        	FUtil.log("Cannot run harmony - no harmony instance!");
				return;
			}
	        FileLog.Log("Ran mod register, started harmony (harmony log)");
	        FUtil.log("Ran mod register, started harmony");
	        
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
