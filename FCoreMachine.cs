using System;

using System.Collections.Generic;
using System.Collections;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public abstract class FCoreMachine : MachineEntity {
		
		protected FCoreMachine(eSegmentEntity type, SpawnableObjectEnum objectType, long x, long y, long z, ushort cube, byte flags, ushort value, Vector3 position, Segment segment) : base(type, objectType, x, y, z, cube, flags, value, position, segment) {
			this.mbNeedsLowFrequencyUpdate = true;
			this.mbNeedsUnityUpdate = true;
		}

		protected FCoreMachine(MachineEntityCreationParameters parameters) : base(parameters) {
			this.mbNeedsLowFrequencyUpdate = true;
			this.mbNeedsUnityUpdate = true;
		}
		
		public override string ToString() {
			return GetType().Name+" @ "+new Coordinate(this).ToString();
		}
		
		public virtual bool onInteract(Player ep) {
			return false;
		}
		
		public virtual string getName() {
			return FUtil.getBlockName(mCube, mValue);
		}
		
		public override string GetPopupText() {
			if (Input.GetButtonDown("Interact") && UIManager.AllowInteracting) {
				if (onInteract(WorldScript.mLocalPlayer))
					AudioHUDManager.instance.HUDClick();
			}
			return getName();
		}
		
		protected virtual bool setupHolobaseVisuals(Holobase hb, out GameObject model, out Vector3 size, out Color color) {
			model = this is PowerConsumerInterface ? hb.PowerStorage : hb.mPreviewCube;
			size = Vector3.one;
			color = Color.white;
			return true;
		}
		
		public sealed override HoloMachineEntity CreateHolobaseEntity(Holobase hb) {
			GameObject go;
			Vector3 size;
			Color c;
			if (!setupHolobaseVisuals(hb, out go, out size, out c))
				return null;
			HolobaseEntityCreationParameters ecp = new HolobaseEntityCreationParameters(this);
			HolobaseVisualisationParameters hvp = ecp.AddVisualisation(go);
			hvp.Scale = size;
			hvp.Color = c;
			return hb.CreateHolobaseEntity(ecp);
		}
		
	}
}
