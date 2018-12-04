using System;
using System.Collections;
using System.Collections.Generic;
using AxisVM;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;

namespace DyToAxisVM
{
    /// <summary>
    /// Axis VM model
    /// </summary>
    public class AxModel
    {
        public AxisVMApplication AxApp = new AxisVMApplication();
        public AxisVMModels AxModels = new AxisVMModels();
        public AxisVMModel AxModel_ = new AxisVMModel();
        public AxisVMNodes AxNodes = new AxisVMNodes();
        public AxisVMLines AxLines = new AxisVMLines();
        public AxisVMLine AxLine = new AxisVMLine();
        [SupressImportIntoVM]
        public ELineGeomType geomType = new ELineGeomType();
        public RLineGeomData geomData = new RLineGeomData();
        public AxisVMCalculation AxCalc = new AxisVMCalculation();
        public AxisVMMaterials AxMaterials = new AxisVMMaterials();
        public AxisVMMaterial AxMaterial = new AxisVMMaterial();
        [SupressImportIntoVM]
        public ENationalDesignCode code = new ENationalDesignCode();
        public AxisVMCrossSections AxCrossSections = new AxisVMCrossSections();
        public AxisVMCrossSection AxCrossSection = new AxisVMCrossSection();
        public AxisVMMembers AxisMembers = new AxisVMMembers();
        public AxisVMMembers AxMembers = new AxisVMMembers();
        public AxisVMMember AxMember = new AxisVMMember();
        public AxisVMLoadCases AxLoadCases = new AxisVMLoadCases();
        public AxisVMLoadCombinations AxLoadComb = new AxisVMLoadCombinations();
        public AxisVMLoads AxLoads = new AxisVMLoads();
        public AxisVMResults AxResults = new AxisVMResults();
        public AxisVMWindows AxWindows = new AxisVMWindows();
        public AxisVMNodesSupports AxNodeSupport = new AxisVMNodesSupports();

        public List<string> Mstrs = new List<string>(); // list of material names already loaded
        public List<int> MIDs = new List<int>(); // list of material IDs already loaded
        public List<string> CSstrs = new List<string>(); // list of sect names already loaded
        public List<int> CSIDs = new List<int>(); // list of sect IDs already loaded

        public List<Point> pts = new List<Point>();
        public List<Line> lns = new List<Line>();
        public List<int> sIDs = new List<int>();
        public List<int> eIDs = new List<int>();
        public List<int[]> membProps = new List<int[]>(); // csID, secID, typeID
        public List<int[]> nodeloadIDs = new List<int[]>(); // load case and loaded node
        public List<bool> sw = new List<bool>(); // line IDs for which self weight was defined
        public List<int> supNodeIDs = new List<int>();

        internal AxModel()
        {

            //Show AxisVM GUI and setup AxisVM to remain opened when COM client finished
            AxApp.CloseOnLastReleased = ELongBoolean.lbFalse; //Axis doesn't exit when script finishes
            AxApp.AskCloseOnLastReleased = ELongBoolean.lbFalse; //Show close dialog before exit
            AxApp.Visible = ELongBoolean.lbFalse; //set on lbFalse can improve speed

            //Create new model
            AxModels = AxApp.Models;
            AxModel_ = AxModels.Item[AxModels.New()];

            //create geometry
            AxNodes = AxModel_.Nodes;
            AxLines = AxModel_.Lines;

            //material, section
            code = ENationalDesignCode.ndcEuroCode; //currently limited to Eurocode
            AxMaterials = AxModel_.Materials;
            AxCrossSections = AxModel_.CrossSections;

            //support
            AxNodeSupport = AxModel_.NodesSupports;

            //load
            AxLoadCases = AxModel_.LoadCases;
            AxLoads = AxModel_.Loads;

            //calculation
            AxCalc = AxModel_.Calculation;
            AxResults = AxModel_.Results;
            AxWindows = AxModel_.Windows;
        }

        /// <summary>
        /// Starts new AxisVM model. After the export, if AxisVM is closed manually, this model is deleted. To start a new model, delete this node and add a new StartAxModel node.
        /// </summary>
        public static AxModel StartAxModel()
        {
            return new AxModel();
        }

