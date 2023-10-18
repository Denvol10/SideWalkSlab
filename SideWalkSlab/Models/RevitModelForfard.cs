using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using System.Collections.ObjectModel;
using SideWalkSlab.Models;
using System.IO;
using System.Windows.Media;

namespace SideWalkSlab
{
    public class RevitModelForfard
    {
        private UIApplication Uiapp { get; set; } = null;
        private Application App { get; set; } = null;
        private UIDocument Uidoc { get; set; } = null;
        private Document Doc { get; set; } = null;

        public RevitModelForfard(UIApplication uiapp)
        {
            Uiapp = uiapp;
            App = uiapp.Application;
            Uidoc = uiapp.ActiveUIDocument;
            Doc = uiapp.ActiveUIDocument.Document;
        }

        #region Ребро для построения края плиты
        public Edge EdgeForSweep { get; set; }

        private string _edgeRepresentation;
        public string EdgeRepresentation
        {
            get => _edgeRepresentation;
            set => _edgeRepresentation = value;
        }
        #endregion

        #region Получение ребра с помощью пользовательского выбора
        public void GetEdgeBySelection()
        {
            EdgeForSweep = RevitGeometryUtils.GetEdgeBySelection(Uiapp, out _edgeRepresentation);
        }

        #region Проверка на то существует ли ребро в модели
        public bool IsEdgeExistInModel(string edgeRepresentation)
        {
            return RevitGeometryUtils.IsEdgeExistInModel(Doc, edgeRepresentation);
        }
        #endregion

        #region Получение ребра из Settings
        public void GetEdgeBySettings(string edgeRepresentation)
        {
            Reference reference = Reference.ParseFromStableRepresentation(Doc, edgeRepresentation);

            EdgeForSweep = Doc.GetElement(reference).GetGeometryObjectFromReference(reference) as Edge;
        }
        #endregion

        #region Линии края плиты
        public List<Curve> SideWalkLines { get; set; }

        private string _sideWalkLineElemIds;
        public string SideWalkLineElemIds
        {
            get => _sideWalkLineElemIds;
            set => _sideWalkLineElemIds = value;
        }
        #endregion

        #region Получение линий края плиты с помощью пользовательского выбора
        public void GetSideWalkLinesBySelection()
        {
            SideWalkLines = RevitGeometryUtils.GetSideWalkLinesBySelection(Uiapp, out _sideWalkLineElemIds);
        }
        #endregion

        #region Проверка на то, существуют линии в модели
        public bool IsModelCurvesExistInModel(string modelCurvesIds)
        {
            var elemIds = RevitGeometryUtils.GetIdsByString(modelCurvesIds);

            return RevitGeometryUtils.IsElemsExistInModel(Doc, elemIds, typeof(ModelCurve));
        }
        #endregion

        #region Получение линий края плиты из Settings
        public void GetSideWalkLinesBySettings(string elemIdsInSettings)
        {
            SideWalkLines = RevitGeometryUtils.GetCurvesById(Doc, elemIdsInSettings);
        }
        #endregion

