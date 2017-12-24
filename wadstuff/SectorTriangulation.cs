using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This handles triangulating a sector from a map.

It will give up and return null when a sector cannot be triangulated properly.
There are still situations to add to make sure it can handle anything that is thrown at it.
It does the best it can.
*/

public class SectorPolygon {
	public List<Vector2> points;
	public List<int> triangles;

	public SectorPolygon() {
		points = new List<Vector2>();
		triangles = new List<int>();
	}

	public List<int> GetOffsetTriangles(int offset) {
		List<int> output = new List<int>();
		for (int i = 0; i < triangles.Count; i++) {
			output.Add(triangles[i] + offset);
		}
		return output;
	}

	public List<Vector3> PointsToVector3(float z) {
		List<Vector3> output = new List<Vector3>();
		for (int i = 0; i < points.Count; i++) {
			output.Add(new Vector3(points[i].x, z, points[i].y));
		}
		return output;
	}

	public bool ThingInside(Thing thing) {
		if (SectorTriangulation.PointInPolygon(new Vector2(thing.x, thing.y), points)) {
			return true;
		}
		return false;
	}
}

public class SectorTriangulation {

	private WadFile wad;
	private MapData map;

	private class SectorIsland {
		public List<Vector2> shell;
		public List<List<Vector2>> holes;

		public SectorIsland() {
			shell = new List<Vector2>();
			holes = new List<List<Vector2>>();
		}

		public void OrderHoles() {
			// Sort the holes so the outermost holes are placed first.
			// We sort by the value of the rightmost vertex
			List<List<Vector2>> orderedHoles = new List<List<Vector2>>();

			List<float> holeValue = new List<float>();
			for (int i = 0; i < holes.Count; i++) {
				holeValue.Add(holes[i][RightmostVertex(holes[i])].x);
			}

			while (holes.Count > 1) {
				int highIndex = 0;
				for (int i = 1; i < holes.Count; i++) {
					if (holeValue[i] > holeValue[highIndex]) highIndex = i;
				}
				orderedHoles.Add(holes[highIndex]);
				holes.RemoveAt(highIndex);
				holeValue.RemoveAt(highIndex);
			}

			// Add the final one
			orderedHoles.Add(holes[0]);

			holes = orderedHoles;
		}

		public int RightmostVertex(List<Vector2> polygon) {
			int output = 0;
			for (int i = 1; i < polygon.Count; i++) {
				if (polygon[i].x > polygon[output].x) {
					output = i;
				}
			}
			return output;
		}

