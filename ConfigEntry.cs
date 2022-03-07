using System;

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Xml;
using ReikaKalseki.FortressCore;

namespace ReikaKalseki.FortressCore
{/*
	public class ConfigEntryArray : ConfigEntry
	{
		private readonly ConfigEntry[] values;
		
		public ConfigEntryArray(string d, Type t, ConfigEntry[] arr) : base(d, t, arr[0].defaultValue, arr[0].vanillaValue) {
			values = arr;
		}
		
		public int count() {
			return values.Length;	
		}
		
		public ConfigEntry getAt(int i) {
			return values[i];
		}
	}*/
		
	public class ConfigEntry : Attribute
	{
		
		public readonly string desc;
		public readonly Type type;
		public readonly float minValue;
		public readonly float maxValue;
		public readonly float defaultValue;
		public readonly float vanillaValue;

		public readonly List<ConfigEntry> children = new List<ConfigEntry>();
		
		public ConfigEntry(string d, bool flag) : this(d, typeof(bool), flag ? 1 : 0, 0, 1, 0) {
			
		}
		
		public ConfigEntry(string d, Type t, float def, float v) : this(d, t, def, float.MinValue, float.MaxValue, v) {
			
		}
		
		public ConfigEntry(string d, Type t, float def, float min, float max, float v) {
			desc = d;
			type = t;
			defaultValue = def;
			minValue = min;
			maxValue = max;
			vanillaValue = v;
		}
		
		public bool validate(ref float val) {
			bool flag = true;
			if (val < minValue) {
				val = minValue;
				flag = false;
			}
			else if (val > maxValue) {
				val = maxValue;
				flag = false;
			}
			return flag;
		}
		
		public float parse(string text) {
			if (type == typeof(bool)) {
				return text.ToLowerInvariant() == "true" ? 1 : 0;
			}
			return float.Parse(text);
		}
			
		public string formatValue(float value) {
			if (type == typeof(bool)) {
				return (value > 0).ToString();
			}
			else if (type == typeof(int) || type == typeof(uint) || type == typeof(byte) || type == typeof(long) || type == typeof(ulong)) {
				return ((int)(value)).ToString();
			}
			return value.ToString("0.00");
		}
	}
}
