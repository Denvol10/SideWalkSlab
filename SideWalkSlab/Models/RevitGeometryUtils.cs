﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SideWalkSlab.Models.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideWalkSlab.Models
{
    public class RevitGeometryUtils
    {
        // Получение ребра с помощью пользовательского выбора
        public static Edge GetEdgeBySelection(UIApplication uiapp, out string edgeRepresentation)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            Selection sel = uiapp.ActiveUIDocument.Selection;

            var selectedEdge = sel.PickObject(ObjectType.Edge, "Select Edge");
            edgeRepresentation = selectedEdge.ConvertToStableRepresentation(doc);

            Edge edge = doc.GetElement(selectedEdge).GetGeometryObjectFromReference(selectedEdge) as Edge;

            return edge;
        }

        // Получение линий края плиты с помощью пользовательского выбора
        public static List<Curve> GetSideWalkLinesBySelection(UIApplication uiapp, out string sideWalkLineElemIds)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            Selection sel = uiapp.ActiveUIDocument.Selection;

            var sideWalkLineRefererences = sel.PickObjects(ObjectType.Element,
                                                           new ModelCurveSelectionFilter(),
                                                           "Выбереие линии границы плиты");
            Options options = new Options();
            var sideWalkModelCurves = sideWalkLineRefererences.Select(r => doc.GetElement(r));
            sideWalkLineElemIds = ElementIdToString(sideWalkModelCurves);
            var sideWalkCurves = sideWalkModelCurves.OfType<ModelCurve>()
                                                    .Select(mc => mc.GeometryCurve)
                                                    .ToList();

            return sideWalkCurves;
        }

        // Генератор нормализованных пораметров точек на линии
        public static List<double> GenerateNormalizeParameters(int count)
        {
            var parameters = new List<double>(count) { 0, 1 };

            switch (count)
            {
                case 0:
                    return new List<double>() { 0, 1 };
                case 1:
                    return new List<double>() { 0, 1 };
                default:
                    double step = (double)1 / (count - 2);
                    if (count % 2 == 0)
                    {
                        for (double d = 0.5 - step / 2; d > 0; d -= step)
                        {
                            parameters.Add(d);
                        }
                        for (double d = 0.5 + step / 2; d < 1; d += step)
                        {
                            parameters.Add(d);
                        }
                    }
                    else
                    {
                        for (double d = 0.5 - step; d > 0; d -= step)
                        {
                            parameters.Add(d);
                        }
                        for (double d = 0.5; d < 1; d += step)
                        {
                            parameters.Add(d);
                        }
                    }
                    break;
            }

            return parameters.OrderBy(p => p).ToList();
        }

        // Метод получения строки с ElementId
        private static string ElementIdToString(IEnumerable<Element> elements)
        {
            var stringArr = elements.Select(e => "Id" + e.Id.IntegerValue.ToString()).ToArray();
            string resultString = string.Join(", ", stringArr);

            return resultString;
        }
    }
}