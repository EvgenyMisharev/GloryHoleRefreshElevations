using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GloryHoleRefreshElevations
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class GloryHoleRefreshElevationsCommand : IExternalCommand
    {
        Guid heightOfBaseLevelGuid = new Guid("9f5f7e49-616e-436f-9acc-5305f34b6933");
        Guid levelOffsetGuid = new Guid("515dc061-93ce-40e4-859a-e29224d80a10");
        Guid intersectionPointWidthGuid = new Guid("8f2e4f93-9472-4941-a65d-0ac468fd6a5d");
        Guid intersectionPointHeightGuid = new Guid("da753fe3-ecfa-465b-9a2c-02f55d0c2ff1");

        Guid gh_FamilyCode = new Guid("40bbbf16-4b6a-45e8-9896-620bb448db96");

        // Список допустимых значений параметра типа
        HashSet<string> validCodes = new HashSet<string>
            {
                "111", "113", "115", // Пересечение_Стена_Прямоугольное
                "112", "114", "116", // Пересечение_Стена_Круглое
                "121", "123",        // Пересечение_Плита_Прямоугольное
                "122", "124",        // Пересечение_Плита_Круглое
                "126",              // Отверстие_Стена_Круглое
                "221", "223",       // Отверстие_Плита_Прямоугольное
                "222", "224"        // Отверстие_Плита_Круглое
            };

        // Список допустимых семейств
        HashSet<string> validFamilies = new HashSet<string>
            {
                "Пересечение_Стена_Прямоугольное",
                "Пересечение_Стена_Круглое",
                "Пересечение_Плита_Прямоугольное",
                "Пересечение_Плита_Круглое",
                "Отверстие_Стена_Прямоугольное",
                "Отверстие_Стена_Круглое",
                "Отверстие_Плита_Прямоугольное",
                "Отверстие_Плита_Круглое",
                "Гильза_Стена",
                "Гильза_Плита"
            };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                _ = GetPluginStartInfo();
            }
            catch { }

            // Получение текущего документа
            Document doc = commandData.Application.ActiveUIDocument.Document;
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            List<Grid> grids = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Grids)
                .OfClass(typeof(Grid))
                .Cast<Grid>()
                .ToList();

            GloryHoleRefreshElevationsWPF gloryHoleRefreshElevationsWPF = new GloryHoleRefreshElevationsWPF();
            gloryHoleRefreshElevationsWPF.ShowDialog();
            if (gloryHoleRefreshElevationsWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }

            string refreshElevationsOptionButtonName = gloryHoleRefreshElevationsWPF.RefreshElevationsOptionButtonName;
            string roundHolesPositionButtonName = gloryHoleRefreshElevationsWPF.RoundHolesPositionButtonName;
            double roundHolePositionIncrement = gloryHoleRefreshElevationsWPF.RoundHolePositionIncrement;

            string roundHolesLocationButtonName = gloryHoleRefreshElevationsWPF.RoundHolesLocationButtonName;
            double roundHoleLocationIncrement = gloryHoleRefreshElevationsWPF.RoundHoleLocationIncrement;

            List<FamilyInstance> intersectionPointFamilyInstanceList = null;
            List<FamilyInstance> intersectionPointWeandrevitList = null;

            if (refreshElevationsOptionButtonName == "rbt_AllProject")
            {
                intersectionPointFamilyInstanceList = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_GenericModel)
                    .OfClass(typeof(FamilyInstance))
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .Where(ip =>
                    {
                        string familyName = ip.Symbol.Family.Name;
                        string familyCode = ip.Symbol.get_Parameter(gh_FamilyCode)?.AsString();

                        // 1. Если семейство есть в списке — сразу берем
                        if (validFamilies.Contains(familyName))
                            return true;

                        // 2. Если параметр задан и его значение допустимо — берем
                        if (!string.IsNullOrEmpty(familyCode) && validCodes.Contains(familyCode))
                            return true;

                        // Если не подошло ни одно из условий — отбрасываем
                        return false;
                    })
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
                    // Получаем значение параметра gh_FamilyCode
                    string familyCode = intersectionPoint.Symbol.get_Parameter(gh_FamilyCode)?.AsString();

                    // Устанавливаем отметку уровня (Elevation)
                    intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);

                    // Если у элемента есть код, обрабатываем его по группам
                    if (!string.IsNullOrEmpty(familyCode))
                    {
                        if (familyCode == "111" || familyCode == "112" || familyCode == "113" || familyCode == "114" || familyCode == "115" || familyCode == "116")
                        {
                            // Обычные отверстия в стенах (пересечения)
                            if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                            {
                                double elev = intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();
                                if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                                {
                                    elev = RoundToIncrement(elev, roundHolePositionIncrement);
                                    intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(elev);
                                }
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(elev);
                            }
                            if (roundHolesLocationButtonName == "radioButton_RoundHolesLocationYes")
                            {
                                XYZ originIntersection = (intersectionPoint.Location as LocationPoint)?.Point;
                                if (originIntersection != null)
                                {
                                    if (familyCode == "111" || familyCode == "113" || familyCode == "115")
                                    {
                                        RoundHolesPositionInWalls(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, true);
                                    }
                                    else if (familyCode == "112" || familyCode == "114" || familyCode == "116")
                                    {
                                        RoundHolesPositionInWalls(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, false);
                                    }
                                }
                            }
                        }
                        else if (familyCode == "121" || familyCode == "122" || familyCode == "123" || familyCode == "124")
                        {
                            // Отверстия в плитах (пересечения)
                            if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                            {
                                double elev = intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8;
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(elev);
                            }
                            if (roundHolesLocationButtonName == "radioButton_RoundHolesLocationYes")
                            {
                                XYZ originIntersection = (intersectionPoint.Location as LocationPoint)?.Point;
                                if (originIntersection != null)
                                {
                                    if (familyCode == "121" || familyCode == "123")
                                    {
                                        RoundHolesPositionInSlabs(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, true);
                                    }
                                    else if (familyCode == "122" || familyCode == "124")
                                    {
                                        RoundHolesPositionInSlabs(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, false);
                                    }
                                }
                            }
                        }
                        else if (familyCode == "221" || familyCode == "222" || familyCode == "223" || familyCode == "224")
                        {
                            // Отверстия в плитах, связанные с Host (оконные отверстия)
                            if (intersectionPoint.Host != null)
                            {
                                if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                                {
                                    double elev = doc.GetElement(intersectionPoint.Host.Id).get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble();
                                    intersectionPoint.get_Parameter(levelOffsetGuid).Set(elev);
                                }

                                if (roundHolesLocationButtonName == "radioButton_RoundHolesLocationYes")
                                {
                                    XYZ originIntersection = (intersectionPoint.Location as LocationPoint)?.Point;
                                    if (originIntersection != null)
                                    {
                                        if (familyCode == "221" || familyCode == "223")
                                        {
                                            RoundHolesPositionInSlabs(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, true);
                                        }
                                        else if (familyCode == "222" || familyCode == "224")
                                        {
                                            RoundHolesPositionInSlabs(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, false);
                                        }
                                    }
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
                    }
                    else
                    {
                        // Если кода нет, используем старую проверку по FamilyName
                        if (intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Прямоугольное"
                            || intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Круглое")
                        {
                            if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                            {
                                double elev = intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8;
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(elev);
                            }
                            if (roundHolesLocationButtonName == "radioButton_RoundHolesLocationYes")
                            {
                                XYZ originIntersection = (intersectionPoint.Location as LocationPoint)?.Point;
                                if (originIntersection != null)
                                {
                                    if (intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Прямоугольное")
                                    {
                                        RoundHolesPositionInSlabs(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, true);
                                    }
                                    else if (intersectionPoint.Symbol.FamilyName == "Пересечение_Плита_Круглое")
                                    {
                                        RoundHolesPositionInSlabs(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, false);
                                    }
                                }
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
                                if (roundHolesLocationButtonName == "radioButton_RoundHolesLocationYes")
                                {
                                    XYZ originIntersection = (intersectionPoint.Location as LocationPoint)?.Point;
                                    if (originIntersection != null)
                                    {
                                        if (intersectionPoint.Symbol.FamilyName == "Отверстие_Плита_Прямоугольное")
                                        {
                                            RoundHolesPositionInSlabs(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, true);
                                        }
                                        else if (intersectionPoint.Symbol.FamilyName == "Отверстие_Плита_Круглое"
                                            || intersectionPoint.Symbol.FamilyName == "Гильза_Плита")
                                        {
                                            RoundHolesPositionInSlabs(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, false);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                                {
                                    intersectionPoint.get_Parameter(levelOffsetGuid).Set(0);
                                }
                                if (roundHolesLocationButtonName == "radioButton_RoundHolesLocationYes")
                                {
                                    XYZ originIntersection = (intersectionPoint.Location as LocationPoint)?.Point;
                                    if (originIntersection != null)
                                    {
                                        if (intersectionPoint.Symbol.FamilyName == "Пересечение_Стена_Прямоугольное"
                                            || intersectionPoint.Symbol.FamilyName == "Отверстие_Стена_Прямоугольное")
                                        {
                                            RoundHolesPositionInWalls(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, true);
                                        }
                                        else if (intersectionPoint.Symbol.FamilyName == "Пересечение_Стена_Круглое"
                                            || intersectionPoint.Symbol.FamilyName == "Отверстие_Стена_Круглое"
                                            || intersectionPoint.Symbol.FamilyName == "Гильза_Стена")
                                        {
                                            RoundHolesPositionInWalls(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, false);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Для остальных элементов применяем обработку по INSTANCE_ELEVATION_PARAM
                            if (intersectionPoint.get_Parameter(levelOffsetGuid) != null)
                            {
                                double elev = intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();
                                if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                                {
                                    elev = RoundToIncrement(elev, roundHolePositionIncrement);
                                    intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(elev);
                                }
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(elev);
                            }
                            if (roundHolesLocationButtonName == "radioButton_RoundHolesLocationYes")
                            {
                                XYZ originIntersection = (intersectionPoint.Location as LocationPoint)?.Point;
                                if (originIntersection != null)
                                {
                                    if (intersectionPoint.Symbol.FamilyName == "Пересечение_Стена_Прямоугольное"
                                        || intersectionPoint.Symbol.FamilyName == "Отверстие_Стена_Прямоугольное")
                                    {
                                        RoundHolesPositionInWalls(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, true);
                                    }
                                    else if (intersectionPoint.Symbol.FamilyName == "Пересечение_Стена_Круглое"
                                        || intersectionPoint.Symbol.FamilyName == "Отверстие_Стена_Круглое"
                                        || intersectionPoint.Symbol.FamilyName == "Гильза_Стена")
                                    {
                                        RoundHolesPositionInWalls(doc, grids, roundHoleLocationIncrement, originIntersection, intersectionPoint, false);
                                    }
                                }
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
        private void RoundHolesPositionInWalls(
            Document doc,
            List<Grid> grids,
            double roundHolePosition,
            XYZ originIntersectionCurve,
            FamilyInstance intersectionPoint,
            bool alignByEdges)
        {
            doc.Regenerate();

            // 1. Находим ближайшую ось, перпендикулярную HandOrientation
            Grid closestGrid = GetClosestPerpendicularGrid(grids, originIntersectionCurve, intersectionPoint.HandOrientation);
            if (closestGrid == null)
                return;

            Line gridLine = closestGrid.Curve as Line;
            if (gridLine == null)
                return;

            // 2. Определяем центр отверстия в плоскости XY и направление HandOrientation (в плоскости XY)
            XYZ originXY = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, 0);
            XYZ handOrientationXY = new XYZ(intersectionPoint.HandOrientation.X, intersectionPoint.HandOrientation.Y, 0).Normalize();

            // 3. Приводим линию оси к плоскости XY
            XYZ gridStart2D = new XYZ(gridLine.GetEndPoint(0).X, gridLine.GetEndPoint(0).Y, 0);
            XYZ gridEnd2D = new XYZ(gridLine.GetEndPoint(1).X, gridLine.GetEndPoint(1).Y, 0);
            XYZ d = (gridEnd2D - gridStart2D).Normalize();

            // Функция проекции точки P на бесконечную прямую
            Func<XYZ, XYZ> ProjectPoint2D = (XYZ P) =>
            {
                double t = (P - gridStart2D).DotProduct(d);
                return gridStart2D + d * t;
            };

            XYZ projXY;
            double currentDistance;

            if (alignByEdges)
            {
                // 4. Получаем ширину отверстия
                double width = intersectionPoint.get_Parameter(intersectionPointWidthGuid).AsDouble();

                // 5. Вычисляем координаты левой и правой грани
                XYZ leftEdgeXY = originXY - handOrientationXY * (width / 2);
                XYZ rightEdgeXY = originXY + handOrientationXY * (width / 2);

                // 6. Проецируем грани на ось
                XYZ leftProjXY = ProjectPoint2D(leftEdgeXY);
                XYZ rightProjXY = ProjectPoint2D(rightEdgeXY);

                // 7. Вычисляем расстояния
                double leftDistance = (leftProjXY - leftEdgeXY).GetLength();
                double rightDistance = (rightProjXY - rightEdgeXY).GetLength();

                // 8. Определяем, какую грань использовать
                bool useLeft = leftDistance < rightDistance;
                projXY = useLeft ? leftProjXY : rightProjXY;
                currentDistance = useLeft ? leftDistance : rightDistance;
            }
            else
            {
                // Проекция от центра отверстия (originXY)
                projXY = ProjectPoint2D(originXY);
                currentDistance = (projXY - originXY).GetLength();
            }

            // 9. Округляем текущее расстояние
            double targetDistance = RoundToIncrement(currentDistance, roundHolePosition);
            double delta = targetDistance - currentDistance;

            // 10. Определяем направление смещения
            XYZ moveDirectionXY = (projXY - originXY).Normalize();
            XYZ finalMoveXY = moveDirectionXY * delta;

            // 11. Перемещаем элемент
            ElementTransformUtils.MoveElement(doc, intersectionPoint.Id, -finalMoveXY);
        }
        private void RoundHolesPositionInSlabs(
            Document doc,
            List<Grid> grids,
            double roundHolePosition,
            XYZ originIntersectionCurve,
            FamilyInstance intersectionPoint,
            bool alignByEdges)
        {
            doc.Regenerate();

            XYZ originXY = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, 0);

            XYZ facingOrientationXY = new XYZ(
                intersectionPoint.FacingOrientation.X,
                intersectionPoint.FacingOrientation.Y,
                0).Normalize();

            XYZ handOrientationXY = new XYZ(
                intersectionPoint.HandOrientation.X,
                intersectionPoint.HandOrientation.Y,
                0).Normalize();

            // Ищем ближайшие оси
            Grid closestGridHand = GetClosestPerpendicularGrid(grids, originIntersectionCurve, intersectionPoint.HandOrientation);
            Grid closestGridFacing = GetClosestPerpendicularGrid(grids, originIntersectionCurve, intersectionPoint.FacingOrientation);

            Line gridLineHand = closestGridHand?.Curve as Line;
            Line gridLineFacing = closestGridFacing?.Curve as Line;

            if (gridLineHand == null && gridLineFacing == null)
                return; // Ни одной оси не найдено

            XYZ finalMoveXY = XYZ.Zero;

            // Функция проекции точки на ось
            XYZ ProjectPoint(XYZ P, XYZ lineStart, XYZ dir) =>
                lineStart + dir * (P - lineStart).DotProduct(dir);

            if (gridLineHand != null)
            {
                XYZ gridHandStart2D = new XYZ(gridLineHand.GetEndPoint(0).X, gridLineHand.GetEndPoint(0).Y, 0);
                XYZ gridHandEnd2D = new XYZ(gridLineHand.GetEndPoint(1).X, gridLineHand.GetEndPoint(1).Y, 0);
                XYZ dHand = (gridHandEnd2D - gridHandStart2D).Normalize();

                XYZ pointToProjectHand = originXY;

                if (alignByEdges)
                {
                    double width = intersectionPoint.get_Parameter(intersectionPointWidthGuid).AsDouble();
                    XYZ leftEdgeXY = originXY - handOrientationXY * (width / 2);
                    XYZ rightEdgeXY = originXY + handOrientationXY * (width / 2);

                    pointToProjectHand = (leftEdgeXY - gridHandStart2D).GetLength() < (rightEdgeXY - gridHandStart2D).GetLength()
                        ? leftEdgeXY : rightEdgeXY;
                }

                XYZ projHandXY = ProjectPoint(pointToProjectHand, gridHandStart2D, dHand);
                double currentHandDistance = (projHandXY - pointToProjectHand).GetLength();
                double targetHandDistance = RoundToIncrement(currentHandDistance, roundHolePosition);
                double deltaHand = targetHandDistance - currentHandDistance;
                XYZ moveDirectionHand = (projHandXY - pointToProjectHand).Normalize();

                finalMoveXY += moveDirectionHand * deltaHand;
            }

            if (gridLineFacing != null)
            {
                XYZ gridFacingStart2D = new XYZ(gridLineFacing.GetEndPoint(0).X, gridLineFacing.GetEndPoint(0).Y, 0);
                XYZ gridFacingEnd2D = new XYZ(gridLineFacing.GetEndPoint(1).X, gridLineFacing.GetEndPoint(1).Y, 0);
                XYZ dFacing = (gridFacingEnd2D - gridFacingStart2D).Normalize();

                XYZ pointToProjectFacing = originXY;

                if (alignByEdges)
                {
                    double height = intersectionPoint.get_Parameter(intersectionPointHeightGuid).AsDouble();
                    XYZ frontEdgeXY = originXY + facingOrientationXY * (height / 2);
                    XYZ backEdgeXY = originXY - facingOrientationXY * (height / 2);

                    pointToProjectFacing = (frontEdgeXY - gridFacingStart2D).GetLength() < (backEdgeXY - gridFacingStart2D).GetLength()
                        ? frontEdgeXY : backEdgeXY;
                }

                XYZ projFacingXY = ProjectPoint(pointToProjectFacing, gridFacingStart2D, dFacing);
                double currentFacingDistance = (projFacingXY - pointToProjectFacing).GetLength();
                double targetFacingDistance = RoundToIncrement(currentFacingDistance, roundHolePosition);
                double deltaFacing = targetFacingDistance - currentFacingDistance;
                XYZ moveDirectionFacing = (projFacingXY - pointToProjectFacing).Normalize();

                finalMoveXY += moveDirectionFacing * deltaFacing;
            }

            if (finalMoveXY.GetLength() > 0)
                ElementTransformUtils.MoveElement(doc, intersectionPoint.Id, -finalMoveXY);
        }
        private Grid GetClosestPerpendicularGrid(List<Grid> grids, XYZ point, XYZ orientation)
        {
            Grid closestGrid = null;
            double closestDistance = double.MaxValue;

            foreach (var grid in grids)
            {
                Line gridLine = grid.Curve as Line;
                if (gridLine == null) continue;

                // Проверяем перпендикулярность оси относительно указанного направления
                double dotProduct = Math.Abs(gridLine.Direction.Normalize().DotProduct(orientation.Normalize()));
                if (dotProduct > 1e-6) continue; // Если не перпендикулярна (скалярное произведение не близко к 0)

                // Вычисляем расстояние от точки до оси
                double distance = gridLine.Distance(point);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestGrid = grid;
                }
            }

            return closestGrid;
        }
        private static async Task GetPluginStartInfo()
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

            if (type != null)
            {
                // Создание экземпляра класса
                object instance = Activator.CreateInstance(type);

                // Получение метода CollectPluginUsageAsync
                var method = type.GetMethod("CollectPluginUsageAsync");

                if (method != null)
                {
                    // Вызов асинхронного метода через reflection
                    Task task = (Task)method.Invoke(instance, new object[] { assemblyName, assemblyNameRus });
                    await task;  // Ожидание завершения асинхронного метода
                }
            }
        }
    }
}
