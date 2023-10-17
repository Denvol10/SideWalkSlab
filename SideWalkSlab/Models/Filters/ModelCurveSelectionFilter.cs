using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace SideWalkSlab.Models.Filters
{
    public class ModelCurveSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is ModelCurve || elem.GetType().IsSubclassOf(typeof(ModelCurve)))
            {
                return true;
            }

            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