		public List<Vector2> Cut() {
			// If there are no holes then it doesn't need cutting.
			if (holes.Count == 0) return shell;

			// Order the holes before cutting
			OrderHoles();

			int safe = 100;
			while (holes.Count > 0 && safe >= 0) { // be careful!!!
				safe -= 1;

				List<Vector2> hole = holes[0];

				// Step 1: Find maximum x-value point in hole
				int rIndex = RightmostVertex(hole);

				Vector2 hPoint = hole[rIndex];
				Vector2 hxPoint = new Vector2(hPoint.x + 10000f, hPoint.y);
				int closestLine = -1;

				// Step 2: Intersect the ray M + t(1, 0) with all directed edges hVi
				// , Vi+1i of the outer polygon for which M is to
				// the left of the line containing the edge (M is inside the outer polygon). Let I be the closest visible
				// point to M on this ray.
				float minDist = 100000f;
				float dist;
				Vector2 closePoint;
				for (int j = 0; j < shell.Count; j++) {
					int j2 = (j + 1) % shell.Count;

					if (shell[j].x > hPoint.x || shell[j2].x > hPoint.x) {

						if (shell[j].y == hPoint.y) {

							dist = Vector2.Distance(shell[j],hPoint);
							if (dist < minDist) {
								//Debug.Log(dist + " newmin: " + minDist);
								minDist = dist;
								closestLine = j;
								closePoint = shell[j];
							}

						} 
						if (LinesIntersect(hPoint, hxPoint, shell[j], shell[j2])) {
							Vector2 inter = FindIntersection(hPoint, hxPoint, shell[j], shell[j2]);
							dist = Vector2.Distance(inter, hPoint);
							
							if (dist < minDist) {
								//Debug.Log(dist + " " + minDist);
								minDist = dist;
								closestLine = j;
								closePoint = inter;
							}
						}
					}
				}

				if (closestLine == -1) {
					Debug.LogError("SectorIsland.Cut(): Couldn't intersect with shell?");
					//Debug.Log(hPoint);
					return null;
				}

				Vector2 shellPoint = shell[closestLine];
				Vector2 shellPoint2 = shell[(closestLine+1)%shell.Count];

				// Step 3: check if that new line intersects any holes
				bool topIntersectsHoles = false;
				bool botIntersectsHoles = false;
				for (int j = 0; j < holes.Count; j++) {
					for (int k = 0; k < holes[j].Count; k++) {
						int k2 = (k+1)%holes[j].Count;
						if (LinesIntersect(hPoint, shellPoint, holes[j][k], holes[j][k2])) {
							topIntersectsHoles = true;
						} 
						if (LinesIntersect(hPoint, shellPoint2, holes[j][k], holes[j][k2])) {
							botIntersectsHoles = true;
						} 
					}
				}

				if (topIntersectsHoles == false) {
					MakeCut(0, closestLine, rIndex);
				} else if (botIntersectsHoles == false) {
					MakeCut(0, (closestLine+1) % shell.Count, rIndex);
				} else {
					// can't do it for now
					Debug.LogWarning("Line we used intersected holes");
					//Debug.Log(hPoint + " " + shellPoint);
					return null;
				}
			}

			if (safe <= 0) {
				Debug.LogError("Safety loop caught break while too long");
				return null;
			}

			// Not Implemented
			return shell;
		}

		private void MakeCut(int holeIndex, int shellPointIndex, int holePointIndex) {
			if (IsClockwise(holes[holeIndex]) == IsClockwise(shell)) {
				holes[holeIndex].Reverse();
				holePointIndex = holes[holeIndex].Count - (holePointIndex + 1);
			}

			Vector2 sp = new Vector2(shell[shellPointIndex].x, shell[shellPointIndex].y);
			for (int i = 0; i < holes[holeIndex].Count; i++) {
				shell.Insert(shellPointIndex + i, holes[holeIndex][(i+holePointIndex) % holes[holeIndex].Count]);
			}
			shell.Insert(shellPointIndex + holes[holeIndex].Count, holes[holeIndex][holePointIndex]);
			shell.Insert(shellPointIndex, sp);
			holes.RemoveAt(holeIndex);
		}

	}

	public SectorTriangulation(MapData map) {
		this.map = map;
	}

	public List<SectorPolygon> Triangulate(int sector, bool benchmark = false) {

		float time = 0f;
		if (benchmark) {
			time = Time.realtimeSinceStartup;
		}
		int i;

		// Trace sector lines 
		List<List<Vector2>> polygons = TraceLines(sector);

		//Debug.Log("Polygons: " + polygons.Count);

		// Determine islands
		if (polygons.Count == 0) return null;
		List<SectorIsland> islands = BuildIslands(polygons);

		// Cut islands
		List<List<Vector2>> cutPolygons = new List<List<Vector2>>();
		for (i = 0; i < islands.Count; i++) {
			List<Vector2> cut = islands[i].Cut();
			if (cut != null) cutPolygons.Add(cut);	
		}

		// Ear clip
		List<SectorPolygon> output = new List<SectorPolygon>();

		for (i = 0; i < cutPolygons.Count; i++) {
			SectorPolygon sp = EarClip(cutPolygons[i]);
			if (sp != null) output.Add(sp);
		}

		// for (i = 0; i < cutPolygons.Count; i++) {
		// 	int conv = IsConvex(cutPolygons[i]);
		// 	if (conv != 0) {
		// 		if (conv == -1) cutPolygons[i].Reverse();
		// 		output.AddRange(cutPolygons[i]);
		// 	}
		// }
		
		// Output

		if (benchmark) {
			Debug.Log("Triangulation time: "+(Time.realtimeSinceStartup - time));
		}

		return output;
	}

