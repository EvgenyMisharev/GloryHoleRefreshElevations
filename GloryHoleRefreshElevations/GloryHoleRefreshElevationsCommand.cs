using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
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
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            Guid heightOfBaseLevelGuid = new Guid("9f5f7e49-616e-436f-9acc-5305f34b6933");
            Guid levelOffsetGuid = new Guid("515dc061-93ce-40e4-859a-e29224d80a10");

            GloryHoleRefreshElevationsWPF gloryHoleRefreshElevationsWPF = new GloryHoleRefreshElevationsWPF();
            gloryHoleRefreshElevationsWPF.ShowDialog();
            if (gloryHoleRefreshElevationsWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }
            string refreshElevationsButtonName = gloryHoleRefreshElevationsWPF.RefreshElevationsButtonName;

            List<FamilyInstance> intersectionPointFamilyInstanceList = new List<FamilyInstance>();
            List<FamilyInstance> intersectionPointWeandrevitList = new List<FamilyInstance>();
            if (refreshElevationsButtonName == "radioButton_Selected")
            {
                List<FamilyInstance> intersectionPointList = new List<FamilyInstance>();
                intersectionPointList = GetIntersectionPointCurrentSelection(doc, sel);
                if (intersectionPointList.Count == 0)
                {
                    //Выбор точек вырезания
                    GloryHoleSelectionFilter gloryHoleSelectionFilter = new GloryHoleSelectionFilter();
                    IList<Reference> intersectionPointRefList = null;
                    try
                    {
                        intersectionPointRefList = sel.PickObjects(ObjectType.Element, gloryHoleSelectionFilter, "Выберите элементы для обновления отметок!");
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }
                    foreach (Reference refElem in intersectionPointRefList)
                    {
                        intersectionPointList.Add(doc.GetElement(refElem) as FamilyInstance);
                    }
                }

                intersectionPointFamilyInstanceList = intersectionPointList
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

                intersectionPointWeandrevitList = intersectionPointList
                    .Where(ip => ip.Symbol.Family.Name == "231_Отверстие прямоугольное (Окно_Стена)"
                    || ip.Symbol.Family.Name == "231_Отверстие круглое с гильзой в стене (Окно_Стена)")
                    .ToList();
            }
            else
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

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Обновление отметок");
                foreach (FamilyInstance intersectionPoint in intersectionPointFamilyInstanceList)
                {
                    if (intersectionPoint.GroupId != ElementId.InvalidElementId)
                    {
                        ElementId groupId = intersectionPoint.GroupId;
                        Group group = doc.GetElement(groupId) as Group;

                        if (group != null)
                        {

                        }
                    }
                    intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                    if (intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Прямоугольное"
                        || intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Круглое")

                    {
                        if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                        {
                            intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8);
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
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(doc.GetElement(intersectionPoint.Host.Id).get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble());
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
                            intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble());
                        }
                    }
                }

                foreach (FamilyInstance intersectionPoint in intersectionPointWeandrevitList)
                {
                    if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                    {
                        intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble());
                    }
                }
                t.Commit();
            }
            return Result.Succeeded;
        }
        private static List<FamilyInstance> GetIntersectionPointCurrentSelection(Document doc, Selection sel)
        {
            ICollection<ElementId> selectedIds = sel.GetElementIds();
            List<FamilyInstance> tempIntersectionPointList = new List<FamilyInstance>();
            foreach (ElementId intersectionPointId in selectedIds)
            {
                if (doc.GetElement(intersectionPointId) is FamilyInstance
                    && null != doc.GetElement(intersectionPointId).Category
                    && (doc.GetElement(intersectionPointId).Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_GenericModel)
                    || doc.GetElement(intersectionPointId).Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_Windows))
                    && ((doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Стена_Прямоугольное")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Стена_Круглое")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Плита_Прямоугольное")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Плита_Круглое")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Отверстие_Стена_Прямоугольное")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Отверстие_Стена_Круглое")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Отверстие_Плита_Прямоугольное")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Отверстие_Плита_Круглое")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Гильза_Стена")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Гильза_Плита")))

                {
                    tempIntersectionPointList.Add(doc.GetElement(intersectionPointId) as FamilyInstance);
                }
            }
            return tempIntersectionPointList;
        }
    }
}
