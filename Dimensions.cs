/*
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
	public class Dimensions {
		
		public static readonly Dimensions BLOCK = new Dimensions(1, 1, 1);
		
		public readonly int width;
		public readonly int height;
		public readonly int depth;
		
		public int volume { get { return width*height*depth; } }
		
		public int minX { get { return -(width-1)/2; } }
		public int minY { get { return -(height-1)/2; } }
		public int minZ { get { return -(depth-1)/2; } }
		public int maxX { get { return width/2; } }
		public int maxY { get { return height/2; } }
		public int maxZ { get { return depth/2; } }
		
		//these are WTF but copying from vanilla
		public int outerX { get { return width/2*2; } }
		public int outerY { get { return height/2*2; } }
		public int outerZ { get { return depth/2*2; } }
		
		public bool isAsymmetric { get { return width != depth; } }
		
		public Dimensions flip { get { return new Dimensions(depth, height, width); } }
		
		public Dimensions(int x, int y, int z) {
			width = x;
			height = y;
			depth = z;
		}
		
		public override string ToString() {
			return width+"x"+height+"x"+depth;
		}
		
		public override bool Equals(object o) {
			if (o is Dimensions) {
				Dimensions w = (Dimensions)o;
				return equals(w.width, w.height, w.depth);
			}
			return false;
		}

		public bool equals(int x, int y, int z) {
			return x == width && y == height && z == depth;
		}
		
		public void write(BinaryWriter writer) {
			writer.Write(width);
			writer.Write(height);
			writer.Write(depth);
		}
		
		public static Dimensions read(BinaryReader reader) {
			return new Dimensions(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
		}
		
		public static bool operator == (Dimensions leftSide, Dimensions rightSide) {
		    if (object.ReferenceEquals(null, leftSide))
		        return object.ReferenceEquals(null, rightSide);
		    if (object.ReferenceEquals(null, rightSide))
		        return object.ReferenceEquals(null, leftSide);
			return leftSide.Equals(rightSide);
		}
		
		public static bool operator != (Dimensions leftSide, Dimensions rightSide) {
			return !(leftSide == rightSide);
		}
		
		public static Dimensions operator * (Dimensions c, int scalar) {
			return new Dimensions(c.width*scalar, c.height*scalar, c.depth*scalar);
		}
	}
}