	private List<List<Vector2>> TraceLines(int sector) {

		List<Linedef> lines = GetSectorLinedefs(sector);

		List<List<Vector2>> output = new List<List<Vector2>>();

		int i;
		int safe1 = 1000;
		int safe2 = 1;

		// First check all vertexes reference two linedefs! This is to find unclosed sectors
		Dictionary<int, int> vertexLines = new Dictionary<int, int>();
		for (int v = 0; v < lines.Count; v++) {
			if (!vertexLines.ContainsKey(lines[v].start)) {
				vertexLines.Add(lines[v].start, 1);
			} else {
				vertexLines[lines[v].start] += 1;
			}

			if (!vertexLines.ContainsKey(lines[v].end)) {
				vertexLines.Add(lines[v].end, 1);
			} else {
				vertexLines[lines[v].end] += 1;
			}
		}

		foreach (KeyValuePair<int, int> entry in vertexLines) {
			if (vertexLines[entry.Key] < 2) {
				Debug.LogError("Unclosed sector: "+sector);
				break;
			}
		}

		while (lines.Count > 0 && safe1 > 0) { // be careful
			safe1--;

			List<int> trace = new List<int>();

			Linedef line = lines[0];
			lines.RemoveAt(0);

			trace.Add(line.start);
			int next = line.end;
			
			safe2 = 1000;
			while ( trace[0] != next && safe2 > 0) { // be careful!!!!
				safe2--;
				for (i = 0; i < lines.Count; i++) {
					if (lines[i].start == next) {
						trace.Add(next);
						next = lines[i].end;
						lines.RemoveAt(i);
						break;
					}
					if (lines[i].end == next) {
						trace.Add(next);
						next = lines[i].start;
						lines.RemoveAt(i);
						break;
					}
				}
			}

			// Convert it to vector2 for actual use.
			// I used the index above for speed.
			List<Vector2> vector2trace = new List<Vector2>();
			for (i = 0; i < trace.Count; i++) {
				vector2trace.Add(VertexToVector2(trace[i]));
			}
			output.Add(vector2trace);
		}

		if (safe1 <= 0) Debug.LogError("First while loop exceeded limit!");
		if (safe2 <= 0) { 
			Debug.LogError("Second while loop exceeded limit! Sector: "+sector+ " Lines left: "+lines.Count);
		}

		return output;
	}

	private List<Linedef> GetSectorLinedefs(int sector) {
		List<Linedef> output = new List<Linedef>();
		List<int> sidedefs = GetSectorSidedefs(sector);

		for (int i = 0; i < map.linedefs.Count; i++) {
			if (sidedefs.Contains(map.linedefs[i].front) || sidedefs.Contains(map.linedefs[i].back)) {
				// We need to ignore linedefs that have the same front and back sector. 
				// No need to trace them, and it'll just cause problems.
				int fs = map.sidedefs[map.linedefs[i].front].sector;
				int bs;
				if (map.linedefs[i].back != -1) {
					bs = map.sidedefs[map.linedefs[i].back].sector;
				} else {
					bs = -1;
				}
				if (fs != bs) {
					output.Add(map.linedefs[i]);
				}
			}
		}

		return output;
	}

	private List<int> GetSectorSidedefs(int sector) {
		List<int> output = new List<int>();

		for (int i = 0; i < map.sidedefs.Count; i++) {
			if (map.sidedefs[i].sector == sector) {
				output.Add(i);
			}
		}

		return output;
	}

