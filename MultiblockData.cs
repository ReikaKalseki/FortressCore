using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using StackFrame = System.Diagnostics.StackFrame;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public class MultiblockData {
			
		public static readonly VanillaMultiblockPrefab LAB = new VanillaMultiblockPrefab(SpawnableObjectEnum.Laboratory, new Dimensions(3, 3, 3));
		public static readonly VanillaMultiblockPrefab REFINERY = new VanillaMultiblockPrefab(SpawnableObjectEnum.RefineryReactorVat, new Dimensions(3, 3, 3));
		public static readonly VanillaMultiblockPrefab FUELCOMPRESSOR = new VanillaMultiblockPrefab(SpawnableObjectEnum.T3_FuelCompressor, new Dimensions(3, 3, 3));
		public static readonly VanillaMultiblockPrefab PSB4 = new VanillaMultiblockPrefab(SpawnableObjectEnum.PowerStorageBlock_MK4, new Dimensions(3, 3, 3));
		public static readonly VanillaMultiblockPrefab PSB5 = new VanillaMultiblockPrefab(SpawnableObjectEnum.PowerStorageBlock_MK5, new Dimensions(5, 9, 5));
		public static readonly VanillaMultiblockPrefab OET = new VanillaMultiblockPrefab(SpawnableObjectEnum.OrbitalEnergyTransmitter, new Dimensions(9, 13, 9));
		public static readonly VanillaMultiblockPrefab C5 = new VanillaMultiblockPrefab(SpawnableObjectEnum.CCCCC, new Dimensions(5, 7, 5));
		public static readonly VanillaMultiblockPrefab HRG = new VanillaMultiblockPrefab(SpawnableObjectEnum.T4_Grinder, new Dimensions(3, 1, 3));
		public static readonly VanillaMultiblockPrefab BLASTFURNACE = new VanillaMultiblockPrefab(SpawnableObjectEnum.BlastFurnace, new Dimensions(3, 7, 3));
		public static readonly VanillaMultiblockPrefab CASTINGBASIN = new VanillaMultiblockPrefab(SpawnableObjectEnum.ContinuousCastingBasin, new Dimensions(5, 3, 5));
		public static readonly VanillaMultiblockPrefab GASFILTER = new VanillaMultiblockPrefab(SpawnableObjectEnum.T4_ParticleFilter, new Dimensions(5, 3, 5));
		public static readonly VanillaMultiblockPrefab GASCOMPRESSOR = new VanillaMultiblockPrefab(SpawnableObjectEnum.T4_ParticleCompressor, new Dimensions(3, 3, 3));
		public static readonly VanillaMultiblockPrefab GASSTORAGE = new VanillaMultiblockPrefab(SpawnableObjectEnum.T4_GasStorage, new Dimensions(3, 7, 3));
		public static readonly VanillaMultiblockPrefab BOTTLER = new VanillaMultiblockPrefab(SpawnableObjectEnum.T4_GasBottler, new Dimensions(3, 3, 9));
		
		public class VanillaMultiblockPrefab {
			
			public readonly SpawnableObjectEnum model;
			public readonly Dimensions size;
			
			internal VanillaMultiblockPrefab(SpawnableObjectEnum mdl, Dimensions d) {
				model = mdl;
				size = d;
			}
		}
	    
	    public readonly ushort blockID;
	    public readonly ushort placerMeta;
	    public readonly ushort bodyMeta;
	    public readonly ushort centerMeta;
	    public readonly ushort centerFlipMeta;
	    
	    private readonly Dimensions baseSize;
	    
	    private Dimensions size;
	    
	    public VanillaMultiblockPrefab prefab;
	    
	    public Dimensions getSize(ushort val) {
	    	return getSize(val == centerFlipMeta);
	    }
	    
	    public Dimensions getSize(bool flip) {
	    	return flip ? baseSize.flip : baseSize;
	    }
	    
	    public string name {
	    	get {
	    		return FUtil.getBlockName(blockID, bodyMeta);
	    	}
	    }
	    
	    internal MultiblockData(ushort id, ushort pm, ushort bm, ushort cm, ushort cfm, VanillaMultiblockPrefab pfb) : this(id, pm, bm, cm, cfm, pfb.size) {
	    	prefab = pfb;
	    }
	    
	    internal MultiblockData(ushort id, ushort pm, ushort bm, ushort cm, ushort cfm, Dimensions dd) {
	    	blockID = id;
	    	placerMeta = pm;
	    	bodyMeta = bm;
	    	centerMeta = cm;
	    	centerFlipMeta = cfm;
	    	if (dd.isAsymmetric && cfm == cm)
	    		throw new Exception("Nonsymmetric MB must have two independent center values!");
	    	baseSize = dd;
	    }

		private bool isCubeThisMachine(long checkX, long checkY, long checkZ, WorldFrustrum frustrum) {
			Segment segment = frustrum.GetSegment(checkX, checkY, checkZ);
			if (segment == null || !segment.mbInitialGenerationComplete || segment.mbDestroyed) {
				return false;
			}
			ushort cube = segment.GetCube(checkX, checkY, checkZ);
			if (cube != eCubeTypes.MachinePlacementBlock) {
				return false;
			}
			ushort mValue = segment.GetCubeData(checkX, checkY, checkZ).mValue;
			return mValue == placerMeta;
		}

		private int getExtents(int x, int y, int z, long lastX, long lastY, long lastZ, WorldFrustrum frustrum) {
			long num = lastX;
			long num2 = lastY;
			long num3 = lastZ;
			int num4 = 0;
			for (int i = 0; i < 100; i++) {
				num += (long)x;
				num2 += (long)y;
				num3 += (long)z;
				if (!isCubeThisMachine(num, num2, num3, frustrum)) {
					break;
				}
				num4++;
			}
			return num4;
		}
	    
	    public bool checkForCompletedMachine(ModCheckForCompletedMachineParameters parameters) {
	    	if (checkForCompletedMachine(parameters.Frustrum, parameters.X, parameters.Y, parameters.Z, false))
	    		return true;
	    	return checkForCompletedMachine(parameters.Frustrum, parameters.X, parameters.Y, parameters.Z, true);
	    }

		private bool checkForCompletedMachine(WorldFrustrum frustrum, long lastX, long lastY, long lastZ, bool flip) {
	    	size = getSize(flip);
	    	/*
			if (-MB_MIN_H + MB_MAX_H + 1 != size.width) {
				FUtil.log("Error, X is configured wrongly");
			}
			if (-MB_MIN_V + MB_MAX_V + 1 != size.height) {
				FUtil.log("Error, Y is configured wrongly");
			}
			if (-MB_MIN_H + MB_MAX_H + 1 != size.depth) {
				FUtil.log("Error, Z is configured wrongly");
			}*/
			int num = getExtents(-1, 0, 0, lastX, lastY, lastZ, frustrum);
			num += getExtents(1, 0, 0, lastX, lastY, lastZ, frustrum);
			num++;
			if (size.width > num) {
				FUtil.log(name+" isn't big enough along X(" + num + ")");
				return false;
			}
			if (size.width > num) {
				return false;
			}
			int num2 = getExtents(0, -1, 0, lastX, lastY, lastZ, frustrum);
			num2 += getExtents(0, 1, 0, lastX, lastY, lastZ, frustrum);
			num2++;
			if (size.height > num2) {
				FUtil.log(name+" isn't big enough along Y(" + num2 + ")");
				return false;
			}
			if (size.height > num2) {
				return false;
			}
			int num3 = getExtents(0, 0, -1, lastX, lastY, lastZ, frustrum);
			num3 += getExtents(0, 0, 1, lastX, lastY, lastZ, frustrum);
			num3++;
			if (size.depth > num3) {
				FUtil.log(name+" isn't big enough along Z(" + num3 + ")");
				return false;
			}
			if (size.depth > num3) {
				return false;
			}
			FUtil.log(string.Concat(new object[] {
				name+" is detecting test span of ",
				num,
				":",
				num2,
				":",
				num3
			}));
			bool[,,] array = new bool[size.width, size.height, size.depth];
			for (int i = size.minY; i <= size.maxY; i++) {
				for (int j = size.minZ; j <= size.maxZ; j++) {
					for (int k = size.minX; k <= size.maxX; k++) {
						array[k + size.maxX, i + size.maxY, j + size.maxZ] = true;
					}
				}
			}
			for (int l = -size.outerY; l <= size.outerY; l++) {
				for (int m = -size.outerZ; m <= size.outerZ; m++) {
					for (int n = -size.outerX; n <= size.outerX; n++) {
						if (n != 0 || l != 0 || m != 0) {
							Segment segment = frustrum.GetSegment(lastX + (long)n, lastY + (long)l, lastZ + (long)m);
							if (segment == null || !segment.mbInitialGenerationComplete || segment.mbDestroyed) {
								return false;
							}
							ushort cube = segment.GetCube(lastX + (long)n, lastY + (long)l, lastZ + (long)m);
							bool flag = false;
							if (cube == eCubeTypes.MachinePlacementBlock) {
								ushort mValue = segment.GetCubeData(lastX + (long)n, lastY + (long)l, lastZ + (long)m).mValue;
								if (mValue == placerMeta) {
									flag = true;
								}
							}
							if (!flag) {
								for (int dy = size.minY; dy <= size.maxY; dy++) {
									for (int dz = size.minZ; dz <= size.maxZ; dz++) {
										for (int dx = size.minX; dx <= size.maxX; dx++) {
											int ax = n + dx;
											int ay = l + dy;
											int az = m + dz;
											if (ax >= size.minX && ax <= size.maxX && ay >= size.minY && ay <= size.maxY && az >= size.minZ && az <= size.maxZ) {
												array[ax + size.maxX, ay + size.maxY, az + size.maxZ] = false;
											}
										}
									}
								}
							}
						}
					}
				}
			}
			int num10 = 0;
			for (int dy = size.minY; dy <= size.maxY; dy++) {
				for (int dz = size.minZ; dz <= size.maxZ; dz++) {
					for (int dx = size.minX; dx <= size.maxX; dx++) {
						if (array[dx + size.maxX, dy + size.maxY, dz + size.maxZ]) {
							num10++;
						}
					}
				}
			}
			if (num10 > 1) {
				FUtil.log("Warning, "+name+" has too many valid positions (" + num10 + ")");
				return false;
			}
			if (num10 == 0) {
				return false;
			}
			for (int dy = size.minY; dy <= size.maxY; dy++) {
				for (int dz = size.minZ; dz <= size.maxZ; dz++) {
					for (int dx = size.minX; dx <= size.maxX; dx++) {
						if (array[dx + size.maxX, dy + size.maxY, dz + size.maxZ]) {
							if (buildMultiBlockMachine(frustrum, lastX + (long)dx, lastY + (long)dy, lastZ + (long)dz, flip)) {
								return true;
							}
							FUtil.log("Error, failed to build "+name+" due to bad segment?");
						}
					}
				}
			}
			if (num10 != 0) {
				FUtil.log("Error, thought we found a valid position, but failed to build the "+name+"?");
			}
			return false;
		}

		private bool buildMultiBlockMachine(WorldFrustrum frustrum, long centerX, long centerY, long centerZ, bool flip) {
			HashSet<Segment> hashSet = new HashSet<Segment>();
			bool flag = true;
			try {
				WorldScript.mLocalPlayer.mResearch.GiveResearch(blockID, 0);
				for (int i = size.minY; i <= size.maxY; i++) {
					for (int j = size.minZ; j <= size.maxZ; j++) {
						for (int k = size.minX; k <= size.maxX; k++) {
							Segment segment = frustrum.GetSegment(centerX + (long)k, centerY + (long)i, centerZ + (long)j);
							if (segment == null || !segment.mbInitialGenerationComplete || segment.mbDestroyed) {
								flag = false;
							}
							else {
								if (!hashSet.Contains(segment)) {
									hashSet.Add(segment);
									segment.BeginProcessing();
								}
								if (k == 0 && i == 0 && j == 0) {
									frustrum.BuildOrientation(segment, centerX + (long)k, centerY + (long)i, centerZ + (long)j, blockID, flip ? centerFlipMeta : centerMeta, 65);
								}
								else {
									frustrum.BuildOrientation(segment, centerX + (long)k, centerY + (long)i, centerZ + (long)j, blockID, bodyMeta, 65);
								}
							}
						}
					}
				}
			}
			finally {
				foreach (Segment segment2 in hashSet) {
					segment2.EndProcessing();
				}
				WorldScript.instance.mNodeWorkerThread.KickNodeWorkerThread();
			}
			if (!flag) {
				FUtil.log("Error, failed to build "+name+" as one of it's segments wasn't valid!");
			}
			else {
				AudioSpeechManager.PlayStructureCompleteDelayed = true;
			}
			return flag;
		}
		
	}
}
