using System;
using System.IO;    //For data read/write methods
using System.Collections;   //Working with Lists and Collections
using System.Collections.Generic;   //Working with Lists and Collections
using System.Linq;   //More advanced manipulation of lists/collections
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.

namespace ReikaKalseki.FortressCore
{
	public static class InstructionHandlers
	{		
		public static long getIntFromOpcode(CodeInstruction ci) {
			switch (ci.opcode.Name) {
				case "ldc.i4.m1":
				return -1;
				case "ldc.i4.0":
				return 0;
				case "ldc.i4.1":
				return 1;
				case "ldc.i4.2":
				return 2;
				case "ldc.i4.3":
				return 3;
				case "ldc.i4.4":
				return 4;
				case "ldc.i4.5":
				return 5;
				case "ldc.i4.6":
				return 6;
				case "ldc.i4.7":
				return 7;
				case "ldc.i4.8":
				return 8;
				case "ldc.i4.s":
				return (int)((sbyte)ci.operand);
				case "ldc.i4":
				return (int)ci.operand;
				case "ldc.i8":
				return (long)ci.operand;
			default:
				return Int64.MaxValue;
			}
		}
		
		public static void nullInstructions(List<CodeInstruction> li, int begin, int end) {
			for (int i = begin; i <= end; i++) {
				CodeInstruction insn = li[i];
				insn.opcode = OpCodes.Nop;
				insn.operand = null;
			}
		}
		
		public static CodeInstruction createMethodCall(Type owner, string name, bool instance, params Type[] args) {
			return new CodeInstruction(OpCodes.Call, convertMethodOperand(owner, name, instance, args));
		}
		
		public static MethodInfo convertMethodOperand(Type owner, string name, bool instance, params Type[] args) {
			MethodInfo ret = AccessTools.Method(owner, name, args);
			if (ret == null)
				throw new Exception("No such method '"+owner.Name+"::"+name+"("+string.Join(", ", args.Select(t => t.Name).ToArray())+") [static="+!instance+"]");
			//ret.IsStatic = !instance;
			return ret;
		}
		
		public static FieldInfo convertFieldOperand(Type owner, string name) {
			FieldInfo ret = AccessTools.Field(owner, name);
			if (ret == null)
				throw new Exception("No such field '"+owner.Name+"::"+name);
			return ret;
		}
		
		public static int getInstruction(List<CodeInstruction> li, int start, int index, OpCode opcode, params object[] args) {
			int count = 0;
			for (int i = start; i < li.Count; i++) {
				CodeInstruction insn = li[i];
				if (insn.opcode == opcode) {
					if (match(insn, args)) {
						if (count == index)
							return i;
						else
							count++;
					}
				}
			}
			return -1;
		}
		
		public static int getFirstOpcode(List<CodeInstruction> li, int after, OpCode opcode) {
			for (int i = after; i < li.Count; i++) {
				CodeInstruction insn = li[i];
				if (insn.opcode == opcode) {
					return i;
				}
			}
			return -1;
		}
		
		public static int getLastOpcodeBefore(List<CodeInstruction> li, int before, OpCode opcode) {
			if (before > li.Count)
				before = li.Count;
			for (int i = before-1; i >= 0; i--) {
				CodeInstruction insn = li[i];
				if (insn.opcode == opcode) {
					return i;
				}
			}
			return -1;
		}
		
		public static int getLastInstructionBefore(List<CodeInstruction> li, int before, OpCode opcode, params object[] args) {
			for (int i = before-1; i >= 0; i--) {
				CodeInstruction insn = li[i];
				if (insn.opcode == opcode) {
					if (match(insn, args)) {
						return i;
					}
				}
			}
			return -1;
		}
		
		public static bool matchPattern(List<CodeInstruction> li, int at, params OpCode[] codes) {
			if (at+codes.Length > li.Count)
				return false;
			for (int i = 0; i < codes.Length; i++) {
				CodeInstruction insn = li[at+i];
				if (insn.opcode != codes[i])
					return false;
			}
			return true;
		}
		
		public static bool matchPattern(List<CodeInstruction> li, int at, params CodeInstruction[] codes) {
			if (at+codes.Length > li.Count)
				return false;
			for (int i = 0; i < codes.Length; i++) {
				CodeInstruction insn = li[at+i];
				if (!match(insn, codes[i]))
				    return false;
			}
			return true;
		}
		
		public static bool match(CodeInstruction a, CodeInstruction b) {
			return a.opcode == b.opcode && matchOperands(a.operand, b.operand);
		}
		
		public static bool matchOperands(object o1, object o2) {
			if (o1 == o2)
				return true;
			if (o1 == null || o2 == null)
				return false;
			if (o1 is LocalBuilder && o2 is LocalBuilder) {
				return ((LocalBuilder)o1).LocalIndex == ((LocalBuilder)o2).LocalIndex;
			}
			return o1.Equals(o2);
		}
		
