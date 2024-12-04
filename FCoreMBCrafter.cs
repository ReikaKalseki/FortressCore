﻿/*
 * Created by SharpDevelop.
 * User: Reika
 * Date: 04/11/2019
 * Time: 11:28 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
//For data read/write methods
using System.Collections;
//Working with Lists and Collections
using System.Collections.Generic;
//Working with Lists and Collections
using System.Linq;
//More advanced manipulation of lists/collections
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;
//Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using ReikaKalseki.FortressCore;

namespace ReikaKalseki.FortressCore {
	
	public abstract class FCoreMBCrafter<M, R> : FCoreMBMachine<M>, PowerConsumerInterface where M : FCoreMBCrafter<M, R> where R : CraftData {
		
		public enum OperatingState {
			WaitingOnResources,
			OutOfPower,
			Processing,
			OutOfStorage
		}

		public float currentPower { get; private set; }

		public readonly float mrMaxPower;

		public readonly float mrMaxTransferRate;

		public readonly float powerPerSecond;

		private readonly List<R> recipes;

		private ItemBase mCreatedItem;

		private int mEntityVersion;

		private List<StorageMachineInterface> mAttachedHoppers = new List<StorageMachineInterface>();

		private List<MassStorageCrate> mAttachedMassStorage = new List<MassStorageCrate>();

		public int mnCurrentSideIndex { get; private set; }

		public int mnCurrentSide { get; private set; }

		public R currentRecipe { get; private set; }

		private int mnPreviousRecipeIndex;

		public float processTimer { get; private set; }

		public OperatingState mOperatingState { get; private set; }

		private int[][] mRecipeCounters;

		public float mrStateTimer { get; private set; }

		public byte mnAttachedHoppers { get; private set; }

		protected FCoreMBCrafter(ModCreateSegmentEntityParameters parameters, MultiblockData mb, float pps, float maxPower, float maxIO, List<R> li) : base(parameters, mb) {
			if (maxPower < pps)
				throw new Exception("Machine "+mb.name+" stores too little power to operate!");
			if (maxIO < pps)
				throw new Exception("Machine "+mb.name+" has too low power IO to operate continuously!");
			
			if (parameters.Value == mb.centerMeta || parameters.Value == mb.centerFlipMeta) {
				powerPerSecond = pps;
				mrMaxPower = maxPower;
				mrMaxTransferRate = maxIO;
				recipes = li;
				
				this.mRecipeCounters = new int[li.Count][];
				for (int i = 0; i < this.mRecipeCounters.Length; i++) {
					this.mRecipeCounters[i] = new int[li[i].Costs.Count];
				}
			}
			else {
				powerPerSecond = 0;
				maxPower = 0;
				mrMaxTransferRate = 0;
				recipes = new List<R>();
			}
		}

		public override void LowFrequencyUpdate() {
			base.LowFrequencyUpdate();
			if (this.mbIsCenter && WorldScript.mbIsServer) {
				int num = 1;
				if (this.mAttachedMassStorage.Count == 0 && this.mAttachedHoppers.Count == 0) {
					num = 84;
				}
				for (int i = 0; i < num; i++) {
					this.LookForMachines();
				}
				this.UpdateOperatingState();
			}
		}

		private void SetNewOperatingState(OperatingState newState) {
			this.mrStateTimer = 0f;
			this.mOperatingState = newState;
		}

		private void UpdateOperatingState() {
			switch (this.mOperatingState) {
				case OperatingState.WaitingOnResources:
					this.UpdateWaitingForResources();
					return;
				case OperatingState.OutOfPower:
					this.UpdateOutOfPower();
					return;
				case OperatingState.Processing:
					this.UpdateProcessing();
					return;
				case OperatingState.OutOfStorage:
					this.UpdateOutOfStorage();
					return;
				default:
					return;
			}
		}

		private void UpdateOutOfPower() {
			if (this.currentPower >= this.powerPerSecond * LowFrequencyThread.mrPreviousUpdateTimeStep) {
				this.SetNewOperatingState(OperatingState.Processing);
			}
		}

		private void InitCounters() {
			for (int i = 0; i < this.mRecipeCounters.Length; i++) {
				int[] array = this.mRecipeCounters[i];
				for (int j = 0; j < array.Length; j++) {
					if (recipes[i] == null) {
						FUtil.log("Error, recipe " + i + " was null?");
					}
					if (recipes[i].Costs[j] == null) {
						FUtil.log(string.Concat(new object[] {
							"Error, recipe ",
							i,
							" cost ",
							j,
							" was null?"
						}));
					}
					array[j] = (int)recipes[i].Costs[j].Amount;
				}
			}
		}

		private void UpdateWaitingForResources() {
			this.InitCounters();
			if (this.mAttachedHoppers.Count > 0) {
				for (int i = this.mAttachedHoppers.Count-1; i >= 0; i--) {
					StorageMachineInterface storageMachineInterface = this.mAttachedHoppers[i];
					if (storageMachineInterface == null || ((SegmentEntity)storageMachineInterface).mbDelete) {
						this.mAttachedHoppers.RemoveAt(i);
					}
					else {
						eHopperPermissions permissions = storageMachineInterface.GetPermissions();
						if (permissions == eHopperPermissions.RemoveOnly || permissions == eHopperPermissions.AddAndRemove) {
							storageMachineInterface.IterateContents(new IterateItem(this.IterateHopperItem), null);
						}
					}
				}
			}
			for (int j = 0; j < this.mRecipeCounters.Length; j++) {
				int num = (j + this.mnPreviousRecipeIndex + 1) % this.mRecipeCounters.Length;
				int[] array = this.mRecipeCounters[num];
				bool flag = true;
				for (int k = 0; k < array.Length; k++) {
					if (array[k] > 0) {
						flag = false;
						break;
					}
				}
				if (flag) {
					this.mnPreviousRecipeIndex = num;
					this.currentRecipe = recipes[num];
					processTimer = currentRecipe.CraftTime;
					this.SetNewOperatingState(OperatingState.Processing);
					this.RemoveIngredients();
					return;
				}
			}
			for (int l = 0; l < this.mRecipeCounters.Length; l++) {
				int[] array2 = this.mRecipeCounters[l];
				int num2 = 0;
				for (int m = 0; m < array2.Length; m++) {
					num2 += (int)(recipes[l].Costs[m].Amount - (uint)array2[m]);
				}
			}
		}

		private void RemoveIngredients() {
			int[] array = new int[this.currentRecipe.Costs.Count];
			for (int i = 0; i < array.Length; i++) {
				array[i] = (int)this.currentRecipe.Costs[i].Amount;
			}
			if (this.mAttachedHoppers.Count > 0) {
				for (int j = 0; j < this.mAttachedHoppers.Count; j++) {
					StorageMachineInterface storageMachineInterface = this.mAttachedHoppers[j];
					if (storageMachineInterface == null || ((SegmentEntity)storageMachineInterface).mbDelete) {
						this.mAttachedHoppers.RemoveAt(j);
					}
					else {
						for (int k = 0; k < this.currentRecipe.Costs.Count; k++) {
							if (array[k] > 0) {
								CraftCost craftCost = this.currentRecipe.Costs[k];
								if (craftCost.ItemType >= 0) {
									int num;
									if (craftCost.Amount > 1U) {
										num = storageMachineInterface.TryPartialExtractItems(this, craftCost.ItemType, array[k]);
									}
									else {
										num = storageMachineInterface.CountItems(craftCost.ItemType);
										if (num > 0) {
											bool flag = storageMachineInterface.TryExtractItems(this, craftCost.ItemType, 1);
										}
									}
									array[k] -= num;
								}
								else {
									int num2;
									if (craftCost.Amount > 1U) {
										num2 = storageMachineInterface.TryPartialExtractCubes(this, craftCost.CubeType, craftCost.CubeValue, array[k]);
									}
									else {
										num2 = storageMachineInterface.CountCubes(craftCost.CubeType, craftCost.CubeValue);
										if (num2 > 0) {
											bool flag = storageMachineInterface.TryExtractCubes(this, craftCost.CubeType, craftCost.CubeValue, 1);
										}
									}
									array[k] -= num2;
								}
							}
						}
					}
				}
			}
			for (int l = 0; l < array.Length; l++) {
				if (array[l] > 0) {
				}
			}
		}

		private bool IterateHopperItem(ItemBase item, object userState) {
			for (int i = 0; i < this.mRecipeCounters.Length; i++) {
				int[] array = this.mRecipeCounters[i];
				bool flag = false;
				bool flag2 = true;
				for (int j = 0; j < array.Length; j++) {
					CraftCost craftCost = recipes[i].Costs[j];
					if (!flag) {
						if (craftCost.CubeType != 0 && item.mType == ItemType.ItemCubeStack) {
							ItemCubeStack itemCubeStack = item as ItemCubeStack;
							if (itemCubeStack.mCubeType == craftCost.CubeType && itemCubeStack.mCubeValue == craftCost.CubeValue) {
								ARTHERPetSurvival.instance.GotOre(itemCubeStack.mCubeType);
								array[j] -= itemCubeStack.mnAmount;
								flag = true;
							}
						}
						else
						if (item.mnItemID == craftCost.ItemType) {
							array[j] -= ItemManager.GetCurrentStackSize(item);
							flag = true;
						}
					}
					if (array[j] > 0) {
						flag2 = false;
					}
				}
				if (flag2) {
					return false;
				}
			}
			return true;
		}

		private bool IterateCrateItem(ItemBase item) {
			for (int i = 0; i < this.mRecipeCounters.Length; i++) {
				int[] array = this.mRecipeCounters[i];
				bool flag = false;
				bool flag2 = true;
				for (int j = 0; j < array.Length; j++) {
					CraftCost craftCost = recipes[i].Costs[j];
					if (!flag) {
						if (craftCost.CubeType != 0 && item.mType == ItemType.ItemCubeStack) {
							ItemCubeStack itemCubeStack = item as ItemCubeStack;
							if (itemCubeStack.mCubeType == craftCost.CubeType && itemCubeStack.mCubeValue == craftCost.CubeValue) {
								array[j] -= itemCubeStack.mnAmount;
								flag = true;
							}
						}
						else
						if (item.mnItemID == craftCost.ItemType) {
							array[j] -= ItemManager.GetCurrentStackSize(item);
							flag = true;
						}
					}
					if (array[j] > 0) {
						flag2 = false;
					}
				}
				if (flag2) {
					return false;
				}
			}
			return true;
		}

		private void UpdateProcessing() {
			if (this.currentRecipe == null) {
				this.SetNewOperatingState(OperatingState.WaitingOnResources);
				return;
			}
			this.currentPower -= this.powerPerSecond * LowFrequencyThread.mrPreviousUpdateTimeStep;
			if (this.currentPower < 0f) {
				this.currentPower = 0f;
				this.SetNewOperatingState(OperatingState.OutOfPower);
				return;
			}
			this.processTimer -= LowFrequencyThread.mrPreviousUpdateTimeStep;
			if (this.processTimer <= 0f) {
				this.mCreatedItem = ItemManager.SpawnItem(this.currentRecipe.CraftableItemType);
				if (!this.AttemptToOffload()) {
					this.SetNewOperatingState(OperatingState.OutOfStorage);
				}
			}
		}

		private void UpdateOutOfStorage() {
			this.AttemptToOffload();
		}

		public bool AttemptToOffload() {
			if (this.mAttachedHoppers.Count > 0) {
				for (int i = mAttachedHoppers.Count-1; i >= 0; i--) {
					StorageMachineInterface smi = this.mAttachedHoppers[i];
					if (smi == null || ((SegmentEntity)smi).mbDelete) {
						this.mAttachedHoppers.RemoveAt(i);
					}
					else {
						eHopperPermissions perms = smi.GetPermissions();
						if ((perms == eHopperPermissions.AddOnly || perms == eHopperPermissions.AddAndRemove) && !smi.IsFull() && smi.TryInsert(this, this.mCreatedItem)) {
							this.mCreatedItem = null;
							this.SetNewOperatingState(OperatingState.WaitingOnResources);
							return true;
						}
					}
				}
			}
			return false;
		}

		private void RoundRobinSide(out int y, out int x, out int z) {
			int num;
			if (this.mnCurrentSide == 0) {
				y = this.mnCurrentSideIndex / machineBounds.depth + machineBounds.minY;
				x = machineBounds.minX - 1;
				z = this.mnCurrentSideIndex % machineBounds.depth + machineBounds.minZ;
				num = machineBounds.height * machineBounds.depth;
			}
			else
			if (this.mnCurrentSide == 1) {
				y = this.mnCurrentSideIndex / machineBounds.depth + machineBounds.minY;
				x = machineBounds.maxX + 1;
				z = this.mnCurrentSideIndex % machineBounds.depth + machineBounds.minZ;
				num = machineBounds.height * machineBounds.depth;
			}
			else
			if (this.mnCurrentSide == 2) {
				y = this.mnCurrentSideIndex / machineBounds.width + machineBounds.minY;
				x = this.mnCurrentSideIndex % machineBounds.width + machineBounds.minX;
				z = machineBounds.maxZ + 1;
				num = machineBounds.height * machineBounds.width;
			}
			else
			if (this.mnCurrentSide == 3) {
				y = this.mnCurrentSideIndex / machineBounds.width + machineBounds.minY;
				x = this.mnCurrentSideIndex % machineBounds.width + machineBounds.minX;
				z = machineBounds.minZ - 1;
				num = machineBounds.height * machineBounds.width;
			}
			else
			if (this.mnCurrentSide == 4) {
				y = machineBounds.minY - 1;
				x = this.mnCurrentSideIndex / machineBounds.depth + machineBounds.minX;
				z = this.mnCurrentSideIndex % machineBounds.depth + machineBounds.minZ;
				num = machineBounds.width * machineBounds.depth;
			}
			else {
				y = machineBounds.maxY + 1;
				x = this.mnCurrentSideIndex / machineBounds.depth + machineBounds.minX;
				z = this.mnCurrentSideIndex % machineBounds.depth + machineBounds.minZ;
				num = machineBounds.width * machineBounds.depth;
			}
			this.mnCurrentSideIndex++;
			if (this.mnCurrentSideIndex == num) {
				this.mnCurrentSideIndex = 0;
				this.mnCurrentSide = (this.mnCurrentSide + 1) % 6;
			}
		}

		private void LookForMachines() {
			int x;
			int y;
			int z;
			this.RoundRobinSide(out y, out x, out z);
			long dx = (long)x + this.mnX;
			long dy = (long)y + this.mnY;
			long dz = (long)z + this.mnZ;
			Segment segment = base.AttemptGetSegment(dx, dy, dz);
			if (segment == null) {
				return;
			}
			SegmentEntity e = segment.SearchEntity(dx, dy, dz);
			segment.GetCube(dx, dy, dz);
			if (e is StorageMachineInterface) {
				this.AddAttachedHopper((StorageMachineInterface)e);
			}
			else if (e is MassStorageCrate) {
				this.AddAttachedMassStorage((MassStorageCrate)e);
			}
		}

		private void AddAttachedHopper(StorageMachineInterface smi) {
			for (int i = 0; i < this.mAttachedHoppers.Count; i++) {
				StorageMachineInterface storageMachineInterface = this.mAttachedHoppers[i];
				if (storageMachineInterface != null) {
					if ((storageMachineInterface as SegmentEntity).mbDelete) {
						this.mAttachedHoppers.RemoveAt(i);
						i--;
					}
					else if (storageMachineInterface == smi) {
						smi = null;
					}
				}
			}
			if (smi != null) {
				this.mAttachedHoppers.Add(smi);
			}
		}

		private void AddAttachedMassStorage(MassStorageCrate crate) {
			for (int i = 0; i < this.mAttachedMassStorage.Count; i++) {
				MassStorageCrate massStorageCrate = this.mAttachedMassStorage[i];
				if (massStorageCrate != null) {
					if (massStorageCrate.mbDelete) {
						this.mAttachedMassStorage.RemoveAt(i);
						i--;
					}
					else if (massStorageCrate == crate) {
						crate = null;
					}
				}
			}
			if (crate != null) {
				this.mAttachedMassStorage.Add(crate);
			}
		}

		public sealed override int GetVersion() {
			return this.mEntityVersion;
		}

		public override void Write(BinaryWriter writer) {
			base.Write(writer);
			if (!this.mbIsCenter) {
				return;
			}
			writer.Write(this.mEntityVersion);
			writer.Write(this.currentPower);
			writer.Write((byte)this.mOperatingState);
			if (this.mAttachedHoppers != null) {
				writer.Write((byte)this.mAttachedHoppers.Count);
				return;
			}
			writer.Write(0);
		}

		public override void Read(BinaryReader reader, int entityVersion) {
			base.Read(reader, entityVersion);
			if (!this.mbIsCenter) {
				return;
			}
			this.mEntityVersion = reader.ReadInt32();
			this.currentPower = reader.ReadSingle();
			this.mOperatingState = (OperatingState)reader.ReadByte();
			if (this.currentPower < 0f) {
				this.currentPower = 0f;
			}
			if (this.currentPower > this.mrMaxPower) {
				this.currentPower = this.mrMaxPower;
			}
			if (entityVersion >= 1) {
				this.mnAttachedHoppers = reader.ReadByte();
			}
		}

		public float GetRemainingPowerCapacity() {
			if (this.mLinkedCenter != null) {
				return this.mLinkedCenter.GetRemainingPowerCapacity();
			}
			return this.mrMaxPower - this.currentPower;
		}

		public float GetMaximumDeliveryRate() {
			return this.mrMaxTransferRate;
		}

		public float GetMaxPower() {
			if (this.mLinkedCenter != null) {
				return this.mLinkedCenter.GetMaxPower();
			}
			return this.mrMaxPower;
		}

		public bool DeliverPower(float amount) {
			if (this.mLinkedCenter != null) {
				return this.mLinkedCenter.DeliverPower(amount);
			}
			if (amount > this.GetRemainingPowerCapacity()) {
				return false;
			}
			this.currentPower += amount;
			this.MarkDirtyDelayed();
			return true;
		}

		public bool WantsPowerFromEntity(SegmentEntity entity) {
			return this.mLinkedCenter == null || this.mLinkedCenter.WantsPowerFromEntity(entity);
		}

		public override string GetPopupText() {
			if (this.mLinkedCenter != null) {
				return this.mLinkedCenter.GetPopupText();
			}
			string text = multiblockData.name;
			string text2 = text;
			text = string.Concat(new string[] {
				text2,
				"\nPower: ",
				this.currentPower.ToString("N0"),
				"/",
				this.mrMaxPower.ToString("N0")
			});
			text = text + "\nNeeds " + this.powerPerSecond.ToString() + " PPS";
			if (WorldScript.mbIsServer) {
				if (this.mAttachedMassStorage.Count == 0 && this.mAttachedHoppers.Count == 0) {
					text += "\nNo Attached Storage Hoppers or Mass Storage found";
				}
				else {
					if (this.mAttachedMassStorage.Count > 0) {
						object obj = text;
						text = string.Concat(new object[] {
							obj,
							"\nAttached to ",
							this.mAttachedMassStorage.Count,
							" Mass Storage units."
						});
					}
					if (this.mAttachedHoppers.Count > 0) {
						object obj2 = text;
						text = string.Concat(new object[] {
							obj2,
							"\nAttached to ",
							this.mAttachedHoppers.Count,
							" Storage Hoppers."
						});
					}
				}
			}
			else if (this.mnAttachedHoppers == 0) {
				text += "\nNo Attached Storage Hoppers found";
			}
			else {
				object obj3 = text;
				text = string.Concat(new object[] {
					obj3,
					"\nAttached to ",
					this.mnAttachedHoppers,
					" Storage Hoppers."
				});
			}
			text = text + "\nState: " + this.mOperatingState;
			if (this.mOperatingState == OperatingState.Processing) {
				if (this.currentRecipe != null) {
					text = text + "\nProcessing: " + this.currentRecipe.CraftedName+", "+this.processTimer.ToString("N1") + "s";
				}
			}
			if (this.mOperatingState == OperatingState.WaitingOnResources) {
				text += "\nMissing ingredients.";
			}
			return text;
		}

	
	}

}