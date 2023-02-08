using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using View = Autodesk.Revit.DB.View;

namespace RevitAPITraining_ArrangeElemBtwTwoPoints
{
    internal class MainViewViewModel
    {
        private ExternalCommandData _commandData;
        public XYZ Point1 { get; }
        public XYZ Point2 { get; }
        public List<FamilySymbol> FamilyList { get; } = new List<FamilySymbol>();
        public FamilySymbol SelectedFamily { get; set; }
        public DelegateCommand SaveCommand { get; }
        private int _numElem = 2;
        public int NumElem
        {
            get => _numElem;
            set
            {
                bool val = false;
                do
                {
                    if (int.TryParse(value.ToString(), out int result))
                    {
                        if (value <= 2)
                        {
                            _numElem = 2;
                            val = true;
                            break;
                        }
                        else
                        {
                            _numElem = result;
                            val = true;
                            break;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Введите целое число");
                    }
                } while (!val);
            }
        }
        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            Point1 = Utils.GetPoint(commandData);
            Point2 = Utils.GetPoint(commandData);

            FamilyList = Utils.GetFamilyList(commandData);
            SaveCommand = new DelegateCommand(OnSaveCommand);
        }

        public void OnSaveCommand()
        {
            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            //Получение уровеня активного вида (для использования в качестве уровня для размещения семейства)
            View activeView = doc.ActiveView;
            ElementId levelId = activeView.LevelId;
            Level level = doc.GetElement(levelId) as Level;

            if (SelectedFamily == null)
            {
                return;
            }

            //Создание вектора
            XYZ direction = Point2 - Point1;
            //Получение расстояния между точками
            double distance = direction.GetLength();
            //Вычисление шага
            double step = distance / (_numElem - 1);

            //Размещение семейств
            for (int i = 0; i < _numElem; i++)
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
            RaiseCloseRequest();
        }

        public event EventHandler CloseRequest;
        public void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
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
