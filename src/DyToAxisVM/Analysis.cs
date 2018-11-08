using System;
using System.Collections;
using System.Collections.Generic;
using AxisVM;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;

namespace DyToAxisVM
{
    /// <summary>
    /// Run linear structural analysis
    /// WIP - Work In Progress
    /// </summary>
    public class Analysis
    {
        /// <summary>
        /// Private methods, such as this constructor,
        /// will not be visible in the Dynamo library.
        /// </summary>
        private Analysis() { }

        /// <summary>
        /// Run linear structural analysis. 
        /// WIP - Work In Progress
        /// </summary>
        /// <param name="AxModel">Model to analyse.</param>
        /// <param name = "b" > Analysis starts if true. Set this to false while changing geometry, if the structural model is large, since analysis might take a long time.</param>
        /// <returns>AxModel if successful</returns>
        /// <search>axisvm, analysis</search>
        public static AxModel LinearAnalysis(AxModel AxModel, Boolean b = true)
        {
            if (b == true)
            {
                //todo: turn off results
                //AXM.AxApp.Visible = ELongBoolean.lbFalse;
                AxModel.AxModel_.BeginUpdate();

                AxModel.AxCalc.LinearAnalysis(ECalculationUserInteraction.cuiNoUserInteractionWithAutoCorrect);

                AxModel.AxModel_.EndUpdate();
                AxModel.AxApp.Visible = ELongBoolean.lbTrue;
                AxModel.AxApp.BringToFront();

                //todo: turn on results

                return AxModel;
            }

            return AxModel;

        }
    }
}
