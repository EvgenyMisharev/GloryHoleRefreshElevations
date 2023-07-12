using System.IO;
using System.Xml.Serialization;

namespace GloryHoleRefreshElevations
{
    public class GloryHoleRefreshElevationsSettings
    {
        public string RefreshElevationsButtonName { get; set; }
        public GloryHoleRefreshElevationsSettings GetSettings()
        {
            GloryHoleRefreshElevationsSettings gloryHoleRefreshElevationsSettings = null;
            string assemblyPathAll = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string fileName = "GloryHoleRefreshElevationsSettings.xml";
            string assemblyPath = assemblyPathAll.Replace("GloryHoleRefreshElevations.dll", fileName);

            if (File.Exists(assemblyPath))
            {
                using (FileStream fs = new FileStream(assemblyPath, FileMode.Open))
                {
                    XmlSerializer xSer = new XmlSerializer(typeof(GloryHoleRefreshElevationsSettings));
                    gloryHoleRefreshElevationsSettings = xSer.Deserialize(fs) as GloryHoleRefreshElevationsSettings;
                    fs.Close();
                }
            }
            else
            {
                gloryHoleRefreshElevationsSettings = null;
            }

            return gloryHoleRefreshElevationsSettings;
        }

        public void SaveSettings()
        {
            string assemblyPathAll = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string fileName = "GloryHoleRefreshElevationsSettings.xml";
            string assemblyPath = assemblyPathAll.Replace("GloryHoleRefreshElevations.dll", fileName);

            if (File.Exists(assemblyPath))
            {
                File.Delete(assemblyPath);
            }

            using (FileStream fs = new FileStream(assemblyPath, FileMode.Create))
            {
                XmlSerializer xSer = new XmlSerializer(typeof(GloryHoleRefreshElevationsSettings));
                xSer.Serialize(fs, this);
                fs.Close();
            }
        }
    }
}