        //event AxisVM.IAxisVMModelsEvents_ModelChangedEventHandler ModelChanged
        //Member of AxisVM.IAxisVMModelsEvents_Event
        private void IAxisVMModelsEvents_ModelChanged(object sender, System.EventArgs e)
        {
            // Add your form load event handling code here.
        }

    }

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
        /// <param name="AxModel">Model to export.</param>
        /// <param name="b">Export starts if true.</param>
        /// <param name="ln">List of lines to export.</param>
        /// <returns>exported model AxModel if export was successful</returns>
        /// <returns>Points in AxModel</returns>
        /// <returns>Lines in AxModel</returns>
        /// <search>axisvm, export, line</search>
        [MultiReturn(new[] { "AxModel", "Points", "Lines" })]
        public static IDictionary SendLines(AxModel AxModel, Boolean b, List<Line> ln)
        {
            if (b == true)
            {
                AxModel.AxModel_.BeginUpdate();
                Point newPt = Point.ByCoordinates(0, 0, 0);
                int ptId = -1;
                RPoint3d exc = new RPoint3d { x = 0, y = 0, z = 0 };
                int notValidLineCount = 0;
                Boolean bModify = false;
                if (AxModel.lns.Count > 0) { bModify = true; }

                for (int i = 0; i < ln.Count; i++)
                {
                    if (ln[i].Length > 0)
                    {
                        if (bModify)
                        {
                            // modify existing line
                            newPt = ln[i].StartPoint;
                            ptId = AxModel.sIDs[i];
                            if (ptId != -1)
                            {
                                RPoint3d aPt = new RPoint3d { x = newPt.X, y = newPt.Y, z = newPt.Z };
                                AxModel.AxNodes.SetNodeCoord(ptId, aPt);
                                AxModel.pts[ptId - 1] = newPt;
                                AxModel.lns[i] = ln[i];
                            }
                            newPt = ln[i].EndPoint;
                            ptId = AxModel.eIDs[i];
                            if (ptId != -1)
                            {
                                RPoint3d aPt = new RPoint3d { x = newPt.X, y = newPt.Y, z = newPt.Z };
                                AxModel.AxNodes.SetNodeCoord(ptId, aPt);
                                AxModel.pts[ptId - 1] = newPt;
                                AxModel.lns[i] = ln[i];
                            }
                        }
                        else
                        {
                            //create new line                         
                            newPt = ln[i].StartPoint;
                            if (Extra.GetPointID(AxModel.pts, newPt, 0.001) == -1)
                            {
                                AxModel.pts.Add(newPt);
                                AxModel.AxNodes.Add(newPt.X, newPt.Y, newPt.Z); // add new points to Axis
                            }
                            newPt = ln[i].EndPoint;
                            if (Extra.GetPointID(AxModel.pts, newPt, 0.001) == -1)
                            {
                                AxModel.pts.Add(newPt);
                                AxModel.AxNodes.Add(newPt.X, newPt.Y, newPt.Z); // add new points to Axis
                            }
                            AxModel.sIDs.Add(Extra.GetPointID(AxModel.pts, ln[i].StartPoint, 0.001));
                            AxModel.eIDs.Add(Extra.GetPointID(AxModel.pts, ln[i].EndPoint, 0.001));
                            if ((AxModel.sIDs[i] >= 0) && (AxModel.eIDs[i] >= 0))
                            {
                                AxModel.AxLines.Add(AxModel.sIDs[i], AxModel.eIDs[i], AxModel.geomType, AxModel.geomData);
                                AxModel.AxLine = AxModel.AxLines.Item[i + 1 - notValidLineCount];
                                AxModel.lns.Add(ln[i]);
                            }
                        }
                    }
                    else { notValidLineCount++; }
                }

                //todo: endupdate  only if no analysis or if there is analysis results available, it should be deleted
                AxModel.AxModel_.EndUpdate();

                AxModel.AxApp.Visible = ELongBoolean.lbTrue;
                AxModel.AxApp.BringToFront();


                return new Dictionary<object, object>()
                {
                    {"AxModel", AxModel},
                    {"Points", AxModel.pts},
                    {"Lines", AxModel.lns},
                };
            }

            return new Dictionary<object, object>()
            {
                 {"AxModel", AxModel},
                 {"Points", AxModel.pts},
                 {"Lines", AxModel.lns},
            };

        }