		public static bool match(CodeInstruction insn, params object[] args) {
			//FileLog.Log("Comparing "+insn.operand.GetType()+" "+insn.operand.ToString()+" against seek of "+String.Join(",", args.Select(p=>p.ToString()).ToArray()));
			if (insn.opcode == OpCodes.Call || insn.opcode == OpCodes.Callvirt) { //Type class, string name, bool instance, Type[] args
				MethodInfo info = convertMethodOperand((Type)args[0], (string)args[1], (bool)args[2], (Type[])args[3]);
				return insn.operand == info;
			}
			else if (insn.opcode == OpCodes.Isinst || insn.opcode == OpCodes.Newobj) {
				return insn.operand == (Type)args[0];
			}
			else if (insn.opcode == OpCodes.Ldfld || insn.opcode == OpCodes.Stfld || insn.opcode == OpCodes.Ldsfld || insn.opcode == OpCodes.Stsfld) { //Type class, string name
				FieldInfo info = convertFieldOperand((Type)args[0], (string)args[1]);
				return insn.operand == info;
			}
			else if (insn.opcode == OpCodes.Ldarg) { //int pos
				return insn.operand == args[0];
			}
			else if (insn.opcode == OpCodes.Ldc_I4) { //ldc
				return insn.LoadsConstant(Convert.ToInt32(args[0]));
			}
			else if (insn.opcode == OpCodes.Ldc_R4) { //ldc
				return insn.LoadsConstant(Convert.ToSingle(args[0]));
			}
			else if (insn.opcode == OpCodes.Ldc_I8) { //ldc
				return insn.LoadsConstant(Convert.ToInt64(args[0]));
			}
			else if (insn.opcode == OpCodes.Ldc_R8) { //ldc
				return insn.LoadsConstant(Convert.ToDouble(args[0]));
			}
			else if (insn.opcode == OpCodes.Ldloc_S || insn.opcode == OpCodes.Stloc_S) { //LocalBuilder contains a pos and type
				LocalBuilder loc = (LocalBuilder)insn.operand;
				return args[0] is int && loc.LocalIndex == (int)args[0]/* && loc.LocalType == args[1]*/;
			}
			return true;
		}
		
		public static string toString(List<CodeInstruction> li) {
			return "\n"+String.Join("\n", li.Select(p=>toString(p)).ToArray());
		}
		
		public static string toString(List<CodeInstruction> li, int idx) {
			return idx < 0 || idx >= li.Count ? "ERROR: OOB "+idx+"/"+li.Count : "#"+Convert.ToString(idx, 16)+" = "+toString(li[idx]);
		}
		
		public static string toString(CodeInstruction ci) {
			return ci.opcode.Name+" "+(ci.operand != null ? ci.operand.ToString() : "");
		}
		/*
		public static void runPatchesIn(HarmonyInstance h, Type parent) {
       		FileLog.logPath = Path.Combine(Path.GetDirectoryName(parent.Assembly.Location), "harmony-log.txt");
			FUtil.log("Running harmony patches in "+parent.Assembly.GetName().Name+"::"+parent.Name);
			FileLog.Log("Running harmony patches in "+parent.Assembly.GetName().Name+"::"+parent.Name);
			foreach (Type t in parent.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
				FileLog.Log("Running harmony patches in "+t.Name);
				h.Patch(parent);
			}
		}*/
		
		public static void patchMethod(HarmonyInstance h, Type methodHolder, string name, Type patchHolder, string patchName) {
       		//FileLog.logPath = Path.Combine(Path.GetDirectoryName(patchHolder.Assembly.Location), "harmony-log.txt");
			FileLog.Log("Running harmony patch in "+patchHolder.FullName+"::"+patchName+" on "+methodHolder.FullName+"::"+name);
			MethodInfo m = methodHolder.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			if (m == null)
				throw new Exception("Method "+name+" not found in "+methodHolder.AssemblyQualifiedName);
			patchMethod(h, m, new HarmonyMethod(AccessTools.Method(patchHolder, patchName, new Type[]{typeof(IEnumerable<CodeInstruction>)})));
		}
		
		public static void patchMethod(HarmonyInstance h, Type methodHolder, string name, Assembly patchHolder, Action<List<CodeInstruction>> patch) {
       		//FileLog.logPath = Path.Combine(Path.GetDirectoryName(patchHolder.Location), "harmony-log.txt");
       		FileLog.Log("Running harmony patch from "+patchHolder.GetName().Name+" on "+methodHolder.FullName+"::"+name);
			MethodInfo m = methodHolder.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			if (m == null)
				throw new Exception("Method "+name+" not found in "+methodHolder.FullName);
			currentPatch = patch;
			patchMethod(h, m, new HarmonyMethod(AccessTools.Method(MethodBase.GetCurrentMethod().DeclaringType, "patchHook", new Type[]{typeof(IEnumerable<CodeInstruction>)})));
			currentPatch = null;
		}
		
