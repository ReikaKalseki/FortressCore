using System;

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Xml;
using ReikaKalseki.FortressCore;

namespace ReikaKalseki.FortressCore
{
	public class Config<E>
	{
		private readonly string filename = "FortressTweaks_Config.xml";
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
			string folder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string path = System.IO.Path.Combine(folder, filename);
			if (System.IO.File.Exists(path))
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
								FUtil.log("Chosen "+name+" value ("+raw+") was out of bounds, clamed to "+get);
							}
							data[name] = get;
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
			}
			else {
				FUtil.log("Config file does not exist at "+path+"; generating.");
				try
				{
					XmlDocument doc = new XmlDocument();
					XmlElement root = doc.CreateElement("Settings");
					doc.AppendChild(root);
					foreach (E key in Enum.GetValues(typeof(E))) {
						createNode(doc, root, key);
					}
					doc.Save(path);
					FUtil.log("Default config successfully generated.");
				}
				catch (Exception ex)
				{
					FUtil.log("Config failed to generate: "+ex.ToString());
				}
			}
		}
			
		private void createNode(XmlDocument doc, XmlElement root, E key) {
			ConfigEntry e = getEntry(key);
			XmlElement node = doc.CreateElement(Enum.GetName(typeof(E), key));
			
			XmlComment com = doc.CreateComment(e.desc);
			
			XmlElement val = doc.CreateElement("value");
			val.InnerText = e.formatValue(e.defaultValue);
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
