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
using System.Windows.Input;
using SideWalkSlab.Infrastructure;
using SideWalkSlab.Models;

namespace SideWalkSlab.ViewModels
{
    internal class MainWindowViewModel : Base.ViewModel
    {
        private RevitModelForfard _revitModel;

        internal RevitModelForfard RevitModel
        {
            get => _revitModel;
            set => _revitModel = value;
        }

        #region Заголовок
        private string _title = "Край плиты";

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }
        #endregion

        #region Ребро балки
        private string _edgeRepresentation;

        public string EdgeRepresentation
        {
            get => _edgeRepresentation;
            set => Set(ref _edgeRepresentation, value);
        }
        #endregion

        #region Список семейств и их типоразмеров
        private ObservableCollection<FamilySymbolSelector> _sideWalkFamilySymbols = new ObservableCollection<FamilySymbolSelector>();
        public ObservableCollection<FamilySymbolSelector> SideWalkFamilySymbols
        {
            get => _sideWalkFamilySymbols;
            set => Set(ref _sideWalkFamilySymbols, value);
        }
        #endregion

        #region Выбранный типоразмер семейства
        private FamilySymbolSelector _familySymbolName;
        public FamilySymbolSelector FamilySymbolName
        {
            get => _familySymbolName;
            set => Set(ref _familySymbolName, value);
        }
        #endregion

        #region Индекс в списке выбранного типоразмера сечения края плиты
        private int _familySymbolIndex = Properties.Settings.Default.FamilySymbolIndex;
        #endregion

        #region Развернуть край плиты
        private bool _reverseSideWalk = Properties.Settings.Default.ReverseSideWalk;
        public bool ReverseSideWalk
        {
            get => _reverseSideWalk;
            set => Set(ref _reverseSideWalk, value);
        }
        #endregion

        #region Шаг растановки сечений края плиты
        private double _sectionStep = Properties.Settings.Default.SectionStep;
        public double SectionStep
        {
            get => _sectionStep;
            set => Set(ref _sectionStep, value);
        }
        #endregion

        #region Линии подрезки 1
        private string _cutLineIds1;
        public string CutLineIds1
        {
            get => _cutLineIds1;
            set => Set(ref _cutLineIds1, value);
        }
        #endregion

        #region Линии подрезки 2
        private string _cutLineIds2;
        public string CutLineIds2
        {
            get => _cutLineIds2;
            set => Set(ref _cutLineIds2, value);
        }
        #endregion

        #region Команды

        #region Получить ребро элемента
        public ICommand GetEdgeCommand { get; }

        private void OnGetEdgeCommandExecuted(object parameter)
        {
            RevitCommand.mainView.Hide();
            RevitModel.GetEdgeBySelection();
            EdgeRepresentation = RevitModel.EdgeRepresentation;
            RevitCommand.mainView.ShowDialog();
        }

        private bool CanGetEdgeCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Получить линии подрезки 1
        public ICommand GetCutLines1Command { get; }

        private void OnGetCutLines1CommandExecuted(object parameter)
        {
            RevitCommand.mainView.Hide();
            RevitModel.GetCutLines1BySelection();
            CutLineIds1 = RevitModel.CutLinesIds1;
            RevitCommand.mainView.ShowDialog();
        }

        private bool CanGetCutLines1CommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Получить линии подрезки 2
        public ICommand GetCutLines2Command { get; }

        private void OnGetCutLines2CommandExecuted(object parameter)
        {
            RevitCommand.mainView.Hide();
            RevitModel.GetCutLines2BySelection();
            CutLineIds2 = RevitModel.CutLinesIds2;
            RevitCommand.mainView.ShowDialog();
        }

        private bool CanGetCutLines2CommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Создать край плиты
        public ICommand CreateSideWalkCommand { get; }

        private void OnCreateSideWalkCommandExecuted(object parameter)
        {
            RevitModel.CreateSideWalk(FamilySymbolName, ReverseSideWalk, SectionStep);
            SaveSettings();
            RevitCommand.mainView.Close();
        }

