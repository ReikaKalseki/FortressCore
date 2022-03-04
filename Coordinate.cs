/*
 * Created by SharpDevelop.
 * User: Reika
 * Date: 04/11/2019
 * Time: 5:34 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ReikaKalseki.RoomEnvironmentals
{
	/// <summary>
	/// Description of Coordinate.
	/// </summary>
	public class Coordinate {
		
		public readonly int xCoord;
		public readonly int yCoord;
		public readonly int zCoord;
		
		public Coordinate(int x, int y, int z) {
			xCoord = x;
			yCoord = y;
			zCoord = z;
		}
		
		public Coordinate(SegmentEntity e) : this((int)(e.mnX-WorldScript.mDefaultOffset), (int)(e.mnY-WorldScript.mDefaultOffset), (int)(e.mnZ-WorldScript.mDefaultOffset)) {
		
		}
		
		public override string ToString() {
			return xCoord+", "+yCoord+", "+zCoord;
		}
		
		public override int GetHashCode() {
			return xCoord + (zCoord << 8) + (yCoord << 16); //copied from DragonAPI
		}
		
		public override bool Equals(Object o) {
			if (o is Coordinate) {
				Coordinate w = (Coordinate)o;
				return equals(w.xCoord, w.yCoord, w.zCoord);
			}
			return false;
		}

		public bool equals(int x, int y, int z) {
			return x == xCoord && y == yCoord && z == zCoord;
		}
		
		public static bool operator == (Coordinate leftSide, Coordinate rightSide) {
			return leftSide.Equals(rightSide);
		}
		
		public static bool operator != (Coordinate leftSide, Coordinate rightSide) {
			return !leftSide.Equals(rightSide);
		}
	}
}