		public static void patchMethod(HarmonyInstance h, MethodInfo m, Assembly patchHolder, Action<List<CodeInstruction>> patch) {
       		//FileLog.logPath = Path.Combine(Path.GetDirectoryName(patchHolder.Location), "harmony-log.txt");
			FileLog.Log("Running harmony patch from "+patchHolder.GetName().Name+" on "+m.DeclaringType.FullName+"::"+m.Name);
			currentPatch = patch;
			patchMethod(h, m, new HarmonyMethod(AccessTools.Method(MethodBase.GetCurrentMethod().DeclaringType, "patchHook", new Type[]{typeof(IEnumerable<CodeInstruction>)})));
			currentPatch = null;
		}
		
		private static Action<List<CodeInstruction>> currentPatch;
		private static IEnumerable<CodeInstruction> patchHook(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			currentPatch.Invoke(codes);
			//FileLog.Log("Codes are "+InstructionHandlers.toString(codes));
			return codes.AsEnumerable();
		}
		
		private static void patchMethod(HarmonyInstance h, MethodInfo m, HarmonyMethod patch) {
			try {
				h.Patch(m, null, null, patch);
				FileLog.Log("Done patch");
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
		}
		
	    public static Type getTypeBySimpleName(string name) {
	        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse()) {
				try {
		            Type tt = assembly.GetType(name);
		            if (tt != null)
		                return tt;
				}
				catch {
					
				}
	        }	
	        return null;
	    }
		
		public static void patchEveryReturnPre(List<CodeInstruction> codes, params CodeInstruction[] insert) {
			patchEveryReturnPre(codes, insert.ToList<CodeInstruction>());
		}
		
		public static void patchEveryReturnPre(List<CodeInstruction> codes, List<CodeInstruction> insert) {
			patchEveryReturnPre(codes, (li, idx) => li.InsertRange(idx, insert));
		}
		
		public static void patchEveryReturnPre(List<CodeInstruction> codes, Action<List<CodeInstruction>, int> injectHook) {
			for (int i = codes.Count-1; i >= 0; i--) {
				if (codes[i].opcode == OpCodes.Ret) {
					injectHook(codes, i);
				}
			}
		}
		
		public static void patchInitialHook(List<CodeInstruction> codes, params CodeInstruction[] insert) {
			List<CodeInstruction> li = new List<CodeInstruction>();
			foreach (CodeInstruction c in insert) {
				li.Add(c);
			}
			patchInitialHook(codes, li);
		}
		
		public static void patchInitialHook(List<CodeInstruction> codes, List<CodeInstruction> insert) {
			for (int i = insert.Count-1; i >= 0; i--) {
				codes.Insert(0, insert[i]);
			}
		}
		
		public static List<CodeInstruction> extract(List<CodeInstruction> codes, int from, int to) {
			List<CodeInstruction> li = new List<CodeInstruction>();
			for (int i = from; i <= to; i++) {
				li.Add(codes[i]);
			}
			codes.RemoveRange(from, to-from+1);
			return li;
		}
		
		//everything below this line ported from newer harmony 
		
		/// <summary>Tests if the code instruction loads an integer constant</summary>
		/// <param name="code">The <see cref="CodeInstruction"/></param>
		/// <param name="number">The integer constant</param>
		/// <returns>True if the instruction loads the constant</returns>
		///
		public static bool LoadsConstant(this CodeInstruction code, long number)
		{
			var op = code.opcode;
			if (number == -1 && op == OpCodes.Ldc_I4_M1) return true;
			if (number == 0 && op == OpCodes.Ldc_I4_0) return true;
			if (number == 1 && op == OpCodes.Ldc_I4_1) return true;
			if (number == 2 && op == OpCodes.Ldc_I4_2) return true;
			if (number == 3 && op == OpCodes.Ldc_I4_3) return true;
			if (number == 4 && op == OpCodes.Ldc_I4_4) return true;
			if (number == 5 && op == OpCodes.Ldc_I4_5) return true;
			if (number == 6 && op == OpCodes.Ldc_I4_6) return true;
			if (number == 7 && op == OpCodes.Ldc_I4_7) return true;
			if (number == 8 && op == OpCodes.Ldc_I4_8) return true;
			if (op != OpCodes.Ldc_I4 && op != OpCodes.Ldc_I4_S && op != OpCodes.Ldc_I8) return false;
			return Convert.ToInt64(code.operand) == number;
		}

		/// <summary>Tests if the code instruction loads a floating point constant</summary>
		/// <param name="code">The <see cref="CodeInstruction"/></param>
		/// <param name="number">The floating point constant</param>
		/// <returns>True if the instruction loads the constant</returns>
		///
		public static bool LoadsConstant(this CodeInstruction code, double number)
		{
			if (code.opcode != OpCodes.Ldc_R4 && code.opcode != OpCodes.Ldc_R8) return false;
			double val = Convert.ToDouble(code.operand);
			return Math.Abs(val-number) < 0.001;
		}

		/// <summary>Tests if the code instruction loads a string constant</summary>
		/// <param name="code">The <see cref="CodeInstruction"/></param>
		/// <param name="str">The string</param>
		/// <returns>True if the instruction loads the constant</returns>
		///
		public static bool LoadsConstant(this CodeInstruction code, string str)
		{
			if (code.opcode != OpCodes.Ldstr) return false;
			var val = Convert.ToString(code.operand);
			return val == str;
		}
	}
}
