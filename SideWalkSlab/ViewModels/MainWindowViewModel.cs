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

        #region Создать край плиты
        public ICommand CreateSideWalkCommand { get; }

        private void OnCreateSideWalkCommandExecuted(object parameter)
        {

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
            RevitCommand.mainView.Close();
        }

        private bool CanCloseWindowCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #endregion

        #region Конструктор класса MainWindowViewModel
        public MainWindowViewModel(RevitModelForfard revitModel)
        {
            RevitModel = revitModel;

            #region Команды

            GetEdgeCommand = new LambdaCommand(OnGetEdgeCommandExecuted, CanGetEdgeCommandExecute);

            CreateSideWalkCommand = new LambdaCommand(OnCreateSideWalkCommandExecuted, CanCreateSideWalkCommandExecute);

            CloseWindowCommand = new LambdaCommand(OnCloseWindowCommandExecuted, CanCloseWindowCommandExecute);

            #endregion
        }

        public MainWindowViewModel() { }
        #endregion
    }
}
