using System;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using System.Xml;
using ReikaKalseki.FortressCore;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public class Interpolation {
	
		private readonly SortedDictionary<float, float> curvePoints = new SortedDictionary<float, float>();
		private readonly List<float> keys = new List<float>();
		
		public float minValue { get { return keys.Count == 0 ? 0 : curvePoints[keys[0]]; } }
		public float maxValue { get { return keys.Count == 0 ? 0 : curvePoints[keys[keys.Count-1]]; } }
		
		public Interpolation() {
			
		}
		
		public Interpolation addPoint(float at, float value) {
			curvePoints[at] = value;
			if (!keys.Contains(at))
				keys.Add(at);
			keys.Sort();
			return this;
		}
		
		public float getValue(float at) {
			if (curvePoints.Count == 0) {
				FUtil.log("Cannot fetch a value from an empty interpolation!");
				return 0;
			}
			if (at <= keys[0])
				return minValue;
			if (at >= keys[keys.Count-1])
				return maxValue;
			int idx = keys.BinarySearch(at);
			if (idx >= 0)
				return curvePoints[keys[idx]]; //keys[idx] == at
			int prev = ~idx-1;
			int next = prev+1;
			bool flag = false;
			if (!keys.Contains(prev)) {
				FUtil.log("Error, looking up "+at+" in "+curvePoints+" found prev="+prev+" but that is not in the dict!");
				flag = true;
			}
			if (!keys.Contains(next)) {
				FUtil.log("Error, looking up "+at+" in "+curvePoints+" found next="+next+" but that is not in the dict!");
				flag = true;
			}
			if (flag)
				return 0;
			float y1 = curvePoints[keys[prev]];
			float y2 = curvePoints[keys[next]];
			return Mathf.Lerp(y1, y2, (at-prev)/(float)(next-prev));
		}
		
		public void iterate(Action<KeyValuePair<float, float>> act) {
			foreach (KeyValuePair<float, float> kvp in curvePoints) {
				act.Invoke(kvp);
			}
		}
		
		public void clear() {
			curvePoints.Clear();
			keys.Clear();
		}
	}
}
