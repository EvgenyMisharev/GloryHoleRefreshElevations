using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GloryHoleRefreshElevations
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class GloryHoleRefreshElevationsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Получение текущего документа
            Document doc = commandData.Application.ActiveUIDocument.Document;
            Guid heightOfBaseLevelGuid = new Guid("9f5f7e49-616e-436f-9acc-5305f34b6933");
            Guid levelOffsetGuid = new Guid("515dc061-93ce-40e4-859a-e29224d80a10");

            List<FamilyInstance> intersectionPointFamilyInstanceList = new FilteredElementCollector(doc)
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

            List<FamilyInstance> intersectionPointWeandrevitList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(ip => ip.Symbol.Family.Name == "231_Отверстие прямоугольное (Окно_Стена)"
                || ip.Symbol.Family.Name == "231_Отверстие круглое с гильзой в стене (Окно_Стена)")
                .ToList();

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Обновление отметок");
                foreach (FamilyInstance intersectionPoint in intersectionPointFamilyInstanceList)
                {
                    intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                    if(intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Прямоугольное"
                        || intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Круглое")

                    {
                        if(intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                        {
                            intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8);
                        }
                    }
                    else if(intersectionPoint.Symbol.FamilyName == "Отверстие_Плита_Прямоугольное"
                        || intersectionPoint.Symbol.FamilyName == "Отверстие_Плита_Круглое"
                        || intersectionPoint.Symbol.FamilyName == "Гильза_Плита")
                    {
                        if (intersectionPoint.Host != null)
                        {
                            if(intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                            {
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(doc.GetElement(intersectionPoint.Host.Id).get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble());
                            }
                        }
                        else
                        {
                            if(intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                            {
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(0);
                            }
                        }
                    }
                    else
                    {
                        if(intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                        {
                            intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble());
                        }
                    }
                }

                foreach (FamilyInstance intersectionPoint in intersectionPointWeandrevitList)
                {
                    if(intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                    {
                        intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble());
                    }
                }
                t.Commit();
            }
            return Result.Succeeded;
        }
    }
}
