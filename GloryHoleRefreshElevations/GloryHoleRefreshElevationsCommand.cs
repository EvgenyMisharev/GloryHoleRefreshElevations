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

            List<FamilyInstance> intersectionPointRectangularWallFamilyInstanceList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(ip => ip.Symbol.Family.Name == "Пересечение_Стена_Прямоугольное"
                || ip.Symbol.Family.Name == "Пересечение_Стена_Круглое"
                || ip.Symbol.Family.Name == "Пересечение_Плита_Прямоугольное"
                || ip.Symbol.Family.Name == "Пересечение_Плита_Круглое")
                .ToList();

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Обновление отметок");
                foreach (FamilyInstance intersectionPoint in intersectionPointRectangularWallFamilyInstanceList)
                {
                    intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                    if(intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Прямоугольное"
                        || intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Круглое")
                    {
                        intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8);
                    }
                    else
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
