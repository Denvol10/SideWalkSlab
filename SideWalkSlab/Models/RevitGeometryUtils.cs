using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideWalkSlab.Models
{
    public class RevitGeometryUtils
    {
        public static Edge GetEdgeBySelection(UIApplication uiapp, out string edgeRepresentation)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            Selection sel = uiapp.ActiveUIDocument.Selection;

            var selectedEdge = sel.PickObject(ObjectType.Edge, "Select Edge");
            edgeRepresentation = selectedEdge.ConvertToStableRepresentation(doc);

            Edge edge = doc.GetElement(selectedEdge).GetGeometryObjectFromReference(selectedEdge) as Edge;

            return edge;
        }
    }
}
