using System;

using System.Collections.Generic;
using System.Collections;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public static class WorldUtil {
		
		public static readonly long COORD_OFFSET = 4611686017890516992L;
		
	    public static Coordinate checkSegmentForCube(Segment s, ushort blockID, ushort metadata = 0) {
			return checkSegmentForCube(s, (id, meta) => id == blockID && (metadata == 32767 || (meta.mValue == metadata)));
	    }
		
		public static Coordinate checkSegmentForCube(Segment s, Func<ushort, CubeData, bool> validity) {
			if (s == null || !s.IsSegmentInAGoodState() || s.maCubes == null || s.maCubeData == null)
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
		
	}
}
