﻿using System;

using System.Collections.Generic;
using System.Collections;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public class FCoreMachine : MachineEntity {
		
		public FCoreMachine(eSegmentEntity type, SpawnableObjectEnum objectType, long x, long y, long z, ushort cube, byte flags, ushort value, Vector3 position, Segment segment) : base(type, objectType, x, y, z, cube, flags, value, position, segment) {
			this.mbNeedsLowFrequencyUpdate = true;
			this.mbNeedsUnityUpdate = true;
		}
		
		public override string ToString() {
			return GetType().Name+" @ "+new Coordinate(this).ToString();
		}
		
	}
}