using System;

using System.Collections.Generic;
using System.Collections;

using System.IO;

using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public abstract class FCoreMBMachine<M> : FCoreMachine where M : FCoreMBMachine<M> {
		
		public readonly MultiblockData multiblockData;

		public bool mbIsCenter { get; private set; }

		public M mLinkedCenter { get; private set; }

		public MachineEntity.MBMState mMBMState { get; private set; }

		public long mLinkX { get; private set; }

		public long mLinkY { get; private set; }

		public long mLinkZ { get; private set; }

		protected string FriendlyState = "Unknown state!";
		
		protected Dimensions machineBounds { get; private set; }
		
		protected FCoreMBMachine(MultiblockData mb, eSegmentEntity type, SpawnableObjectEnum objectType, long x, long y, long z, ushort cube, byte flags, ushort value, Vector3 position, Segment segment) : base(type, objectType, x, y, z, cube, flags, value, position, segment) {
			multiblockData = mb;
			this.mMBMState = MBMState.WaitingForLink;
			if (value == mb.centerMeta || value == mb.centerFlipMeta) {
				setupCenter();
			}
			else {
				mbNeedsLowFrequencyUpdate = false;
				mbNeedsUnityUpdate = false;
			}
		}

		protected FCoreMBMachine(MachineEntityCreationParameters parameters, MultiblockData mb) : base(parameters) {
			multiblockData = mb;
			this.mMBMState = MBMState.WaitingForLink;
			if (parameters.Value == mb.centerMeta || parameters.Value == mb.centerFlipMeta) {
				setupCenter();
			}
			else {
				mbNeedsLowFrequencyUpdate = false;
				mbNeedsUnityUpdate = false;
			}
		}

		public sealed override void SpawnGameObject() {
			if (multiblockData.prefab != null)
				mObjectType = multiblockData.prefab.model;
			if (this.mbIsCenter) {
				base.SpawnGameObject();
			}
		}
		
		private void setupCenter() {
			this.mbIsCenter = true;
			this.mMBMState = MBMState.ReacquiringLink;
			this.RequestLowFrequencyUpdates();
			this.mbNeedsUnityUpdate = true;
			
			machineBounds = multiblockData.getSize(mValue);
		}

		private void RequestLowFrequencyUpdates() {
			if (!this.mbNeedsLowFrequencyUpdate) {
				this.mbNeedsLowFrequencyUpdate = true;
				this.mSegment.mbNeedsLowFrequencyUpdate = true;
				if (!this.mSegment.mbIsQueuedForUpdate) {
					WorldScript.instance.mSegmentUpdater.AddSegment(this.mSegment);
				}
			}
		}

		public sealed override bool ShouldSave() {
			return true;
		}

		public sealed override bool ShouldNetworkUpdate() {
			return true;
		}

		public override void Write(BinaryWriter writer) {
			writer.Write(this.mbIsCenter);
			writer.Write(this.mLinkX);
			writer.Write(this.mLinkY);
			writer.Write(this.mLinkZ);
		}

		public override void Read(BinaryReader reader, int entityVersion) {
			this.mbIsCenter = reader.ReadBoolean();
			this.mLinkX = reader.ReadInt64();
			this.mLinkY = reader.ReadInt64();
			this.mLinkZ = reader.ReadInt64();
			this.mMBMState = MBMState.ReacquiringLink;
			if (this.mbIsCenter)
				this.RequestLowFrequencyUpdates();
		}

		public override void LowFrequencyUpdate() {
			if (this.mbIsCenter) {
				if (mMBMState == MachineEntity.MBMState.ReacquiringLink) {
					this.LinkMultiBlockMachine();
				}
				if (mMBMState == MachineEntity.MBMState.Delinking) {
					this.DeconstructMachineFromCentre(null);
				}
			}
		}
		
		public override sealed string getName() {
			return multiblockData.name;
		}
		
		public override sealed string GetPopupText() {
			if (this.mLinkedCenter != null) {
				return this.mLinkedCenter.GetPopupText();
			}
			return base.GetPopupText()+"\n"+GetUIText();
		}
		
		public virtual string GetUIText() {
			return "";
		}
		
		protected override bool setupHolobaseVisuals(Holobase hb, out GameObject model, out Vector3 size, out Color color) {
			if (!base.setupHolobaseVisuals(hb, out model, out size, out color))
				return false;
			if (!mbIsCenter)
				return false;
			size = new Vector3((float)this.machineBounds.width, (float)this.machineBounds.height, (float)this.machineBounds.depth);
			return true;
		}

		public override sealed void OnDelete() {
			base.OnDelete();
			if (mMBMState != MachineEntity.MBMState.Linked) {
				if (mMBMState != MachineEntity.MBMState.Delinked) {
					FUtil.log("Deleted crafter while in state " + this.mMBMState);
				}
				return;
			}
			if (WorldScript.mbIsServer) {
				ItemManager.DropNewCubeStack(eCubeTypes.MachinePlacementBlock, multiblockData.placerMeta, 1, this.mnX, this.mnY, this.mnZ, Vector3.zero);
			}
			this.mMBMState = MBMState.Delinking;
			if (this.mbIsCenter) {
				this.DeconstructMachineFromCentre((M)this);
				return;
			}
			if (this.mLinkedCenter == null) {
				FUtil.log("Error, "+multiblockData.name+" had no linked centre, so cannot destroy linked centre?");
				return;
			}
			this.mLinkedCenter.DeconstructMachineFromCentre((M)this);
		}

		private void DeconstructMachineFromCentre(M deletedBlock) {
			FUtil.log("Deconstructing "+multiblockData.name+" into placement blocks");
			for (int i = machineBounds.minY; i <= machineBounds.maxY; i++) {
				for (int j = machineBounds.minZ; j <= machineBounds.maxZ; j++) {
					for (int k = machineBounds.minX; k <= machineBounds.maxX; k++) {
						long dx = this.mnX + (long)k;
						long dy = this.mnY + (long)i;
						long dz = this.mnZ + (long)j;
						if ((k != 0 || i != 0 || j != 0) && (deletedBlock == null || dx != deletedBlock.mnX || dy != deletedBlock.mnY || dz != deletedBlock.mnZ)) {
							Segment segment = WorldScript.instance.GetSegment(dx, dy, dz);
							if (segment == null || !segment.mbInitialGenerationComplete || segment.mbDestroyed) {
								this.mMBMState = MBMState.Delinking;
								this.RequestLowFrequencyUpdates();
								return;
							}
							ushort cube = segment.GetCube(dx, dy, dz);
							if (cube == multiblockData.blockID) {
								M crafter = segment.FetchEntity(eSegmentEntity.Mod, dx, dy, dz) as M;
								if (crafter == null) {
									FUtil.log("Failed to refind a "+multiblockData.name+" entity? wut?");
								}
								else {
									crafter.DeconstructSingleBlock();
								}
							}
						}
					}
				}
			}
			if (this != deletedBlock) {
				this.DeconstructSingleBlock();
			}
		}

		private void DeconstructSingleBlock() {
			this.mMBMState = MBMState.Delinked;
			WorldScript.instance.BuildFromEntity(this.mSegment, this.mnX, this.mnY, this.mnZ, eCubeTypes.MachinePlacementBlock, multiblockData.placerMeta);
		}

		private void LinkMultiBlockMachine() {
			for (int j = machineBounds.minY; j <= machineBounds.maxY; j++) {
				for (int i = machineBounds.minZ; i <= machineBounds.maxZ; i++) {
					for (int k = machineBounds.minX; k <= machineBounds.maxX; k++) {
						long dx = this.mnX + (long)k;
						long dy = this.mnY + (long)j;
						long dz = this.mnZ + (long)i;
						if (k != 0 || i != 0 || j != 0) {
							Segment segment = base.AttemptGetSegment(dx, dy, dz);
							if (segment == null) {
								return;
							}
							ushort cube = segment.GetCube(dx, dy, dz);
							if (cube == multiblockData.blockID) {
								M crafter = segment.FetchEntity(eSegmentEntity.Mod, dx, dy, dz) as M;
								if (crafter == null) {
									return;
								}
								if (crafter.mMBMState != MachineEntity.MBMState.Linked || crafter.mLinkedCenter != this) {
									if (crafter.mMBMState == MachineEntity.MBMState.ReacquiringLink && crafter.mLinkX == this.mnX && crafter.mLinkY == this.mnY) {
										long num4 = crafter.mLinkZ;
										long mnZ = this.mnZ;
									}
									crafter.mMBMState = MBMState.Linked;
									crafter.AttachToCentreBlock((M)this);
								}
							}
						}
					}
				}
			}
			this.ContructionFinished();
			base.DropExtraSegments(null);
		}

		private void ContructionFinished() {
			this.FriendlyState = multiblockData.name+" Constructed!";
			this.mMBMState = MBMState.Linked;
			this.mSegment.RequestRegenerateGraphics();
			this.MarkDirtyDelayed();
		}

		private void AttachToCentreBlock(M centerBlock) {
			if (centerBlock == null) {
				FUtil.log("Error, can't set side - requested centre is null!");
			}
			this.mMBMState = MBMState.Linked;
			if (this.mLinkX != centerBlock.mnX) {
				this.MarkDirtyDelayed();
				this.mSegment.RequestRegenerateGraphics();
			}
			this.mLinkedCenter = centerBlock;
			this.mLinkX = centerBlock.mnX;
			this.mLinkY = centerBlock.mnY;
			this.mLinkZ = centerBlock.mnZ;
		}
		
	}
}
