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
        public XYZ point1 { get; }
        public XYZ point2 { get; }
        public List<FamilySymbol> familyList { get; } = new List<FamilySymbol>();
        public FamilySymbol selectedFamily { get; set; }
        public DelegateCommand saveCommand { get; }
        private int _numElem = 2;
        public int numElem
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
            point1 = Utils.GetPoint(commandData);
            point2 = Utils.GetPoint(commandData);

            familyList = Utils.GetFamilyList(commandData);
            saveCommand = new DelegateCommand(OnSaveCommand);
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

            if (selectedFamily == null)
            {
                return;
            }

            //Создание вектора
            XYZ direction = point2 - point1;
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
                    if (!selectedFamily.IsActive)
                    {
                        selectedFamily.Activate();
                        doc.Regenerate();
                    }
                    XYZ location = point1 + (direction.Normalize() * step * i);
                    FamilyInstance fi = doc.Create.NewFamilyInstance(location, selectedFamily, level, StructuralType.NonStructural);
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
