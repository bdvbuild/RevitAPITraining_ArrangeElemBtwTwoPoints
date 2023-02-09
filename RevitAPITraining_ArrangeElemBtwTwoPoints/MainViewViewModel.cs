using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using View = Autodesk.Revit.DB.View;

namespace RevitAPITraining_ArrangeElemBtwTwoPoints
{
    internal class MainViewViewModel
    {
        private readonly ExternalCommandData _commandData;
        public DelegateCommand GetPoints { get; private set; }
        public XYZ Point1 { get; set; }
        public XYZ Point2 { get; set; }
        public List<FamilySymbol> FamilyList { get; } = new List<FamilySymbol>();
        public FamilySymbol SelectedFamily { get; set; }
        public int NumElem { get; set; } = 2;
        public DelegateCommand SaveCommand { get; }
        private RevitTask RevitTask { get; set; }
        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;

            GetPoints = new DelegateCommand(OnGetPoints);
            FamilyList = Utils.GetFamilyList(commandData);
            SaveCommand = new DelegateCommand(OnSaveCommand);
            RevitTask = new RevitTask();
        }

        private void OnGetPoints()
        {
            RaiseHideRequest();

            Point1 = Utils.GetPoint(_commandData);
            Point2 = Utils.GetPoint(_commandData);

            RaiseShowRequest();
        }

        public async void OnSaveCommand()
        {
            RaiseHideRequest();

            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            //Получение уровеня активного вида (для использования в качестве уровня для размещения семейства)
            View activeView = doc.ActiveView;
            ElementId levelId = activeView.LevelId;
            Level level = doc.GetElement(levelId) as Level;

            if (SelectedFamily == null)
            {
                MessageBox.Show("Выберите семейство", "Ошибка ввода");
                RaiseShowRequest();
                return;
            }

            if (Point1 == null || Point2 == null)
            {
                MessageBox.Show("Укажите точки", "Ошибка ввода");
                RaiseShowRequest();
                return;
            }

            //Создание вектора
            XYZ direction = Point2 - Point1;
            //Получение расстояния между точками
            double distance = direction.GetLength();
            //Вычисление шага
            double step = distance / (NumElem - 1);


            string mess = string.Empty;
            try
            {
                await RevitTask.Run(app =>
                {
                    //Размещение семейств
                    for (int i = 0; i < NumElem; i++)
                    {
                        using (Transaction ts = new Transaction(doc, "Set family"))
                        {
                            ts.Start();
                            if (!SelectedFamily.IsActive)
                            {
                                SelectedFamily.Activate();
                                doc.Regenerate();
                            }
                            XYZ location = Point1 + (direction.Normalize() * step * i);
                            FamilyInstance fi = doc.Create.NewFamilyInstance(location, SelectedFamily, level, StructuralType.NonStructural);
                            ts.Commit();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                mess = ex.Message;
            }
            RaiseShowRequest();
        }

        public event EventHandler CloseRequest;
        public void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler HideRequest;
        public void RaiseHideRequest()
        {
            HideRequest?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler ShowRequest;
        public void RaiseShowRequest()
        {
            ShowRequest?.Invoke(this, EventArgs.Empty);
        }


    }
    public class Utils
    {
        public static XYZ GetPoint(ExternalCommandData commandData)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            XYZ pickedPoint = uidoc.Selection.PickPoint("Выберите точку");
            return pickedPoint;
        }
        public static List<FamilySymbol> GetFamilyList(ExternalCommandData commandData)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;

            var familyList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .ToList();
            return familyList;
        }
    }
}
