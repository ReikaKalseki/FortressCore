using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Xml;
using ReikaKalseki.FortressCore;

namespace ReikaKalseki.FortressCore
{
	public class Config<E>
	{
		private readonly string filename;
		private readonly Dictionary<string, float> data = new Dictionary<string, float>();
		
		private readonly FCoreMod owner;
		
		public Config(FCoreMod mod)
		{
			owner = mod;
			filename = Environment.UserName+"_"+owner.name+"_Config.xml";
			populateDefaults();
		}
		
		private void populateDefaults() {
			foreach (E key in Enum.GetValues(typeof(E))) {
				ConfigEntry e = getEntry(key);
				string name = Enum.GetName(typeof(E), key);
				data[name] = e.defaultValue;
			}
		}
		
		public void load() {
			string folder = Path.GetDirectoryName(FUtil.tryGetModDLL().Location);
			string path = Path.Combine(folder, filename);
			HashSet<string> missing = new HashSet<string>(data.Keys);
			bool gen = false;
			if (File.Exists(path))
			{
				FUtil.log("Loading config file at "+path);
				try
				{
					XmlDocument doc = new XmlDocument();
					doc.Load(path);
					XmlElement root = (XmlElement)doc.GetElementsByTagName("Settings")[0];
					foreach (XmlNode e in root.ChildNodes) {
						if (!(e is XmlElement))
							continue;
						string name = e.Name;
						try
						{
							XmlElement val = (XmlElement)(e as XmlElement).GetElementsByTagName("value")[0];
							E key = (E)Enum.Parse(typeof(E), name);
							ConfigEntry entry = getEntry(key);
							float raw = entry.parse(val.InnerText);
							float get = raw;
							if (!entry.validate(ref get)) {
								FUtil.log("Chosen "+name+" value ("+raw+") was out of bounds, clamped to "+get);
							}
							data[name] = get;
							missing.Remove(name);
						}
						catch (ArgumentException ex)
						{
							FUtil.log("Config entry "+name+" did not find a corresponding config mapping. Skipping.");
						}
						catch (Exception ex)
						{
							FUtil.log("Config entry "+name+" failed to load: "+ex.ToString());
						}
					}
					string vals = string.Join(";", data.Select(x => x.Key + "=" + x.Value).ToArray());
					FUtil.log("Config successfully loaded: "+vals);
				}
				catch (Exception ex)
				{
					FUtil.log("Config failed to load: "+ex.ToString());
				}
				if (missing.Count > 0) {
					FUtil.log("Config missing the following entries: "+string.Join(", ", missing.ToArray()));
					FUtil.log("Will generate a fresh copy, with your custom values.");
					gen = true;
				}
			}
			else {
				FUtil.log("Config file does not exist at "+path+"; generating.");
				gen = true;
			}
			if (gen) {
				try
				{
					XmlDocument doc = new XmlDocument();
					XmlElement root = doc.CreateElement("Settings");
					doc.AppendChild(root);
					foreach (E key in Enum.GetValues(typeof(E))) {
						createNode(doc, root, key);
					}
					doc.Save(path);
					FUtil.log("Config successfully generated.");
				}
				catch (Exception ex)
				{
					FUtil.log("Config failed to generate: "+ex.ToString());
				}
			}
		}
			
		private void createNode(XmlDocument doc, XmlElement root, E key) {
			ConfigEntry e = getEntry(key);
			string name = Enum.GetName(typeof(E), key);
			XmlElement node = doc.CreateElement(name);
			
			XmlComment com = doc.CreateComment(e.desc);
			
			XmlElement val = doc.CreateElement("value");
			val.InnerText = e.formatValue(data[name]);
			node.AppendChild(val);
			
			XmlElement def = doc.CreateElement("defaultValue");
			def.InnerText = e.formatValue(e.defaultValue);
			node.AppendChild(def);
			XmlElement van = doc.CreateElement("vanillaValue");
			van.InnerText = e.formatValue(e.vanillaValue);
			node.AppendChild(van);
			
			//XmlElement desc = doc.CreateElement("description");
			//desc.InnerText = e.desc;
			//node.AppendChild(desc);
			
			if (e.type != typeof(bool)) {
				XmlElement min = doc.CreateElement("minimumValue");
				min.InnerText = e.formatValue(e.minValue);
				node.AppendChild(min);
				XmlElement max = doc.CreateElement("maximumValue");
				max.InnerText = e.formatValue(e.maxValue);
				node.AppendChild(max);
			}
			root.AppendChild(com);
			root.AppendChild(node);
		}
		
		private float getValue(string key) {
			return data.ContainsKey(key) ? data[key] : 0;
		}
		
		public bool getBoolean(E key) {
			float ret = getFloat(key);
			return ret > 0.001;
		}
		
		public int getInt(E key) {
			float ret = getFloat(key);
			return (int)Math.Floor(ret);
		}
		
		public float getFloat(E key) {
			return getValue(Enum.GetName(typeof(E), key));
		}
		
		public ConfigEntry getEntry(E key) {
			MemberInfo info = typeof(E).GetField(Enum.GetName(typeof(E), key));
			return (ConfigEntry)Attribute.GetCustomAttribute(info, typeof(ConfigEntry));
		}
	}
}
