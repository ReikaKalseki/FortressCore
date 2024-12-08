using System;

using System.Collections.Generic;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public static class MathUtil {
		
	    public static double py3d(double rawX, double rawY, double rawZ, double rawX2, double rawY2, double rawZ2) {
	    	return py3d(rawX2-rawX, rawY2-rawY, rawZ2-rawZ);
	    }
		
	    public static double py3d(double x, double y, double z) {
	    	return Math.Sqrt(x*x+y*y+z*z);
	    }

		public static bool isPointInsideEllipse(double x, double y, double z, double ra, double rb, double rc) {
			return (ra > 0 ? ((x*x)/(ra*ra)) : 0) + (rb > 0 ? ((y*y)/(rb*rb)) : 0) + (rc > 0 ? ((z*z)/(rc*rc)) : 0) <= 1;
		}
		
	}
}
