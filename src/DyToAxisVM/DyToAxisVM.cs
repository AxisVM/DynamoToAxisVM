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
    /// Structural member defined by a line, a cross-section, material, and type (truss, beam, rib).
    /// </summary>
    public class AxMember
    {
        public string mat = "S 235";
        public string cs = "IPE 240";
        public string typ = "beam";
        public Line ln;

        private AxMember(Line line, string material, string crossSection, string type)
        {
            mat = material;
            cs = crossSection;
            typ = type; //truss, beam, rib
            ln = line;
        }

        /// <summary>
        /// Create structural member defined by a line, a cross-section, material, and type (truss, beam, rib).
        /// </summary>
        /// <param name="line"></param>
        /// <param name="material">Material name e.g., S235</param>
        /// <param name="crossSection">Cross-section name e.g., IPE 240</param>
        /// <param name="type">Element type (truss, beam or rib)</param>
        /// <returns>AxisVM structural member (AxMember)</returns>
        /// <search>axisvm, member</search>
        public static AxMember ByProperties(Line line, string material = "S 235", string crossSection = "IPE 240", string type = "beam")
        {
            return new AxMember(line, material, crossSection, type);
        }

    }

    /// <summary>
    /// Options for export
    /// </summary>
    public class ExportOptions
    {

        /// <summary>
        /// Private methods, such as this constructor,
        /// will not be visible in the Dynamo library.
        /// </summary>
        private ExportOptions(){}

        /// <summary>
        /// Send (export) lines to AxisVM if b=true.
        /// </summary>
        /// <param name="b">Export starts if true.</param>
        /// <param name="line">Lines to export.</param>
        /// <returns>true if successful</returns>
        /// <search>axisvm, export, line</search>
        public static Boolean SendLines(Boolean b, List<Line> ln)
        {
            if (b == true)
            {
                AxisVMApplication AxApp = new AxisVMApplication();
                AxisVMModels AxModels = new AxisVMModels();
                 AxisVMModel AxModel = new AxisVMModel();
                AxisVMNodes AxNodes = new AxisVMNodes();
                AxisVMLines AxLines = new AxisVMLines();
                AxisVMLine AxLine = new AxisVMLine();
                ELineGeomType geomType = new ELineGeomType();
                RLineGeomData geomData = new RLineGeomData();

                //Show AxisVM GUI and setup AxisVM to remain opened when COM client finished
                AxApp.CloseOnLastReleased = ELongBoolean.lbFalse; //Axis doesn't exit when script finishes
                AxApp.AskCloseOnLastReleased = ELongBoolean.lbFalse; //Show close dialog before exit
                AxApp.Visible = ELongBoolean.lbFalse; //set on lbFalse can improve speed

                //Create new model
                AxModels = AxApp.Models;
                AxModel = AxModels.Item[AxModels.New()];

                //create nodes
                AxNodes = AxModel.Nodes;

                List<Point> pt = new List<Point>();
                Point newPt = Point.ByCoordinates(0, 0, 0);
                for (int i = 0; i < ln.Count; i++)
                {
                    if (ln[i].Length > 0)
                    {
                        newPt = ln[i].StartPoint;
                        if (Extra.GetPointID(pt, newPt, 0.001) == -1)
                        {
                            pt.Add(newPt);
                        }
                        newPt = ln[i].EndPoint;
                        if (Extra.GetPointID(pt, newPt, 0.001) == -1)
                        {
                            pt.Add(newPt);
                        }
                    }
                }
                for (int i = 0; i < pt.Count; i++)
                {
                    AxNodes.Add(pt[i].X, pt[i].Y, pt[i].Z);
                }

                //create lines, elements
                AxLines = AxModel.Lines;
                RPoint3d exc = new RPoint3d { x = 0, y = 0, z = 0 };
                int IDs = -1;
                int IDe = -1;
                int notValidLineCount = 0;
                for (int i = 0; i < ln.Count; i++)
                {
                    if (ln[i].Length > 0)
                    {
                        IDs = Extra.GetPointID(pt, ln[i].StartPoint, 0.001);
                        IDe = Extra.GetPointID(pt, ln[i].EndPoint, 0.001);
                        if ((IDs >= 0) && (IDe >= 0))
                        {
                            AxLines.Add(IDs, IDe, geomType, geomData);
                            AxLine = AxLines.Item[i + 1 - notValidLineCount];
                        }
                        else return false;
                    }
                    else { notValidLineCount++; }
                }
                newPt.Dispose();

                AxApp.Visible = ELongBoolean.lbTrue;
                AxApp.BringToFront();

                return true;
            }

            return false;

        }

        /// <summary>
        /// Send (export) structural members to AxisVM if b=true.
        /// 
        /// Each structural member should have a cross-section, material and element type (truss, beam, or rib).
        /// </summary>
        /// <param name="b">Export starts if true.</param>
        /// <param name="AxMember">AxisVM structural member</param>
        /// <returns>true if successful</returns>
        /// <search>axisvm, export, member</search>
        public static Boolean SendMembers(Boolean b, List<AxMember> axm)
        {
            if (b == true)
            {
                AxisVMApplication AxApp = new AxisVMApplication();
                AxisVMModels AxModels = new AxisVMModels();
                AxisVMModel AxModel = new AxisVMModel();
                AxisVMMaterials AxMaterials = new AxisVMMaterials();
                AxisVMMaterial AxMaterial = new AxisVMMaterial();
                ENationalDesignCode code = new ENationalDesignCode();
                AxisVMCrossSections AxCrossSections = new AxisVMCrossSections();
                AxisVMCrossSection AxCrossSection = new AxisVMCrossSection();
                AxisVMNodes AxNodes = new AxisVMNodes();
                AxisVMLines AxLines = new AxisVMLines();
                AxisVMLine AxLine = new AxisVMLine();
                ELineGeomType geomType = new ELineGeomType();
                RLineGeomData geomData = new RLineGeomData();
                AxisVMMembers AxisMembers = new AxisVMMembers();
                AxisVMNodalSupports AxNodalSupports = new AxisVMNodalSupports();
                AxisVMMembers AxMembers = new AxisVMMembers();
                AxisVMMember AxMember = new AxisVMMember();
                AxisVMLoadCases AxLoadCases = new AxisVMLoadCases();
                AxisVMLoadCombinations AxLoadComb = new AxisVMLoadCombinations();
                AxisVMLoads AxLoads = new AxisVMLoads();

                //Show AxisVM GUI and setup AxisVM to remain opened when COM client finished
                AxApp.CloseOnLastReleased = ELongBoolean.lbFalse; //Axis doesn't exit when script finishes
                AxApp.AskCloseOnLastReleased = ELongBoolean.lbFalse; //Show close dialog before exit
                AxApp.Visible = ELongBoolean.lbFalse; //set on lbFalse can improve speed

                //Create new model
                AxModels = AxApp.Models;
                AxModel = AxModels.Item[AxModels.New()];

                //Create material
                code = ENationalDesignCode.ndcEuroCode; //currently limited to Eurocode
                AxMaterials = AxModel.Materials;
                int[] MatID = new int[axm.Count]; //material ID for each structural member
                List<string> Mstrs = new List<string>(); // list of material names already loaded
                List<int> MIDs = new List<int>(); // list of material IDs already loaded
                StringComparison sc = StringComparison.CurrentCultureIgnoreCase;

                for (int i = 0; i < axm.Count; i++)
                {
                    string Mstr = axm[i].mat;

                    //chcek if this material has already been defined or not
                    bool alreadyDefined = false;
                    for (int j = 0; j < Mstrs.Count; j++)
                    { if (Mstrs[j].Equals(Mstr, sc)) { MatID[i] = MIDs[j]; alreadyDefined = true; } }

                    if (!alreadyDefined)
                    {
                        MatID[i] = AxMaterials.AddFromCatalog(code, Mstr);
                        Mstrs.Add(Mstr);
                        MIDs.Add(MatID[i]);
                        AxMaterial = AxMaterials.Item[MatID[i]];
                    }
                }

                //Add cross sections
                AxCrossSections = AxModel.CrossSections;
                int[] SectID = new int[axm.Count]; //section ID for each structural member
                List<string> CSstrs = new List<string>(); // list of sect names already loaded
                List<int> CSIDs = new List<int>(); // list of sect IDs already loaded

                for (int i = 0; i < axm.Count; i++)
                {
                    string CSstr = axm[i].cs;

                    //chcek if this cross-section has already been defined or not
                    bool alreadyDefined = false;
                    for (int j = 0; j < CSstrs.Count; j++)
                    { if (CSstrs[j].Equals(CSstr, sc)) { SectID[i] = CSIDs[j]; alreadyDefined = true; } }

                    if (!alreadyDefined)
                    {
                        SectID[i] = Extra.GetCrossSection(CSstr, sc, AxCrossSections); //currently limited to pipe, I, Box
                        CSstrs.Add(CSstr);
                        CSIDs.Add(SectID[i]);
                        AxCrossSection = AxCrossSections.Item[SectID[i]];
                    }
                }

                //create nodes
                AxNodes = AxModel.Nodes;

                List<Point> pt = new List<Point>();
                Point newPt = Point.ByCoordinates(0, 0, 0);
                for (int i = 0; i < axm.Count; i++)
                {
                    if (axm[i].ln.Length > 0)
                    {
                        newPt = axm[i].ln.StartPoint;
                        if (Extra.GetPointID(pt, newPt, 0.001) == -1)
                        {
                            pt.Add(newPt);
                        }
                        newPt = axm[i].ln.EndPoint;
                        if (Extra.GetPointID(pt, newPt, 0.001) == -1)
                        {
                            pt.Add(newPt);
                        }
                    }
                }
                for (int i = 0; i < pt.Count; i++)
                {
                    AxNodes.Add(pt[i].X, pt[i].Y, pt[i].Z);
                }

                //create lines, elements
                AxLines = AxModel.Lines;
                RPoint3d exc = new RPoint3d { x = 0, y = 0, z = 0 };
                int IDs = -1;
                int IDe = -1;
                int notValidLineCount = 0;
                for (int i = 0; i < axm.Count; i++)
                {
                    if (axm[i].ln.Length > 0)
                    {
                        IDs = Extra.GetPointID(pt, axm[i].ln.StartPoint, 0.001);
                        IDe = Extra.GetPointID(pt, axm[i].ln.EndPoint, 0.001);
                        if ((IDs >= 0) && (IDe >= 0))
                        {
                            AxLines.Add(IDs, IDe, geomType, geomData);
                            AxLine = AxLines.Item[i + 1];
                            string Tstr = axm[i].typ;
                            if (Tstr.Equals("truss")) { AxLine.DefineAsTruss(MatID[i], SectID[i], ELineNonLinearity.lnlTensionAndCompression, 0); }
                            else if (Tstr.Equals("beam")) { AxLine.DefineAsBeam(MatID[i], SectID[i], SectID[i], exc, exc); }
                            else if (Tstr.Equals("rib")) { AxLine.DefineAsRib(MatID[i], SectID[i], SectID[i], exc, exc); };
                        }
                        else return false;
                    }
                    else { notValidLineCount++; }
                }
                newPt.Dispose();

                AxApp.Visible = ELongBoolean.lbTrue;
                AxApp.BringToFront();

                return true;
            }

            return false;

        }

    }

    [SupressImportIntoVM]
    public class Extra
    {
        /// <summary>
        /// get ID of point p in list L, return -1 if the point is not in the list
        /// </summary>
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

    }

}
