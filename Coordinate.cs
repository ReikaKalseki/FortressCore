﻿/*
 * Created by SharpDevelop.
 * User: Reika
 * Date: 04/11/2019
 * Time: 5:34 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
 
using System;
using System.IO;
using UnityEngine;

namespace ReikaKalseki.FortressCore
{
	public class Coordinate {
		
		public static readonly Coordinate ZERO = new Coordinate(0, 0, 0);
		
		public readonly long xCoord;
		public readonly long yCoord;
		public readonly long zCoord;
		
		public Coordinate(long x, long y, long z) {
			xCoord = x;
			yCoord = y;
			zCoord = z;
		}
		
		public Coordinate(Player e) : this(e.mnWorldX-WorldUtil.COORD_OFFSET, e.mnWorldY-WorldUtil.COORD_OFFSET, e.mnWorldZ-WorldUtil.COORD_OFFSET) {
		
		}
		
		public Coordinate(MobEntity e) : this(e.mnX-WorldUtil.COORD_OFFSET, e.mnY-WorldUtil.COORD_OFFSET, e.mnZ-WorldUtil.COORD_OFFSET) {
		
		}
		
		public Coordinate(SegmentEntity e) : this(e.mnX-WorldUtil.COORD_OFFSET, e.mnY-WorldUtil.COORD_OFFSET, e.mnZ-WorldUtil.COORD_OFFSET) {
		
		}
		
		public Coordinate(RaycastResult ray) : this(ray.mnHitX, ray.mnHitY, ray.mnHitZ) {
		
		}
		
		public override string ToString() {
			return "("+xCoord+", "+yCoord+", "+zCoord+")";
		}
		
		public override int GetHashCode() {
			return (int)(xCoord + (zCoord << 8) + (yCoord << 16)); //copied from DragonAPI
		}
		
		public override bool Equals(object o) {
			if (o is Coordinate) {
				Coordinate w = (Coordinate)o;
				return equals(w.xCoord, w.yCoord, w.zCoord);
			}
			return false;
		}

		public bool equals(long x, long y, long z) {
			return x == xCoord && y == yCoord && z == zCoord;
		}
		
		public long getTaxicabDistance(Coordinate other) {
			return other.xCoord-xCoord+other.yCoord-yCoord+other.zCoord-zCoord;
		}

		public Coordinate offset(long d) {
			return offset(d, d, d);
		}

		public Coordinate offset(long x, long y, long z) {
			return new Coordinate(xCoord+x, yCoord+y, zCoord+z);
		}

		public Coordinate toWorld(SegmentEntity e) {
			return offset(-e.mnX, -e.mnY, -e.mnZ);
		}
		
		public Coordinate fromRaw() {
			return offset(-WorldUtil.COORD_OFFSET, -WorldUtil.COORD_OFFSET, -WorldUtil.COORD_OFFSET);
		}

		public Vector3 asVector3() {
			return new Vector3(xCoord, yCoord, zCoord);
		}
		
		public Segment getSegment(Func<long, long, long, Segment> segmentGetter) {
			return segmentGetter(xCoord, yCoord, zCoord);
		}
		
		public ushort getBlock(Segment s) {
			return s.GetCube(xCoord, yCoord, zCoord);
		}
		
		public void write(BinaryWriter writer) {
			writer.Write(xCoord);
			writer.Write(yCoord);
			writer.Write(zCoord);
		}
		
		public static Coordinate read(BinaryReader reader) {
			return new Coordinate(reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt64());
		}
		
		public static Coordinate fromRawXYZ(long x, long y, long z) {
			return new Coordinate(x-WorldUtil.COORD_OFFSET, y-WorldUtil.COORD_OFFSET, z-WorldUtil.COORD_OFFSET);
		}
		
		public static Coordinate fromLook(LocalPlayerScript ep, bool toReal = true) {
			long x = ep.mPlayerBlockPicker.selectFaceX;
			long y = ep.mPlayerBlockPicker.selectFaceY;
			long z = ep.mPlayerBlockPicker.selectFaceZ;
			return toReal ? fromRawXYZ(x, y, z) : new Coordinate(x, y, z);
		}
		
		public static bool operator == (Coordinate leftSide, Coordinate rightSide) {
		    if (object.ReferenceEquals(null, leftSide))
		        return object.ReferenceEquals(null, rightSide);
		    if (object.ReferenceEquals(null, rightSide))
		        return object.ReferenceEquals(null, leftSide);
			return leftSide.Equals(rightSide);
		}
		
		public static bool operator != (Coordinate leftSide, Coordinate rightSide) {
			return !(leftSide == rightSide);
		}
		
		public static Coordinate operator + (Coordinate c, Coordinate offset) {
			return c.offset(offset.xCoord, offset.yCoord, offset.zCoord);
		}
		
		public static Coordinate operator * (Coordinate c, long scalar) {
			return new Coordinate(c.xCoord*scalar, c.yCoord*scalar, c.zCoord*scalar);
		}
	}
}