        /// <summary>
        /// Send (export) structural members to AxisVM if b=true.
        /// 
        /// Each structural member should have a cross-section, material and element type (truss, beam, or rib).
        /// </summary>
        /// <param name="AxModel">Model to export.</param>
        /// <param name="b">Export starts if true.</param>
        /// <param name="axm">List of AxisVM structural members to export</param>
        /// <returns>exported model AxModel if export was successful</returns>
        /// <returns>Points in AxModel</returns>
        /// <returns>Lines in AxModel</returns>
        /// <search>axisvm, export, member</search>
        [MultiReturn(new[] { "AxModel", "Points", "Lines"})]
        public static IDictionary SendMembers(AxModel AxModel, Boolean b, List<AxMember> axm)
        {
            bool propChanged = false;
            if (b == true)
            {
                AxModel.AxModel_.BeginUpdate();

                //Create material
                int[] MatID = new int[axm.Count]; //material ID for each structural member
                StringComparison sc = StringComparison.CurrentCultureIgnoreCase;
                for (int i = 0; i < axm.Count; i++)
                {
                    string Mstr = axm[i].mat;

                    //chcek if this material has already been defined or not
                    bool alreadyDefined = false;
                    for (int j = 0; j < AxModel.Mstrs.Count; j++)
                    { if (AxModel.Mstrs[j].Equals(Mstr, sc)) { MatID[i] = AxModel.MIDs[j]; alreadyDefined = true; } }

                    if (!alreadyDefined)
                    {
                        MatID[i] = AxModel.AxMaterials.AddFromCatalog(AxModel.code, Mstr);
                        AxModel.Mstrs.Add(Mstr);
                        AxModel.MIDs.Add(MatID[i]);
                        AxModel.AxMaterial = AxModel.AxMaterials.Item[MatID[i]];
                    }
                }

                //Add cross sections
                int[] SectID = new int[axm.Count]; //section ID for each structural member
                for (int i = 0; i < axm.Count; i++)
                {
                    string CSstr = axm[i].cs;

                    //chcek if this cross-section has already been defined or not
                    bool alreadyDefined = false;
                    for (int j = 0; j < AxModel.CSstrs.Count; j++)
                    { if (AxModel.CSstrs[j].Equals(CSstr, sc)) { SectID[i] = AxModel.CSIDs[j]; alreadyDefined = true; } }

                    if (!alreadyDefined)
                    {
                        SectID[i] = Extra.GetCrossSection(CSstr, sc, AxModel.AxCrossSections); //currently limited to pipe, I, Box
                        AxModel.CSstrs.Add(CSstr);
                        AxModel.CSIDs.Add(SectID[i]);
                        AxModel.AxCrossSection = AxModel.AxCrossSections.Item[SectID[i]];
                    }
                }

                //Geometry
                Point newPt = Point.ByCoordinates(0, 0, 0);
                int ptId = -1;
                RPoint3d exc = new RPoint3d { x = 0, y = 0, z = 0 };
                int notValidLineCount = 0;
                Boolean bModify = false;
                if (AxModel.lns.Count > 0) { bModify = true; }
                for (int i = 0; i < axm.Count; i++)
                {
                    if (axm[i].ln.Length > 0)
                    {
                        if (bModify)
                        {
                            // modify existing line
                            newPt = axm[i].ln.StartPoint;
                            ptId = AxModel.sIDs[i];
                            if (ptId != -1)
                            {
                                RPoint3d aPt = new RPoint3d { x = newPt.X, y = newPt.Y, z = newPt.Z };
                                AxModel.AxNodes.SetNodeCoord(ptId, aPt);
                                AxModel.pts[ptId - 1] = newPt;
                                AxModel.lns[i] = axm[i].ln;
                            }
                            newPt = axm[i].ln.EndPoint;
                            ptId = AxModel.eIDs[i];
                            if (ptId != -1)
                            {
                                RPoint3d aPt = new RPoint3d { x = newPt.X, y = newPt.Y, z = newPt.Z };
                                AxModel.AxNodes.SetNodeCoord(ptId, aPt);
                                AxModel.pts[ptId - 1] = newPt;
                                AxModel.lns[i] = axm[i].ln;
                            }
                            if (AxModel.membProps[i] != new int[] { SectID[i], MatID[i], 0 })
                            {
                                AxModel.AxLine = AxModel.AxLines.Item[i + 1 - notValidLineCount];
                                string Tstr = axm[i].typ;
                                if (Tstr.Equals("truss")) { AxModel.AxLine.DefineAsTruss(MatID[i], SectID[i], ELineNonLinearity.lnlTensionAndCompression, 0); }
                                else if (Tstr.Equals("beam")) { AxModel.AxLine.DefineAsBeam(MatID[i], SectID[i], SectID[i], exc, exc); }
                                else if (Tstr.Equals("rib")) { AxModel.AxLine.DefineAsRib(MatID[i], SectID[i], SectID[i], exc, exc); }
                            }
                        }
                        else
                        {
                            //create new line                         
                            newPt = axm[i].ln.StartPoint;
                            if (Extra.GetPointID(AxModel.pts, newPt, 0.001) == -1)
                            {
                                AxModel.pts.Add(newPt);
                                AxModel.AxNodes.Add(newPt.X, newPt.Y, newPt.Z); // add new points to Axis
                            }
                            newPt = axm[i].ln.EndPoint;
                            if (Extra.GetPointID(AxModel.pts, newPt, 0.001) == -1)
                            {
                                AxModel.pts.Add(newPt);
                                AxModel.AxNodes.Add(newPt.X, newPt.Y, newPt.Z); // add new points to Axis
                            }
                            AxModel.sIDs.Add(Extra.GetPointID(AxModel.pts, axm[i].ln.StartPoint, 0.001));
                            AxModel.eIDs.Add(Extra.GetPointID(AxModel.pts, axm[i].ln.EndPoint, 0.001));
                            if ((AxModel.sIDs[i] >= 0) && (AxModel.eIDs[i] >= 0))
                            {
                                AxModel.AxLines.Add(AxModel.sIDs[i], AxModel.eIDs[i], AxModel.geomType, AxModel.geomData);
                                AxModel.AxLine = AxModel.AxLines.Item[i + 1 - notValidLineCount];
                                string Tstr = axm[i].typ;
                                if (Tstr.Equals("truss")) {
                                    AxModel.AxLine.DefineAsTruss(MatID[i], SectID[i], ELineNonLinearity.lnlTensionAndCompression, 0);
                                    AxModel.membProps.Add(new int[] { SectID[i], MatID[i], 0 });
                                }
                                else if (Tstr.Equals("beam")) {
                                    AxModel.AxLine.DefineAsBeam(MatID[i], SectID[i], SectID[i], exc, exc);
                                    AxModel.membProps.Add(new int[] { SectID[i], MatID[i], 1 });
                                }
                                else if (Tstr.Equals("rib")) {
                                    AxModel.AxLine.DefineAsRib(MatID[i], SectID[i], SectID[i], exc, exc);
                                    AxModel.membProps.Add(new int[] { SectID[i], MatID[i], 2 });
                                };
                                AxModel.lns.Add(axm[i].ln);
                                AxModel.sw.Add(false);
                            }
                        }
                    }
                    else { notValidLineCount++; }
                }

                //todo: endupdate only if no analysis
                AxModel.AxModel_.EndUpdate();
                //RExtendedDisplayParameters dispextpar = new RExtendedDisplayParameters();
                //long lcID = 0;
                //safearray secID = 0;
                //AxModel.AxWindows.GetStaticDisplayParameters(1, dispextpar, lcID, secIDs);
                //dispextpar
                AxModel.AxApp.Visible = ELongBoolean.lbTrue;
                AxModel.AxApp.BringToFront();

                return new Dictionary<object, object>()
                {
                    {"AxModel", AxModel},
                    {"Points", AxModel.pts},
                    {"Lines", AxModel.lns},
                };
            }

            return new Dictionary<object, object>()
            {
                 {"AxModel", AxModel},
                 {"Points", AxModel.pts},
                 {"Lines", AxModel.lns},
            };

        }

    }

}
