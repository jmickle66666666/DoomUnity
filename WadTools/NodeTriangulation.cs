using WadTools;
using UnityEngine;
using System.Collections.Generic;

/*
    Triangulation based on map nodes.

    Imperfect. Currently known maps with issues:

    Doom 2 -
        MAP04 -- small triangle missing
        MAP10 -- large triangles missing
        MAP16 -- small triangle missing, subsectors where they shouldn't exist
        MAP17 -- large triangle missing
        MAP18 -- small triangle missing, subsectors where they shouldn't exist
        MAP19 -- large triangles missing
        MAP22 -- triangles missing
        MAP24 -- small triangle overlap, triangles missing
        MAP25 -- major triangles missing
        MAP26 -- triangles missing
        MAP27 -- triangles missing
        MAP28 -- very slim triangles missing
        MAP30 -- subsectors where they shouldn't exist
        MAP31 -- major triangle missing 


    This list is not comprehensive. 
    I have no way to automatically verify how correct the triangulation is currently
*/

namespace WadTools {

    public struct SubsectorHull {
        public int sector;
        public Vector2[] hull;
    }

    public class NodeTriangulation
    {

        public MapData map;

        public NodeTriangulation(MapData map) {
            this.map = map;
        }

        public List<SubsectorHull> subsectorHulls;
        public void TraverseNodes() {
            subsectorHulls = new List<SubsectorHull>();
            ProcessNode(map.nodes.Length - 1, new List<int>(), new List<Vector2>());
        }

        void ProcessNode(int nodeIndex, List<int> previousNodes, List<Vector2> nodeIntersections)
        {
            Node node = map.nodes[Mathf.Abs(nodeIndex)];

            List<Vector2> newIntersections = new List<Vector2>();
            Vector2 intersect = new Vector2();
            for (int i = 0; i < previousNodes.Count; i++) {
                if (NodeIntersection(in map.nodes[Mathf.Abs(previousNodes[i])], in node, out intersect)) {
                    newIntersections.Add(intersect);
                }
            }

            // Cull intersection points
            for (int i = 0; i < newIntersections.Count; i++) {
                bool removed = false;

                for (int j = 0; j < previousNodes.Count; j++) {
                    Node testNode = map.nodes[Mathf.Abs(previousNodes[j])];

                    if (!PointOnNodeLine(in testNode, newIntersections[i])) {
                        if (NodeSide(in testNode, newIntersections[i]) == previousNodes[j] > 0) {
                            removed = true;
                            break;
                        }
                    }
                }

                if (!removed) {
                    nodeIntersections.Add(newIntersections[i]);
                }
            }

            List<Vector2> leftSide = new List<Vector2>();
            List<Vector2> rightSide = new List<Vector2>();

            for (int i = 0; i < nodeIntersections.Count; i++) {
                if (PointOnNodeLine(in node, nodeIntersections[i])) {
                    leftSide.Add(nodeIntersections[i]);
                    rightSide.Add(nodeIntersections[i]);
                } else {
                    if (!NodeSide(in node, nodeIntersections[i])) {
                        rightSide.Add(nodeIntersections[i]);
                    } else {
                        leftSide.Add(nodeIntersections[i]);
                    }
                }
            }

            List<int> previousNodesLeft = new List<int>(previousNodes.ToArray());
            previousNodesLeft.Add(-nodeIndex);
            List<int> previousNodesRight = new List<int>(previousNodes.ToArray());
            previousNodesRight.Add(nodeIndex);

            if (node.leftChild >= 0) {
                ProcessNode(node.leftChild, previousNodesLeft, leftSide);
            } else {
                subsectorHulls.Add(SubsectorConvexHull(map.subsectors[node.leftChild & 32767], previousNodesLeft, leftSide));
            }

            if (node.rightChild >= 0) {
                ProcessNode(node.rightChild, previousNodesRight, rightSide);
            } else {
                subsectorHulls.Add(SubsectorConvexHull(map.subsectors[node.rightChild & 32767], previousNodesRight, rightSide));
            }
        }

