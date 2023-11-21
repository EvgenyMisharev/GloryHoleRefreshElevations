using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;

namespace GloryHoleRefreshElevations
{
    public class HolesSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is FamilyInstance
                && elem.Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_GenericModel)
                && ((elem as FamilyInstance).Symbol.FamilyName == "Пересечение_Стена_Прямоугольное"
                || (elem as FamilyInstance).Symbol.Family.Name == "Пересечение_Стена_Круглое"
                || (elem as FamilyInstance).Symbol.Family.Name == "Пересечение_Плита_Прямоугольное"
                || (elem as FamilyInstance).Symbol.Family.Name == "Пересечение_Плита_Круглое"
                || (elem as FamilyInstance).Symbol.Family.Name == "Отверстие_Стена_Прямоугольное"
                || (elem as FamilyInstance).Symbol.Family.Name == "Отверстие_Стена_Круглое"
                || (elem as FamilyInstance).Symbol.Family.Name == "Отверстие_Плита_Прямоугольное"
                || (elem as FamilyInstance).Symbol.Family.Name == "Отверстие_Плита_Круглое"
                || (elem as FamilyInstance).Symbol.Family.Name == "Гильза_Стена"
                || (elem as FamilyInstance).Symbol.Family.Name == "Гильза_Плита"))

            {
                return true;
            }
            else if (elem is FamilyInstance
                && elem.Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_Windows)
                && ((elem as FamilyInstance).Symbol.FamilyName == "231_Отверстие прямоугольное (Окно_Стена)"
                || (elem as FamilyInstance).Symbol.Family.Name == "231_Отверстие круглое с гильзой в стене (Окно_Стена)"))
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new NotImplementedException();
        }
    }
}
