using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GloryHoleRefreshElevations
{
    public partial class GloryHoleRefreshElevationsWPF : Window
    {
        public string RefreshElevationsOptionButtonName;
        GloryHoleRefreshElevationsSettings GloryHoleRefreshElevationsSettingsItem = null;
        public GloryHoleRefreshElevationsWPF()
        {
            GloryHoleRefreshElevationsSettingsItem = GloryHoleRefreshElevationsSettings.GetSettings();
            InitializeComponent();

            if (GloryHoleRefreshElevationsSettingsItem != null)
            {
                if (GloryHoleRefreshElevationsSettingsItem.RefreshElevationsOptionButtonName == "rbt_AllProject")
                {
                    rbt_AllProject.IsChecked = true;
                }
                else
                {
                    rbt_SelectedItems.IsChecked = true;
                }
            }
        }
        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            DialogResult = true;
            Close();
        }
        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        private void GloryHoleRefreshElevationsWPF_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                SaveSettings();
                DialogResult = true;
                Close();
            }

            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
        private void SaveSettings()
        {
            GloryHoleRefreshElevationsSettingsItem = new GloryHoleRefreshElevationsSettings();

            RefreshElevationsOptionButtonName = (groupBox_RefreshElevationsOption.Content as Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;

            GloryHoleRefreshElevationsSettingsItem.RefreshElevationsOptionButtonName = RefreshElevationsOptionButtonName;
            GloryHoleRefreshElevationsSettingsItem.SaveSettings();
        }
    }
}