        SubsectorHull SubsectorConvexHull(Subsector subsector, List<int> previousNodes, List<Vector2> nodeIntersections) {
            List<Vector2> hullPoints = new List<Vector2>();

            Vector2 min = new Vector2();
            Vector2 max = new Vector2();
            min.Set(map.vertices[map.segs[subsector.firstSeg].startIndex].x, map.vertices[map.segs[subsector.firstSeg].startIndex].y);
            max.Set(min.x, min.y);

            // Add all seg points
            for (int j = subsector.firstSeg; j < subsector.firstSeg + subsector.segCount; j++) {
                Seg seg = map.segs[j];
                Vector2 start = new Vector2(map.vertices[seg.startIndex].x, map.vertices[seg.startIndex].y);
                Vector2 end = new Vector2(map.vertices[seg.endIndex].x, map.vertices[seg.endIndex].y);
                // start = Vector2.Lerp(start, end, seg.offset / Vector2.Distance(start, end));

                hullPoints.Add(start);
                hullPoints.Add(end);
            }

            Vector2 inter = new Vector2();

            // Add seg/seg intersections
            // for (int i = subsector.firstSeg; i < subsector.firstSeg + subsector.segCount; i++) {
            //     for (int j = i + 1; j < subsector.firstSeg + subsector.segCount; j++) {
            //         Seg seg1 = map.segs[i];
            //         Seg seg2 = map.segs[j];
            //         if (SegSegIntersection(seg1, seg2, out inter)) {
            //             nodeIntersections.Add(inter);
            //         }
            //     }
            // }

            // Add seg/node intersections
            for (int i = 0; i < previousNodes.Count; i++) {
                for (int j = subsector.firstSeg; j < subsector.firstSeg + subsector.segCount; j++) {
                    Seg seg = map.segs[j];
                    if (NodeSegIntersection(in map.nodes[Mathf.Abs(previousNodes[i])], seg, out inter)) {
                        nodeIntersections.Add(inter);
                    }
                }
            }

            // Cull node intersections by nodes and segs
            for (int i = 0; i < nodeIntersections.Count; i++) {
                bool ignored = false;

                for (int j = 0; j < previousNodes.Count; j++) {
                    Node node = map.nodes[Mathf.Abs(previousNodes[j])];
                    if (!PointOnNodeLine(in node, nodeIntersections[i])) {
                        if (NodeSide(in node, nodeIntersections[i]) == previousNodes[j] > 0) {
                            ignored = true;
                            break;
                        }
                    }
                }

                if (!ignored) {
                    for (int j = subsector.firstSeg; j < subsector.firstSeg + subsector.segCount; j++) {
                        Seg seg = map.segs[j];
                        if (!PointOnSegLine(seg, nodeIntersections[i])) {
                            if (SegSide(seg, nodeIntersections[i])) {
                                ignored = true;
                                break;
                            }
                        }
                    }
                }

                if (!ignored) {
                    hullPoints.Add(nodeIntersections[i]);
                }
            }

            Seg firstSeg = map.segs[subsector.firstSeg];
            Linedef line = map.linedefs[firstSeg.linedefIndex];
            Sidedef sidedef = map.sidedefs[firstSeg.direction?line.back:line.front];
            int sector = sidedef.sector;

            // int sector = map.sidedefs[map.segs[subsector.firstSeg].direction?map.linedefs[map.segs[subsector.firstSeg].linedefIndex].back:map.linedefs[map.segs[subsector.firstSeg].linedefIndex].front].sector;

            return new SubsectorHull() {
                sector = sector,
                hull = new List<Vector2>(ConvexHull.MakeHull(hullPoints)).ToArray()
            };
        }

        public Sector SectorAtPosition(Vector2 point)
        {
            int currentNode = map.nodes.Length-1;

            Subsector subsector = TraverseToSubsector(in map.nodes[currentNode], point);
            Seg firstSeg = map.segs[subsector.firstSeg];
            Linedef line = map.linedefs[firstSeg.linedefIndex];
            Sidedef side = map.sidedefs[firstSeg.direction?line.back:line.front];
            return map.sectors[side.sector];
        }

        Subsector TraverseToSubsector(in Node node, Vector2 point)
        {
            int nextNode = NodeSide(in node, point)?node.leftChild:node.rightChild;
            if (nextNode < 0) return map.subsectors[nextNode & 32767];
            return TraverseToSubsector(in map.nodes[nextNode], point);
        }

        // Geometry
        public static float epsilon = 1f;

        public static bool NodeSide(in Node node, Vector2 point)
        {
            if (node.dx == 0) {
                return point.x <= node.x ? node.dy > 0 : node.dy < 0;
            }

            if (node.dy == 0) {
                return point.y <= node.y ? node.dx < 0 : node.dx > 0;
            }

            int x = (int)point.x - node.x;
            int y = (int)point.y - node.y;

            // Try to quickly decide by looking at sign bits.
            if(((int)node.dy ^ (int)node.dx ^ x ^ y) < 0)
                return ((int)node.dy ^ x) < 0;  // (left is negative)
            return y * node.dx >= node.dy * x;
        }

        public static bool NodeIntersection(in Node nodeA, in Node nodeB, out Vector2 point)
        {
            float a1 = nodeA.dy;
            float a2 = nodeB.dy;
            float b1 = -nodeA.dx;
            float b2 = -nodeB.dx;
            float c1 = a1 * nodeA.x + b1 * nodeA.y;
            float c2 = a2 * nodeB.x + b2 * nodeB.y;
            float delta = a1 * b2 - a2 * b1;

            if (Mathf.Abs(delta) < Mathf.Epsilon) {
                point.x = 0;
                point.y = 0;
                return false;
            }

            point.x = (b2 * c1 - b1 * c2) / delta;
            point.y = (a1 * c2 - a2 * c1) / delta;
            return true;
        }

