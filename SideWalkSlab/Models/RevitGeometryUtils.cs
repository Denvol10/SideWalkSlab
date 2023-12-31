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

        // Проверка на то существует ли ребро в модели
        public static bool IsEdgeExistInModel(Document doc, string edgeRepresentation)
        {
            if (string.IsNullOrEmpty(edgeRepresentation))
            {
                return false;
            }

            try
            {
                Reference reference = Reference.ParseFromStableRepresentation(doc, edgeRepresentation);
                if (reference is null)
                {
                    return false;
                }
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                return false;
            }
            catch (ArgumentNullException)
            {
                return false;
            }

            return true;
        }

        // Получение ребра из Reference
        public static Edge GetEdgeByReference(Document doc, string edgeRepresentation)
        {
            Reference reference = Reference.ParseFromStableRepresentation(doc, edgeRepresentation);

            string[] elementInfo = edgeRepresentation.Split(':');
            int edgeId = int.Parse(elementInfo.ElementAt(elementInfo.Length - 2));

            Element element = doc.GetElement(reference.ElementId);
            Options options = new Options();
            var elementGeometry = element.get_Geometry(options);
            var elementGeometryInstance = elementGeometry.OfType<GeometryInstance>().First();
            var edgeArrays = elementGeometryInstance.GetInstanceGeometry().OfType<Solid>().Select(s => s.Edges);
            foreach (var edgeArray in edgeArrays)
            {
                foreach (var edgeObject in edgeArray)
                {
                    if (edgeObject is Edge edge && edge.Id == edgeId)
                    {
                        return edge;
                    }
                }
            }

            return null;
        }

        // Получение линий с помощью пользовательского выбора
        public static List<ModelCurve> GetCurvesBySelection(UIApplication uiapp, out string sideWalkLineElemIds)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            Selection sel = uiapp.ActiveUIDocument.Selection;

            var sideWalkLineRefererences = sel.PickObjects(ObjectType.Element,
                                                           new ModelCurveSelectionFilter(),
                                                           "Выбереие линии подрезки плиты");
            Options options = new Options();
            var sideWalkModelCurves = sideWalkLineRefererences.Select(r => doc.GetElement(r));
            sideWalkLineElemIds = ElementIdToString(sideWalkModelCurves);
            var sideWalkCurves = sideWalkModelCurves.OfType<ModelCurve>().ToList();

            return sideWalkCurves;
        }

        // Проверка на то существуют линии в модели
        public static bool IsElemsExistInModel(Document doc, IEnumerable<int> elems, Type type)
        {
            if (elems is null)
            {
                return false;
            }

            foreach (var elem in elems)
            {
                ElementId id = new ElementId(elem);
                Element curElem = doc.GetElement(id);
                if (curElem is null || !(curElem.GetType().IsSubclassOf(type) || curElem.GetType() == type))
                {
                    return false;
                }
            }

            return true;
        }

        // Получение линий из Settings
        public static List<ModelCurve> GetCurvesById(Document doc, string elemIdsInSettings)
        {
            var elemId = GetIdsByString(elemIdsInSettings);
            var modelCurvesId = elemId.Select(id => new ElementId(id));
            var modelCurveElems = modelCurvesId.Select(id => doc.GetElement(id)).OfType<ModelCurve>().ToList();

            return modelCurveElems;
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

        // Получение id элементов на основе списка в виде строки
        public static List<int> GetIdsByString(string elems)
        {
            if (string.IsNullOrEmpty(elems))
            {
                return null;
            }

            var elemIds = elems.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => int.Parse(s.Remove(0, 2)))
                         .ToList();

            return elemIds;
        }

        // Создать форму на основе линий профиля
        public static Form CreateExtrusionForm(Document doc, IEnumerable<ModelCurve> curves, double extrusionHeight)
        {
            ReferenceArray referenceArray = new ReferenceArray();
            foreach (var curve in curves)
            {
                referenceArray.Append(curve.GeometryCurve.Reference);
            }

            XYZ direction = new XYZ(0, 0, extrusionHeight);

            Form form = doc.FamilyCreate.NewExtrusionForm(false, referenceArray, direction);

            return form;
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