        public void CreateSideWalk(FamilySymbolSelector sideWalkFamilySelector)
        {
            FamilySymbol sideWalkFamilySymbol = GetFamilySymbolByName(sideWalkFamilySelector);
            var sideWalkCurves = new List<Curve>();

            string resultPath = @"O:\Revit Infrastructure Tools\SideWalkSlab\SideWalkSlab\result.txt";

            FamilyInstance sideWalkInstance = null;
            using (Transaction trans = new Transaction(Doc, "Create Side Walk Instance"))
            {
                trans.Start();
                if (!sideWalkFamilySymbol.IsActive)
                {
                    sideWalkFamilySymbol.Activate();
                }

                sideWalkInstance = Doc.FamilyCreate.NewFamilyInstance(XYZ.Zero, sideWalkFamilySymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                trans.Commit();
            }

            using (Transaction trans = new Transaction(Doc, "Get Curves and Delete Side Walk Instance"))
            {
                trans.Start();
                var sideWalkCurvesGeometryInstanse = sideWalkInstance.get_Geometry(new Options()).OfType<GeometryInstance>().First();
                sideWalkCurves = sideWalkCurvesGeometryInstanse.GetSymbolGeometry().OfType<Curve>().Select(c => c.Clone()).ToList();
                Doc.Delete(sideWalkInstance.Id);
                trans.Commit();
            }

            Curve edgeCurve = EdgeForSweep.AsCurve().Clone();
            double curveLength = edgeCurve.Length;
            double extension = UnitUtils.ConvertToInternalUnits(1, UnitTypeId.Meters);
            edgeCurve.MakeUnbound();
            edgeCurve.MakeBound(-extension, curveLength + extension);
            double step = 1.5;
            step = UnitUtils.ConvertToInternalUnits(step, UnitTypeId.Meters);
            int count = (int)(curveLength / step);
            var normalparameters = RevitGeometryUtils.GenerateNormalizeParameters(count);
            var rowParameters = normalparameters.Select(p => edgeCurve.ComputeRawParameter(p));


            var transforms = rowParameters.Select(p => edgeCurve.ComputeDerivatives(p, false));

            //using (StreamWriter sw = new StreamWriter(resultPath, false, Encoding.Default))
            //{
            //    foreach (var transform in transforms)
            //    {
            //        sw.WriteLine(transform.BasisX.Normalize());
            //        sw.WriteLine(new XYZ(transform.BasisX.X, transform.BasisX.Y, 0).Normalize());
            //    }
            //}

            using (Transaction trans = new Transaction(Doc, "Form Created"))
            {
                trans.Start();
                foreach (var transform in transforms)
                {
                    XYZ basePoint = transform.Origin;
                    XYZ vec1 = new XYZ(transform.BasisX.X, transform.BasisX.Y, 0).Normalize();
                    XYZ vec2 = XYZ.BasisZ;
                    XYZ vec3 = vec2.CrossProduct(vec1).Normalize();

                    Frame frame = new Frame(basePoint, vec3, vec2, vec1);

                    Plane plane = Plane.Create(frame);

                    //Doc.FamilyCreate.NewReferencePoint(plane.Origin);
                    //Doc.FamilyCreate.NewReferencePoint(plane.Origin + plane.YVec);

                    foreach (var curve in sideWalkCurves)
                    {
                        XYZ firstPoint = curve.GetEndPoint(0);
                        XYZ sideWalkFirstPoint = plane.Origin - plane.XVec * firstPoint.X + plane.YVec * firstPoint.Y;
                        Doc.FamilyCreate.NewReferencePoint(sideWalkFirstPoint);
                    }


                }


                trans.Commit();
            }
        }
        #endregion

        #region Получение списка названий типоразмеров семейств
        public ObservableCollection<FamilySymbolSelector> GetFamilySymbolNames()
        {
            var familySymbolNames = new ObservableCollection<FamilySymbolSelector>();
            var allFamilies = new FilteredElementCollector(Doc).OfClass(typeof(Family)).OfType<Family>();
            var structuralFramingFamilies = allFamilies.Where(f => f.FamilyCategory.Id.IntegerValue
                                                              == (int)BuiltInCategory.OST_GenericModel);
            if (structuralFramingFamilies.Count() == 0)
                return familySymbolNames;

            foreach (var family in structuralFramingFamilies)
            {
                foreach (var symbolId in family.GetFamilySymbolIds())
                {
                    var familySymbol = Doc.GetElement(symbolId);
                    familySymbolNames.Add(new FamilySymbolSelector(family.Name, familySymbol.Name));
                }
            }

            return familySymbolNames;
        }
        #endregion

        #region Получение типоразмера по имени
        private FamilySymbol GetFamilySymbolByName(FamilySymbolSelector familyAndSymbolName)
        {
            var familyName = familyAndSymbolName.FamilyName;
            var symbolName = familyAndSymbolName.SymbolName;

            Family family = new FilteredElementCollector(Doc).OfClass(typeof(Family)).Where(f => f.Name == familyName).First() as Family;
            var symbolIds = family.GetFamilySymbolIds();
            foreach (var symbolId in symbolIds)
            {
                FamilySymbol fSymbol = (FamilySymbol)Doc.GetElement(symbolId);
                if (fSymbol.get_Parameter(BuiltInParameter.SYMBOL_NAME_PARAM).AsString() == symbolName)
                {
                    return fSymbol;
                }
            }
            return null;
        }
        #endregion
    }
}
