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
    /// Load
    /// </summary>
    public class Load
    {
        /// <summary>
        /// Private methods, such as this constructor,
        /// will not be visible in the Dynamo library.
        /// </summary>
        private Load() { }

        /// <summary>
        /// Add point load to a node.
        /// </summary>
        /// <param name="AxModel">AxisVM model to add load to</param>
        /// <param name="ptID">list of nodes to add a fixed support to</param>
        /// <param name="lcName">name of load case (existing or new)</param>
        /// <param name="Fx">nodal load values in global coordinate system</param>
        /// <param name="Fy">nodal load values in global coordinate system</param>
        /// <param name="Fz">nodal load values in global coordinate system</param>
        /// <returns>AxisVM model</returns>
        /// <returns>load case names and node IDs of new nodal loads added in the latest run to AxModel</returns>
        /// <returns>load case names and node IDs of all nodal loads in AxModel</returns>
        [MultiReturn(new[] { "AxModel", "new", "all" })]
        public static IDictionary PointLoad(AxModel AxModel, List<int> ptID, string lcName = "ST1", double Fx = 0, double Fy = 0, double Fz = -1)
        {
            AxModel.AxModel_.BeginUpdate();

            // Define Load case
            int lc = -1;
            bool exists = false; //load case defined earlier?
            for (int i = 1; i <= AxModel.AxLoadCases.Count; i++)
            {
                if (AxModel.AxLoadCases.Name[i].Equals(lcName)) { exists = true; lc = i; break; }
            }
            if (exists == false) { lc = AxModel.AxLoadCases.Add(lcName, ELoadCaseType.lctStandard); }

            // Add nodal loads
            RLoadNodalForce ptLoad = new RLoadNodalForce { LoadCaseId = lc };
            int prevlcCOunt = AxModel.nodeloadIDs.Count;
            List<int[]> newNodeloadIDs = new List<int[]>(); // load case and loaded node
            for (int i = 0; i < ptID.Count; i++)
            {
                bool notyet = true;
                for (int j = 0; j < prevlcCOunt; j++)
                {
                    if (AxModel.nodeloadIDs[j][0] == lc && AxModel.nodeloadIDs[j][1] == ptID[i])
                    {
                        notyet = false;
                        break;
                    }
                }
                if (notyet)
                {
                    ptLoad.NodeId = ptID[i];
                    ptLoad.Fx = Fx;
                    ptLoad.Fy = Fy;
                    ptLoad.Fz = Fz;
                    ptLoad.Mx = 0;
                    ptLoad.My = 0;
                    ptLoad.Mz = 0;
                    ptLoad.ReferenceId = 0;
                    AxModel.AxLoads.AddNodalForce(ptLoad);
                    AxModel.nodeloadIDs.Add(new int[] { lc, ptID[i] });
                    newNodeloadIDs.Add(new int[] { lc, ptID[i] });
                }
            }

            AxModel.AxModel_.EndUpdate();

            return new Dictionary<object, object>()
            {
                 {"AxModel", AxModel},
                 {"new", newNodeloadIDs},
                 {"all", AxModel.nodeloadIDs},
            };
        }


        /// <summary>
        /// Add self-weight to all AxMembers.
        /// </summary>
        /// <param name="AxModel">AxisVM model to add load to</param>
        /// <param name="lcName">name of load case (existing or new)</param>
        /// <returns>AxisVM model</returns>
        [MultiReturn(new[] { "AxModel"})]
        public static IDictionary SelfWeight(AxModel AxModel, string lcName = "ST1")
        {
            AxModel.AxModel_.BeginUpdate();

            // Define Load case
            int lc = -1;
            bool exists = false; //load case defined earlier?
            for (int i = 1; i <= AxModel.AxLoadCases.Count; i++)
            {
                if (AxModel.AxLoadCases.Name[i].Equals(lcName)) { exists = true; lc = i; break; }
            }
            if (exists == false) { lc = AxModel.AxLoadCases.Add(lcName, ELoadCaseType.lctStandard); }

            int lnc = 0;

            // Add self weight
            for (int i = 1; i <= AxModel.lns.Count; i++)
            {
                if (AxModel.sw[i-1] == false)
                {
                    if (AxModel.AxLoads.AddBeamSelfWeight(i, lc) < 0) // use only line indexes
                    {
                        if (AxModel.AxLoads.AddTrussSelfWeight(i, lc) < 0)
                        {
                            AxModel.AxLoads.AddRibSelfWeight(i, lc);
                        }
                    }
                    AxModel.sw[i - 1] = true;
                }
            }

            AxModel.AxModel_.EndUpdate();

            return new Dictionary<object, object>()
            {
                 {"AxModel", AxModel},
            };
        }
    }

    /// <summary>
    /// Support
    /// </summary>
    public class Support
    {
        /// <summary>
        /// Private methods, such as this constructor,
        /// will not be visible in the Dynamo library.
        /// </summary>
        private Support() { }

        /// <summary>
        /// Add nodal supports.
        /// </summary>
        /// <param name="AxModel">AxisVM model to add support to</param>
        /// <param name="ptID">list of nodes to add a fixed support to.</param>
        /// <param name="Rx">displacement support stiffness along global x axis</param>
        /// <param name="Ry">displacement support stiffness along global y axis</param>
        /// <param name="Rz">displacement support stiffness along global z axis</param>
        /// <param name="Rxx">rotational support stiffness around global x axis</param>
        /// <param name="Ryy">rotational support stiffness around global y axis</param>
        /// <param name="Rzz">rotational support stiffness around global z axis</param>
        /// <returns>AxisVM model</returns>
        /// <returns>node IDs of the new supports added in the latest run to AxModel</returns>
        /// <returns>node IDs of all supports in AxModel</returns>
        [MultiReturn(new[] { "AxModel", "new", "all" })]
        public static IDictionary NodalSupport(AxModel AxModel, List<int> ptID, double Rx = 1e10, double Ry = 1e10, double Rz = 1e10, double Rxx = 1e10, double Ryy = 1e10, double Rzz = 1e10)
        {
            AxModel.AxModel_.BeginUpdate();

            RStiffnesses node_supp_Stiff = new RStiffnesses();
            RNonLinearity node_supp_NonLin = new RNonLinearity
            {
                x = ELineNonLinearity.lnlTensionAndCompression,
                y = ELineNonLinearity.lnlTensionAndCompression,
                z = ELineNonLinearity.lnlTensionAndCompression,
                xx = ELineNonLinearity.lnlTensionAndCompression,
                yy = ELineNonLinearity.lnlTensionAndCompression,
                zz = ELineNonLinearity.lnlTensionAndCompression,
            };
            RResistances node_supp_Resistance = new RResistances { x = 0, y = 0, z = 0, xx = 0, yy = 0, zz = 0, };
            node_supp_Stiff.x = Rx;
            node_supp_Stiff.y = Ry;
            node_supp_Stiff.z = Rz;
            node_supp_Stiff.xx = Rxx;
            node_supp_Stiff.yy = Ryy;
            node_supp_Stiff.zz = Rzz;

            int prevSupCount = AxModel.supNodeIDs.Count;
            List<int> newSupNodeIDs = new List<int>();
            for (int i = 0; i < ptID.Count; i++)
            {
                bool notyet = true;
                for (int j = 0; j < prevSupCount; j++)
                {
                    if (AxModel.supNodeIDs[j] == ptID[i])
                    {
                        notyet = false;
                        break;
                    }
                }
                if (notyet)
                {
                    AxModel.AxNodeSupport.AddNodalGlobal(node_supp_Stiff, node_supp_NonLin, node_supp_Resistance, ptID[i]);
                    AxModel.supNodeIDs.Add(ptID[i]);
                    newSupNodeIDs.Add(ptID[i]);
                }
            }

            AxModel.AxModel_.EndUpdate();

            return new Dictionary<object, object>()
            {
                 {"AxModel", AxModel},
                 {"new", newSupNodeIDs},
                 {"all", AxModel.supNodeIDs},
            };
        }

        /// <summary>
        /// Add pinned nodal supports (displacements are restircted, rotations are free).
        /// </summary>
        /// <param name="AxModel">AxisVM model to add support to</param>
        /// <param name="ptID">list of nodes to add a pinned support to</param>
        /// <returns>AxisVM model</returns>
        /// <returns>node IDs of the new supports added in the latest run to AxModel</returns>
        /// <returns>node IDs of all supports in AxModel</returns>
        [MultiReturn(new[] { "AxModel", "new", "all" })]
        public static IDictionary PinnedNodalSupport(AxModel AxModel, List<int> ptID)
        {
            AxModel.AxModel_.BeginUpdate();

            RStiffnesses node_supp_Stiff = new RStiffnesses();
            RNonLinearity node_supp_NonLin = new RNonLinearity
            {
                x = ELineNonLinearity.lnlTensionAndCompression,
                y = ELineNonLinearity.lnlTensionAndCompression,
                z = ELineNonLinearity.lnlTensionAndCompression,
                xx = ELineNonLinearity.lnlTensionAndCompression,
                yy = ELineNonLinearity.lnlTensionAndCompression,
                zz = ELineNonLinearity.lnlTensionAndCompression,
            };
            RResistances node_supp_Resistance = new RResistances { x = 0, y = 0, z = 0, xx = 0, yy = 0, zz = 0, };
            node_supp_Stiff.x = 1e10;
            node_supp_Stiff.y = 1e10;
            node_supp_Stiff.z = 1e10;
            node_supp_Stiff.xx = 0;
            node_supp_Stiff.yy = 0;
            node_supp_Stiff.zz = 0;
            int prevSupCount = AxModel.supNodeIDs.Count;
            List<int> newSupNodeIDs = new List<int>();
            for (int i = 0; i < ptID.Count; i++)
            {
                bool notyet = true;
                for (int j = 0; j < prevSupCount; j++)
                {
                    if (AxModel.supNodeIDs[j] == ptID[i])
                    {
                        notyet = false;
                        break;
                    }
                }
                if (notyet)
                {
                    AxModel.AxNodeSupport.AddNodalGlobal(node_supp_Stiff, node_supp_NonLin, node_supp_Resistance, ptID[i]);
                    AxModel.supNodeIDs.Add(ptID[i]);
                    newSupNodeIDs.Add(ptID[i]);
                }
            }

            AxModel.AxModel_.EndUpdate();

            return new Dictionary<object, object>()
            {
                 {"AxModel", AxModel},
                 {"new", newSupNodeIDs},
                 {"all", AxModel.supNodeIDs},
            };
        }

        /// <summary>
        /// Add fixed nodal supports (all displacements and rotations are restricted).
        /// </summary>
        /// <param name="AxModel">AxisVM model to add support to</param>
        /// <param name="ptID">List of nodes to add a fixed support to.</param>
        /// <returns>AxisVM model</returns>
        /// <returns>node IDs of the new supports added in the latest run to AxModel</returns>
        /// <returns>node IDs of all supports in AxModel</returns>
        [MultiReturn(new[] { "AxModel", "new", "all" })]
        public static IDictionary FixedNodalSupport(AxModel AxModel, List<int> ptID)
        {
            AxModel.AxModel_.BeginUpdate();

            RStiffnesses node_supp_Stiff = new RStiffnesses();
            RNonLinearity node_supp_NonLin = new RNonLinearity
            {
                x = ELineNonLinearity.lnlTensionAndCompression,
                y = ELineNonLinearity.lnlTensionAndCompression,
                z = ELineNonLinearity.lnlTensionAndCompression,
                xx = ELineNonLinearity.lnlTensionAndCompression,
                yy = ELineNonLinearity.lnlTensionAndCompression,
                zz = ELineNonLinearity.lnlTensionAndCompression,
            };
            RResistances node_supp_Resistance = new RResistances { x = 0, y = 0, z = 0, xx = 0, yy = 0, zz = 0, };
            node_supp_Stiff.x = 1e10;
            node_supp_Stiff.y = 1e10;
            node_supp_Stiff.z = 1e10;
            node_supp_Stiff.xx = 1e10;
            node_supp_Stiff.yy = 1e10;
            node_supp_Stiff.zz = 1e10;

            int prevSupCount = AxModel.supNodeIDs.Count;
            List<int> newSupNodeIDs = new List<int>();
            for (int i = 0; i < ptID.Count; i++)
            {
                bool notyet = true;
                for (int j = 0; j < prevSupCount; j++)
                {
                    if (AxModel.supNodeIDs[j] == ptID[i])
                    {
                        notyet = false;
                        break;
                    }
                }
                if (notyet)
                {
                    AxModel.AxNodeSupport.AddNodalGlobal(node_supp_Stiff, node_supp_NonLin, node_supp_Resistance, ptID[i]);
                    AxModel.supNodeIDs.Add(ptID[i]);
                    newSupNodeIDs.Add(ptID[i]);
                }
            }

            AxModel.AxModel_.EndUpdate();

            return new Dictionary<object, object>()
            {
                 {"AxModel", AxModel},
                 {"new", newSupNodeIDs},
                 {"all", AxModel.supNodeIDs},
            };
        }

    }
}
