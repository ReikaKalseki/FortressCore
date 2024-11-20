using System;

using System.Collections.Generic;
using System.Collections;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public static class WorldUtil {
		
		public static readonly long COORD_OFFSET = 4611686017890516992L;
		
		public static bool isSegmentValid(this Segment s) {
			return s != null && s.IsSegmentInAGoodState() && s.maCubes != null && s.maCubeData != null;
		}
		
	    public static Coordinate checkSegmentForCube(Segment s, ushort blockID, ushort metadata = 0) {
			return checkSegmentForCube(s, (id, meta) => id == blockID && (metadata == 32767 || (meta.mValue == metadata)));
	    }
		
		public static Coordinate checkSegmentForCube(Segment s, Func<ushort, CubeData, bool> validity) {
			if (!s.isSegmentValid())
				return null;
			for (int j = 15; j >= 0; j--) {
				for (int i = 0; i < 16; i++) {
					for (int k = 0; k < 16; k++) {
						ushort id;
						CubeData data;
						s.GetCubeDataNoChecking(i, j, k, out id, out data);
						if (validity.Invoke(id, data))
							return new Coordinate(s.baseX + (long)i, s.baseY + (long)j, s.baseZ + (long)k);
					}
				}
			}
			return null;
	    }
		
		public static bool adjustCoordinateInPossiblyAdjacentSegment(ref Segment s, ref int i, ref int j, ref int k, Func<long, long, long, Segment> segmentGetter) {
			long sx = s.baseX;
			long sy = s.baseY;
			long sz = s.baseZ;
			
			if (i < 0) {
				sx -= 16;
				i += 16;
			}
			else if (i >= 16) {
				sx += 16;
				i -= 16;
			}
			
			if (j < 0) {
				sy -= 16;
				j += 16;
			}
			else if (j >= 16) {
				sy += 16;
				j -= 16;
			}
			
			if (k < 0) {
				sz -= 16;
				k += 16;
			}
			else if (k >= 16) {
				sz += 16;
				k -= 16;
			}
			
			if (sx != s.baseX || sy != s.baseY || sz != s.baseZ)
				s = segmentGetter.Invoke(sx, sy, sz);
			return s.isSegmentValid();
		}
		
	}
}