        public static float DistanceFromPointToLine(Vector2 point, Vector2 l1, Vector2 l2)
        {
            // given a line based on two points, and a point away from the line,
            // find the perpendicular distance from the point to the line.
            // see http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
            // for explanation and defination.

            float dx = l2.x - l1.x;
            float dy = l2.y - l1.y;

            return Mathf.Abs((dx)*(l1.y - point.y) - (l1.x - point.x)*(dy))/
                    Mathf.Sqrt(Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2));
        }
        
        public static float DistanceFromPointToLine(Vector2 point, in Node node)
        {
            return Mathf.Abs(node.dx*(node.y - point.y) - (node.x - point.x)*node.dy)/
                    Mathf.Sqrt((node.dx * node.dx) + (node.dy * node.dy));
                    
        }

        public static bool PointOnNodeLine(in Node node, Vector2 point)
        {
            // var a = ((node.dx)*(node.y - point.y) - (node.x - point.x)*(node.dy));
            // return a < epsilon2 && a > -epsilon2;
            return Mathf.Abs(DistanceFromPointToLine(point, in node)) < epsilon;
        }

        public bool NodeSegIntersection(in Node node, Seg seg, out Vector2 point)
        {
            Node fakeNode = new Node() {
                x = map.vertices[seg.startIndex].x,
                y = map.vertices[seg.startIndex].y,
                dx = map.vertices[seg.endIndex].x - map.vertices[seg.startIndex].x, 
                dy = map.vertices[seg.endIndex].y - map.vertices[seg.startIndex].y
            };

            return NodeIntersection(in node, in fakeNode, out point);

            // float a1 = node.dy;
            // float b1 = -node.dx;
            // float c1 = a1 * node.x + b1 * node.y;
            // float a2 = map.vertices[seg.endIndex].y - map.vertices[seg.startIndex].y;
            // float b2 = -(map.vertices[seg.endIndex].x - map.vertices[seg.startIndex].x);
            // float c2 = a2 * map.vertices[seg.startIndex].x + b2 * map.vertices[seg.startIndex].y;
            // float delta = a1 * b2 - a2 * b1;

            // if (Mathf.Abs(delta) < Mathf.Epsilon) {
            //     point.x = 0;
            //     point.y = 0;
            //     return false;
            // }

            // point.x = (b2 * c1 - b1 * c2) / delta;
            // point.y = (a1 * c2 - a2 * c1) / delta;
            // return true;
        }

        public bool SegSegIntersection(Seg seg1, Seg seg2, out Vector2 point)
        {
            float a1 = map.vertices[seg1.endIndex].y - map.vertices[seg1.startIndex].y;
            float b1 = -(map.vertices[seg1.endIndex].x - map.vertices[seg1.startIndex].x);
            float c1 = a1 * map.vertices[seg1.startIndex].x + b1 * map.vertices[seg1.startIndex].y;
            float a2 = map.vertices[seg2.endIndex].y - map.vertices[seg2.startIndex].y;
            float b2 = -(map.vertices[seg2.endIndex].x - map.vertices[seg2.startIndex].x);
            float c2 = a2 * map.vertices[seg2.startIndex].x + b2 * map.vertices[seg2.startIndex].y;
            float delta = a1 * b2 - a2 * b1;

            if (Mathf.Abs(delta) < Mathf.Epsilon) {
                point.x = 0;
                point.y = 0;
                return false;
            }

            point.x = (b2 * c1 - b1 * c2) / delta;
            point.y = (a1 * c2 - a2 * c1) / delta;
            return true;
        }

        public bool PointOnSegLine(Seg seg, Vector2 point)
        {
            Node fakeNode = new Node() {
                x = map.vertices[seg.startIndex].x,
                y = map.vertices[seg.startIndex].y,
                dx = map.vertices[seg.endIndex].x - map.vertices[seg.startIndex].x, 
                dy = map.vertices[seg.endIndex].y - map.vertices[seg.startIndex].y
            };
            return Mathf.Abs(DistanceFromPointToLine(point, in fakeNode)) < epsilon;
        }

        public bool SegSide(Seg seg, Vector2 point)
        {
            Node fakeNode = new Node() {
                x = map.vertices[seg.startIndex].x,
                y = map.vertices[seg.startIndex].y,
                dx = map.vertices[seg.endIndex].x - map.vertices[seg.startIndex].x, 
                dy = map.vertices[seg.endIndex].y - map.vertices[seg.startIndex].y
            };
            return NodeTriangulation.NodeSide(in fakeNode, point);
        }
    }
}