        private bool CanCreateSideWalkCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Закрыть окно
        public ICommand CloseWindowCommand { get; }

        private void OnCloseWindowCommandExecuted(object parameter)
        {
            SaveSettings();
            RevitCommand.mainView.Close();
        }

        private bool CanCloseWindowCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #endregion

        private void SaveSettings()
        {
            Properties.Settings.Default.EdgeRepresentation = EdgeRepresentation;
            Properties.Settings.Default.FamilySymbolIndex = SideWalkFamilySymbols.IndexOf(FamilySymbolName);
            Properties.Settings.Default.ReverseSideWalk = ReverseSideWalk;
            Properties.Settings.Default.SectionStep = SectionStep;
            Properties.Settings.Default.CutLineIds1 = CutLineIds1;
            Properties.Settings.Default.CutLineIds2 = CutLineIds2;
            Properties.Settings.Default.Save();
        }

        #region Конструктор класса MainWindowViewModel
        public MainWindowViewModel(RevitModelForfard revitModel)
        {
            RevitModel = revitModel;

            SideWalkFamilySymbols = RevitModel.GetFamilySymbolNames();

            #region Инициализация объектов из Settings

            #region Получение ребра из Settings
            if (!(Properties.Settings.Default.EdgeRepresentation is null))
            {
                string edgeRepresentation = Properties.Settings.Default.EdgeRepresentation;
                if (RevitModel.IsEdgeExistInModel(edgeRepresentation) && !string.IsNullOrEmpty(edgeRepresentation))
                {
                    EdgeRepresentation = edgeRepresentation;
                    RevitModel.GetEdgeBySettings(edgeRepresentation);
                }
            }
            #endregion

            #region Инициализация значения типоразмера края плиты
            if (_familySymbolIndex >= 0 && _familySymbolIndex <= SideWalkFamilySymbols.Count - 1)
            {
                FamilySymbolName = SideWalkFamilySymbols.ElementAt(_familySymbolIndex);
            }
            #endregion

            #region Инициализация линий подрезки 1 из Settings
            if (!(Properties.Settings.Default.CutLineIds1 is null))
            {
                string cutLineIdsInSettings = Properties.Settings.Default.CutLineIds1;
                if (RevitModel.IsCutLinesExistInModel(cutLineIdsInSettings) && !string.IsNullOrEmpty(cutLineIdsInSettings))
                {
                    CutLineIds1 = cutLineIdsInSettings;
                    RevitModel.GetCutLines1BySettings(cutLineIdsInSettings);
                }
            }
            #endregion

            #region Инициализация линий подрезки 2 из Settings
            if (!(Properties.Settings.Default.CutLineIds2 is null))
            {
                string cutLineIdsInSettings = Properties.Settings.Default.CutLineIds2;
                if (RevitModel.IsCutLinesExistInModel(cutLineIdsInSettings) && !string.IsNullOrEmpty(cutLineIdsInSettings))
                {
                    CutLineIds2 = cutLineIdsInSettings;
                    RevitModel.GetCutLines2BySettings(cutLineIdsInSettings);
                }
            }
            #endregion

            #endregion

            #region Команды

            GetEdgeCommand = new LambdaCommand(OnGetEdgeCommandExecuted, CanGetEdgeCommandExecute);

            GetCutLines1Command = new LambdaCommand(OnGetCutLines1CommandExecuted, CanGetCutLines1CommandExecute);

            GetCutLines2Command = new LambdaCommand(OnGetCutLines2CommandExecuted, CanGetCutLines2CommandExecute);

            CreateSideWalkCommand = new LambdaCommand(OnCreateSideWalkCommandExecuted, CanCreateSideWalkCommandExecute);

            CloseWindowCommand = new LambdaCommand(OnCloseWindowCommandExecuted, CanCloseWindowCommandExecute);

            #endregion
        }

        public MainWindowViewModel() { }
        #endregion
    }
}
