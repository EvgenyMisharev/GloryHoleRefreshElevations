using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace GloryHoleRefreshElevations
{
    class GloryHoleSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Autodesk.Revit.DB.Element elem)
        {
            if (elem is FamilyInstance
                && IsCategory(elem, BuiltInCategory.OST_GenericModel)
                && ((elem as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Стена_Прямоугольное")
                    || (elem as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Стена_Круглое")
                    || (elem as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Плита_Прямоугольное")
                    || (elem as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Плита_Круглое")
                    || (elem as FamilyInstance).Symbol.Family.Name.Equals("Отверстие_Стена_Прямоугольное")
                    || (elem as FamilyInstance).Symbol.Family.Name.Equals("Отверстие_Стена_Круглое")
                    || (elem as FamilyInstance).Symbol.Family.Name.Equals("Отверстие_Плита_Прямоугольное")
                    || (elem as FamilyInstance).Symbol.Family.Name.Equals("Отверстие_Плита_Круглое")
                    || (elem as FamilyInstance).Symbol.Family.Name.Equals("Гильза_Стена")
                    || (elem as FamilyInstance).Symbol.Family.Name.Equals("Гильза_Плита")))
            {
                return true;
            }
            if (elem is FamilyInstance 
                && IsCategory(elem, BuiltInCategory.OST_Windows)
                && ((elem as FamilyInstance).Symbol.Family.Name.Equals("231_Отверстие прямоугольное (Окно_Стена)")
                || (elem as FamilyInstance).Symbol.Family.Name.Equals("231_Отверстие круглое с гильзой в стене (Окно_Стена)")))
            {
                return true;
            }
            return false;
        }

        private static bool IsCategory(Element element, BuiltInCategory builtInCategory)
        {
            var categoryId = element?.Category?.Id;
            if (categoryId == null)
                return false;

#if R2019 || R2020 || R2021 || R2022 || R2023 || R2024
            return categoryId.IntegerValue == (int)builtInCategory;
#else
            return categoryId.Value == new ElementId(builtInCategory).Value;
#endif
        }

        public bool AllowReference(Autodesk.Revit.DB.Reference reference, Autodesk.Revit.DB.XYZ position)
        {
            return false;
        }
    }
}
