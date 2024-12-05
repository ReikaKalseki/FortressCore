using System;

using System.Collections.Generic;
using System.Collections;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public static class WorldUtil {
		
		public static readonly long COORD_OFFSET = 4611686017890516992L;
		public static readonly int SEGMENT_SIZE = 16;
		
		public static bool isSegmentValid(this Segment s) {
			return s != null && s.IsSegmentInAGoodState() && s.maCubes != null && s.maCubeData != null;
		}
		
	    public static Coordinate checkSegmentForCube(Segment s, ushort blockID, ushort metadata = 0) {
			return checkSegmentForCube(s, (id, meta) => id == blockID && (metadata == 32767 || (meta.mValue == metadata)));
	    }
		
		public static Coordinate checkSegmentForCube(Segment s, Func<ushort, CubeData, bool> validity) {
			if (!s.isSegmentValid())
				return null;
			for (int j = SEGMENT_SIZE-1; j >= 0; j--) {
				for (int i = 0; i < SEGMENT_SIZE; i++) {
					for (int k = 0; k < SEGMENT_SIZE; k++) {
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
				sx -= SEGMENT_SIZE;
				i += SEGMENT_SIZE;
			}
			else if (i >= SEGMENT_SIZE) {
				sx += SEGMENT_SIZE;
				i -= SEGMENT_SIZE;
			}
			
			if (j < 0) {
				sy -= SEGMENT_SIZE;
				j += SEGMENT_SIZE;
			}
			else if (j >= SEGMENT_SIZE) {
				sy += SEGMENT_SIZE;
				j -= SEGMENT_SIZE;
			}
			
			if (k < 0) {
				sz -= SEGMENT_SIZE;
				k += SEGMENT_SIZE;
			}
			else if (k >= SEGMENT_SIZE) {
				sz += SEGMENT_SIZE;
				k -= SEGMENT_SIZE;
			}
			
			if (sx != s.baseX || sy != s.baseY || sz != s.baseZ)
				s = segmentGetter.Invoke(sx, sy, sz);
			return s.isSegmentValid();
		}
		
		public static Biomes getBiome(SegmentEntity e) {
			return getBiome(e.mnY);
		}
		
		public static Biomes getBiome(long mnY) {
			long depth = -(mnY - COORD_OFFSET);
			if (depth < 40)
				return Biomes.SURFACE;
			else if (depth < -BiomeLayer.CavernColdCeiling)
				return Biomes.UPPERCAVES;
			else if (depth < -BiomeLayer.CavernColdFloor)
				return Biomes.COLDCAVES;
			else if (depth < -BiomeLayer.CavernToxicCeiling)
				return Biomes.LOWERCAVES;
			else if (depth < -BiomeLayer.CavernToxicFloor)
				return Biomes.TOXICCAVES;
			else if (depth < -BiomeLayer.CavernMagmaCeiling)
				return Biomes.DEEPCAVES;
			else if (depth < -BiomeLayer.CavernMagmaFloor+50)
				return Biomes.MAGMACAVES;
			else
				return Biomes.BELOWLAVA;
		}
		/*
		public float getBiomeTemperature(Biomes b) {
			switch (b) {
				default:
					return -20;
			}
		}*/
		
		public enum Biomes {
			SURFACE,
			UPPERCAVES,
			COLDCAVES,
			LOWERCAVES,
			TOXICCAVES,
			DEEPCAVES,
			MAGMACAVES,
			BELOWLAVA
			
		}
		
	}
}