	private List<SectorIsland> BuildIslands(List<List<Vector2>> polygons) {

		List<SectorIsland> output = new List<SectorIsland>();

		// // If there's only one polygon, then it must be a shell.
		// if (polygons.Count == 1) {
		// 	SectorIsland ssi = new SectorIsland();
		// 	ssi.shell = polygons[0];
		// 	output.Add(ssi);
		// 	return output;
		// }

		
		SectorIsland si = new SectorIsland();
		si.shell = polygons[0];
		polygons.RemoveAt(0);
		output.Add(si);

		int safe = 10000;
		while (polygons.Count > 0 && safe >= 0) { // be careful!
			safe --;
			bool done = false;

			// Debug.Log(output.Count);
			// Debug.Log(polygons[0].Count);

			for (int i = 0; i < output.Count; i++) {

				if (PointInPolygon(polygons[0][0], 
					output[i].shell)) {
					output[i].holes.Add(polygons[0]);
					polygons.RemoveAt(0);
					done = true;
				} else {
					if (output[i].holes.Count == 0) {
						if (PointInPolygon(output[i].shell[0], polygons[0])) {
							output[i].holes.Add(output[i].shell);
							output[i].shell = polygons[0];
							polygons.RemoveAt(0);
							done = true;
						}
					}
				}

				if (polygons.Count == 0) {
					//Debug.Log(output[i].shell.Count);
					break;
				}
			}

			if (done == false) {
				// If its not in any existing islands, create a new island
				si = new SectorIsland();
				si.shell = polygons[0];
				output.Add(si);
				polygons.RemoveAt(0);
			}

		}

		if (safe <= 0) {
			Debug.LogError("BuildIslands: While loop broke safety check!");
		}

		return output;
	}

	public static bool PointInPolygon(Vector2 point, List<Vector2> polygon) {
		int crosses = 0;
		Vector2 leftPoint = new Vector2(point.x - 10000f, point.y);
		for (int i = 0; i < polygon.Count; i++) {
			int i2 = (i + 1) % polygon.Count;
			if (LinesIntersect(leftPoint, point, polygon[i], polygon[i2])) {
				crosses += 1;
			}
		}

		return (crosses % 2 == 1);
	}

	private static bool LinesIntersect(Vector2 A, Vector2 B, Vector2 C, Vector2 D) {
		return (CCW(A,C,D) != CCW(B,C,D)) && (CCW(A,B,C) != CCW(A,B,D));
	}

	private static bool LinesIntersect(List<Vector2> list, int a, int b, int c, int d) {
		Vector2 A = list[a % list.Count];
		Vector2 B = list[b % list.Count];
		Vector2 C = list[c % list.Count];
		Vector2 D = list[d % list.Count];
		//Debug.Log(A + " " + B + " " + C + " " + D);
		return (CCW(A,C,D) != CCW(B,C,D) && CCW(A,B,C) != CCW(A,B,D));
	}

