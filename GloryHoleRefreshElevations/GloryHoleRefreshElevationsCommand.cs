using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GloryHoleRefreshElevations
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class GloryHoleRefreshElevationsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                GetPluginStartInfo();
            }
            catch { }
            // Получение текущего документа
            Document doc = commandData.Application.ActiveUIDocument.Document;
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            Guid heightOfBaseLevelGuid = new Guid("9f5f7e49-616e-436f-9acc-5305f34b6933");
            Guid levelOffsetGuid = new Guid("515dc061-93ce-40e4-859a-e29224d80a10");

            GloryHoleRefreshElevationsWPF gloryHoleRefreshElevationsWPF = new GloryHoleRefreshElevationsWPF();
            gloryHoleRefreshElevationsWPF.ShowDialog();
            if (gloryHoleRefreshElevationsWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }

            string refreshElevationsOptionButtonName = gloryHoleRefreshElevationsWPF.RefreshElevationsOptionButtonName;
            string roundHolesPositionButtonName = gloryHoleRefreshElevationsWPF.RoundHolesPositionButtonName;
            double roundHolePositionIncrement = gloryHoleRefreshElevationsWPF.RoundHolePositionIncrement;

            List<FamilyInstance> intersectionPointFamilyInstanceList = null;
            List<FamilyInstance> intersectionPointWeandrevitList = null;

            if (refreshElevationsOptionButtonName == "rbt_AllProject")
            {
                intersectionPointFamilyInstanceList = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_GenericModel)
                    .OfClass(typeof(FamilyInstance))
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .Where(ip => ip.Symbol.Family.Name == "Пересечение_Стена_Прямоугольное"
                    || ip.Symbol.Family.Name == "Пересечение_Стена_Круглое"
                    || ip.Symbol.Family.Name == "Пересечение_Плита_Прямоугольное"
                    || ip.Symbol.Family.Name == "Пересечение_Плита_Круглое"
                    || ip.Symbol.Family.Name == "Отверстие_Стена_Прямоугольное"
                    || ip.Symbol.Family.Name == "Отверстие_Стена_Круглое"
                    || ip.Symbol.Family.Name == "Отверстие_Плита_Прямоугольное"
                    || ip.Symbol.Family.Name == "Отверстие_Плита_Круглое"
                    || ip.Symbol.Family.Name == "Гильза_Стена"
                    || ip.Symbol.Family.Name == "Гильза_Плита")
                    .ToList();

                intersectionPointWeandrevitList = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Windows)
                    .OfClass(typeof(FamilyInstance))
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .Where(ip => ip.Symbol.Family.Name == "231_Отверстие прямоугольное (Окно_Стена)"
                    || ip.Symbol.Family.Name == "231_Отверстие круглое с гильзой в стене (Окно_Стена)")
                    .ToList();

            }
            else
            {
                HolesSelectionFilter holesSelectionFilter = new HolesSelectionFilter();
                IList<Reference> selHoles = null;
                try
                {
                    selHoles = sel.PickObjects(ObjectType.Element, holesSelectionFilter, "Выберите отверстия!");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                intersectionPointFamilyInstanceList = new List<FamilyInstance>();
                intersectionPointWeandrevitList = new List<FamilyInstance>();
                foreach (Reference roomRef in selHoles)
                {
                    if ((doc.GetElement(roomRef) as FamilyInstance) != null && (doc.GetElement(roomRef) as FamilyInstance).Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_GenericModel))
                    {
                        intersectionPointFamilyInstanceList.Add(doc.GetElement(roomRef) as FamilyInstance);
                    }
                    else if ((doc.GetElement(roomRef) as FamilyInstance) != null && (doc.GetElement(roomRef) as FamilyInstance).Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_Windows))
                    {
                        intersectionPointWeandrevitList.Add(doc.GetElement(roomRef) as FamilyInstance);
                    }
                }
            }

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Обновление отметок");
                foreach (FamilyInstance intersectionPoint in intersectionPointFamilyInstanceList)
                {
                    intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                    if (intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Прямоугольное"
                        || intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Круглое")

                    {
                        if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                        {
                            double elev = intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8;
                            intersectionPoint.get_Parameter(levelOffsetGuid).Set(elev);
                        }
                    }
                    else if (intersectionPoint.Symbol.FamilyName == "Отверстие_Плита_Прямоугольное"
                        || intersectionPoint.Symbol.FamilyName == "Отверстие_Плита_Круглое"
                        || intersectionPoint.Symbol.FamilyName == "Гильза_Плита")
                    {
                        if (intersectionPoint.Host != null)
                        {
                            if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                            {
                                double elev = doc.GetElement(intersectionPoint.Host.Id).get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble();
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(elev);
                            }
                        }
                        else
                        {
                            if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                            {
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(0);
                            }
                        }
                    }
                    else
                    {
                        if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                        {
                            double elev = intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();
                            if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                            {
                                elev = RoundToIncrement(elev, roundHolePositionIncrement);
                                intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(elev);
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(elev);
                            }
                            else
                            {
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(elev);
                            }
                        }
                    }
                }
                foreach (FamilyInstance intersectionPoint in intersectionPointWeandrevitList)
                {
                    double elev = intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();
                    if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                    {
                        if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                        {
                            elev = RoundToIncrement(elev, roundHolePositionIncrement);
                            intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(elev);
                            intersectionPoint.get_Parameter(levelOffsetGuid).Set(elev);
                        }
                        else
                        {
                            intersectionPoint.get_Parameter(levelOffsetGuid).Set(elev);
                        }
                    }
                }
                t.Commit();
            }
            return Result.Succeeded;
        }
        private static void GetPluginStartInfo()
        {
            // Получаем сборку, в которой выполняется текущий код
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string assemblyName = "GloryHoleRefreshElevations";
            string assemblyNameRus = "Обновить отметки";
            string assemblyFolderPath = Path.GetDirectoryName(thisAssembly.Location);

            int lastBackslashIndex = assemblyFolderPath.LastIndexOf("\\");
            string dllPath = assemblyFolderPath.Substring(0, lastBackslashIndex + 1) + "PluginInfoCollector\\PluginInfoCollector.dll";

            Assembly assembly = Assembly.LoadFrom(dllPath);
            Type type = assembly.GetType("PluginInfoCollector.InfoCollector");
            var constructor = type.GetConstructor(new Type[] { typeof(string), typeof(string) });

            if (type != null)
            {
                // Создание экземпляра класса
                object instance = Activator.CreateInstance(type, new object[] { assemblyName, assemblyNameRus });
            }
        }
        private double RoundToIncrement(double value, double increment)
        {
            if (increment == 0)
            {
                return Math.Round(value, 6);
            }
            else
            {
                return Math.Round(Math.Round(value * 304.8, 2) / increment) * increment / 304.8;
            }
        }
    }
}
