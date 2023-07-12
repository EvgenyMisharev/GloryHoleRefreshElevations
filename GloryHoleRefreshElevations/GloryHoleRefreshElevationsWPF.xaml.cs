using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GloryHoleRefreshElevations
{
    public partial class GloryHoleRefreshElevationsWPF : Window
    {
        public string RefreshElevationsButtonName;
        GloryHoleRefreshElevationsSettings GloryHoleRefreshElevationsSettingsItem;
        public GloryHoleRefreshElevationsWPF()
        {
            GloryHoleRefreshElevationsSettingsItem = new GloryHoleRefreshElevationsSettings().GetSettings();
            InitializeComponent();

            if (GloryHoleRefreshElevationsSettingsItem != null)
            {
                if (GloryHoleRefreshElevationsSettingsItem.RefreshElevationsButtonName == "radioButton_Selected")
                {
                    radioButton_Selected.IsChecked = true;
                }
                else
                {
                    radioButton_All.IsChecked = true;
                }
            }
        }
        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            this.DialogResult = true;
            this.Close();
        }
        private void GloryHoleRefreshElevationsWPF_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                SaveSettings();
                this.DialogResult = true;
                this.Close();
            }

            else if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }
        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void SaveSettings()
        {
            GloryHoleRefreshElevationsSettingsItem = new GloryHoleRefreshElevationsSettings();
            RefreshElevationsButtonName = (this.groupBox_RefreshElevations.Content as Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;
            GloryHoleRefreshElevationsSettingsItem.RefreshElevationsButtonName = RefreshElevationsButtonName;
            GloryHoleRefreshElevationsSettingsItem.SaveSettings();
        }
    }
}