	private static Vector2 FindIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4) {
		 // Get the segments' parameters.
	    float dx12 = p2.x - p1.x;
	    float dy12 = p2.y - p1.y;
	    float dx34 = p4.x - p3.x;
	    float dy34 = p4.y - p3.y;

	    // Solve for t1 and t2
	    float denominator = (dy12 * dx34 - dx12 * dy34);

	    float t1 =
	        ((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34)
	            / denominator;

	    float t2 =
	        ((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12)
	            / -denominator;

	    // Find the point of intersection.
	    return new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);
	}

	private static bool CCW(Vector2 A, Vector2 B, Vector2 C) {
		return ((C.y-A.y) * (B.x-A.x) > (B.y-A.y) * (C.x-A.x));
	}

	private SectorPolygon EarClip(List<Vector2> polygon) {

		SectorPolygon output = new SectorPolygon();

		// Early out for convex polygons
		int conv = IsConvex(polygon);

		if (conv != 0) {

			if (conv == -1) {
				polygon.Reverse();
			}

			output.points = polygon;

			for (int t = 0; t < polygon.Count - 2; t++) {
				output.triangles.Add(0);
				output.triangles.Add(t+1);
				output.triangles.Add(t+2);
			}

			return output;
		}

		// Make it clockwise because consistent direction is necessary 

		

		List<int> clippedIndexes = new List<int>();

		int polygonCount = polygon.Count;
		int i = 0;
		int i1, i2, i3;
		int safe = 500;
		while (clippedIndexes.Count < polygonCount - 2 && safe >= 0) { // be careful!!!
			safe -= 1;

			i1 = i % polygonCount;

			while (clippedIndexes.Contains(i1)) {
				i1 = (i1 + 1) % polygonCount;
			}

			i2 = (i1 + 1) % polygonCount;

			while (clippedIndexes.Contains(i2) || i2 == i1) {
				i2 = (i2 + 1) % polygonCount;
			}

			i3 = (i2 + 1) % polygonCount;

			while (clippedIndexes.Contains(i3) || i3 == i2) {
				i3 = (i3 + 1) % polygonCount;
			}

			bool clipped = false;
			bool intersects = false;
			bool straightLine = false;

			if (polygon[i1].y == polygon[i2].y && polygon[i1].y == polygon[i3].y) {
				straightLine = true;
			}

			if (straightLine == false) {
				for (int j = 0; j < polygonCount; j++) {
					int j1 = (j+1) % polygonCount;
					if (!Vector2.Equals(polygon[i1], polygon[j]) &&
						!Vector2.Equals(polygon[i3], polygon[j]) &&
						!Vector2.Equals(polygon[i1], polygon[j1]) &&
						!Vector2.Equals(polygon[i3], polygon[j1]) &&
						LinesIntersect(polygon, i1, i3, j, j1)) {

						intersects = true;
						break;
					}
				}
			}

			//Debug.Log(intersects);

			if (intersects == false && straightLine == false) {
				Vector2 midpoint = new Vector2((polygon[i3].x + polygon[i1].x)/2f, (polygon[i3].y + polygon[i1].y)/2f);

				if (PointInPolygon(midpoint, polygon)) {
					// Clippit!!!
					clippedIndexes.Add(i2);
					output.triangles.Add(i1);
					output.triangles.Add(i2);
					output.triangles.Add(i3);
					clipped = true;
				}
			}

			if (clipped == false) {
				i += 1;
			}
		}

		if (safe <= 0) { 
			// OPTIMISATION: Every time this is hit, the polygon is triangulated but we didn't catch it had finished
			// If we can succeessfully detect when it is done it'll hit this less and triangulation will be faster
			Debug.LogWarning("EarClip: while loop broke safety net. Clipped Indexes :" + clippedIndexes.Count + " polygonCount: " + polygonCount);
		}

		if (!IsClockwise(polygon)) {
			output.triangles.Reverse();
		}

		output.points = polygon;
		return output;
	}

	private static bool IsClockwise(List<Vector2> polygon) {
		float count = 0;
		for (int i = 0; i < polygon.Count; i++) {
			int i2 = (i + 1) % polygon.Count;
			count += polygon[i].x * polygon[i2].y - polygon[i2].x * polygon[i].y;
		}

		return (count < 0);
	}

	private bool IsClockwise(Vector2 A, Vector2 B, Vector2 C) {
		float count = 0;
		count += (B.x - A.x) * (B.y - A.y);
		count += (C.x - B.x) * (C.y - B.y);
		count += (A.x - C.x) * (A.y - C.y);
		return (count > 0);
	}

	private Vector2 VertexToVector2(int vertex) {
		return new Vector2(map.vertices[vertex].x, map.vertices[vertex].y);
	}

	private int IsConvex(List<Vector2> Points) {
		bool got_negative = false;
	    bool got_positive = false;
	    int num_points = Points.Count;
	    int B, C;
	    for (int A = 0; A < num_points; A++)
	    {
	        B = (A + 1) % num_points;
	        C = (B + 1) % num_points;

	        float cross_product =
	            CrossProductLength(
	                Points[A].x, Points[A].y,
	                Points[B].x, Points[B].y,
	                Points[C].x, Points[C].y);
	        if (cross_product < 0)
	        {
	            got_negative = true;
	        }
	        else if (cross_product > 0)
	        {
	            got_positive = true;
	        }
	        if (got_negative && got_positive) return 0;
	    }

	    // If we got this far, the polygon is convex.
	    if (got_positive) return 1;
	    return -1;
	}

	private float CrossProductLength(float Ax, float Ay,
	    float Bx, float By, float Cx, float Cy)
	{
	    // Get the vectors' coordinates.
	    float BAx = Ax - Bx;
	    float BAy = Ay - By;
	    float BCx = Cx - Bx;
	    float BCy = Cy - By;

	    // Calculate the Z coordinate of the cross product.
	    return (BAx * BCy - BAy * BCx);
	}



}
