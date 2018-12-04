using System;
using System.Collections;
using System.Collections.Generic;
using AxisVM;
using Autodesk.DesignScript.Geometry;
using System.Text.RegularExpressions;
using Autodesk.DesignScript.Runtime;

namespace DyToAxisVM
{
    /// <summary>
    /// additional functions that speeds up modelling in Dynamo, or functions not visible in Dynamo.
    /// </summary>
    public class Extra
    {
        /// <summary>
        /// Private methods, such as this constructor,
        /// will not be visible in the Dynamo library.
        /// </summary>
        private Extra() { }

        /// <summary>
        /// get ID of point p in list L, return -1 if the point is not in the list
        /// </summary>
        [SupressImportIntoVM]
        public static int GetPointID(List<Point> L, Point p, double tol)
        {
            for (int i = 0; i < L.Count; i++)
            {
                if ((Math.Abs(L[i].X - p.X) < tol) & (Math.Abs(L[i].Y - p.Y) < tol) & (Math.Abs(L[i].Z - p.Z) < tol))
                {
                    return i + 1; //numbering in Axis starts with 1
                }
            }
            return -1;
        }

        /// <summary>
        /// return the cross-section family (e.g., I) of the used crooss-section (e.g. IPE 240)
        /// </summary>
        [SupressImportIntoVM]
        public static int GetCrossSection(string str, StringComparison cs, AxisVMCrossSections AxCs)
        {
            Regex regex1 = new Regex("X");
            Regex regex2 = new Regex("x");
            int res = -1;

            if (str.StartsWith("IPE ", cs) || str.StartsWith("I ", cs) || str.StartsWith("HE ", cs) || str.StartsWith("HP ", cs) ||
                str.StartsWith("HL ", cs) || str.StartsWith("HD ", cs) || str.StartsWith("IPN ", cs) || str.StartsWith("UB ", cs) ||
                str.StartsWith("UC ", cs))
            { res = AxCs.AddFromCatalog(ECrossSectionShape.cssI, str); }
            else if (str.StartsWith("ROR", cs))
            { res = AxCs.AddFromCatalog(ECrossSectionShape.cssPipe, str); }
            else if (str.StartsWith("O ", cs) || str.StartsWith("RND ", cs) || str.StartsWith("ROND ", cs))
            { res = AxCs.AddFromCatalog(ECrossSectionShape.cssCircle, str); }
            else if (regex1.Matches(str, 0).Count == 2) // for boxes the format is always 100X5X5
            { res = AxCs.AddFromCatalog(ECrossSectionShape.cssBox, str); }
            else if (regex2.Matches(str, 0).Count == 1) // rectangular format is always 100x100
            { res = AxCs.AddFromCatalog(ECrossSectionShape.cssRectangular, str); }
            if (res <= 0)
            {
                res = AxCs.AddFromCatalog(ECrossSectionShape.cssI, str);
                if (res > 0) { return res; }
                else
                {
                    res = AxCs.AddFromCatalog(ECrossSectionShape.cssPipe, str);
                    if (res > 0) { return res; }
                    else
                    {
                        res = AxCs.AddFromCatalog(ECrossSectionShape.cssBox, str);
                        if (res > 0) { return res; }
                        else
                        {
                            res = AxCs.AddFromCatalog(ECrossSectionShape.cssRectangular, str);
                            if (res > 0) { return res; }
                            else
                            {
                                res = AxCs.AddFromCatalog(ECrossSectionShape.cssCircle, str);
                                if (res > 0) { return res; }
                                else return -1;
                            }
                        }
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// index of a point (pt) in a list of points (pts)
        /// </summary>
        /// <param name="pts">list of points to search in</param>
        /// <param name="pt">point to search for in the list</param>
        /// <param name="e">tolerance for the differenc of the coordinate values</param>
        public static List<int> PtListIndex(List<Point> pts, List<Point> pt, double e = 1e-5)
        {
            List<int> IDs = new List<int>(new int[pt.Count]);
            for (int j = 0; j < pt.Count; j++)
            {
                IDs[j] = -1;
                for (int i = 0; i < pts.Count; i++)
                {
                    if ((Math.Abs(pt[j].X - pts[i].X) < e) && (Math.Abs(pt[j].Y - pts[i].Y) < e) && (Math.Abs(pt[j].Z - pts[i].Z) < e))
                    {
                        IDs[j] = i + 1;
                        break;
                    }
                }
            }
            return IDs;
        }

        /// <summary>
        /// Indeces of points at given height (Z) in a list of points (pts). Returns -1 if the point is not in the list. 
        /// </summary>
        /// <param name="pts">list of points to search in</param>
        /// <param name="Z">height</param>
        /// <param name="e">tolerance for the differenc of the coordinate values</param>
        public static List<int> PtListIndexAtHeight(List<Point> pts, double Z = 0, double e = 1e-5)
        {
            List<int> IDs = new List<int>();
            for (int i = 0; i < pts.Count; i++)
            {
                if (Math.Abs(Z - pts[i].Z) < e)
                {
                    IDs.Add(i + 1);
                }
            }
            return IDs;
        }

        /// <summary>
        /// Prune lines to exclude duplicates within tolerance of included lines.
        /// </summary>
        /// <param name="lns">list of lines to search in</param>
        /// <param name="tolerance">tolerance for the differenc of the coordinate values</param>
        public static List<Line> PruneDuplicateLines(List<Line> lns, double tolerance = 1e-5)
        {
            List<Line> pruned = new List<Line>();
            bool bFound = false;
            for (int i = 0; i < lns.Count; i++)
            {
                bFound = false;
                Point s1;
                Point s2;
                Point e1;
                Point e2;
                for (int j = i + 1; j < lns.Count; j++)
                {
                    s1 = lns[i].StartPoint;
                    e1 = lns[i].EndPoint;
                    s2 = lns[j].StartPoint;
                    e2 = lns[j].EndPoint;
                    if ((IsSamePt(s1, s2, tolerance) && IsSamePt(e1, e2, tolerance)) ||
                         (IsSamePt(s1, e2, tolerance) && IsSamePt(e1, s2, tolerance)))
                    {
                        bFound = true;
                    }

                }
                if (bFound == false) { pruned.Add(lns[i]); }

            }
            return pruned;
        }

        [SupressImportIntoVM]
        public static bool IsSamePt(Point p1, Point p2, double e)
        {
            if ((Math.Abs(p1.X - p2.X) < e) &&
                (Math.Abs(p1.Y - p2.Y) < e) &&
                (Math.Abs(p1.Z - p2.Z) < e))
            {
                return true;
            }
            return false;
        }
    }
}
