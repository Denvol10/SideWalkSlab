﻿using System;
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

        #region Грань для построения края плиты
        public Edge EdgeForSweep { get; set; }

        private string _edgeRepresentation;
        public string EdgeRepresentation
        {
            get => _edgeRepresentation;
            set => _edgeRepresentation = value;
        }
        #endregion

        #region Получение грани с помощью пользовательского выбора
        public void GetEdgeBySelection()
        {
            EdgeForSweep = RevitGeometryUtils.GetEdgeBySelection(Uiapp, out _edgeRepresentation);
        }
        #endregion

    }
}
