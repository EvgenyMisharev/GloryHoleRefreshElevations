using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GloryHoleRefreshElevations
{
    public partial class GloryHoleRefreshElevationsWPF : Window
    {
        public string RoundHolesPositionButtonName;
        public double RoundHolePositionIncrement;

        public string RoundHolesLocationButtonName;
        public double RoundHoleLocationIncrement;

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

                if (GloryHoleRefreshElevationsSettingsItem.RoundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                {
                    radioButton_RoundHolesPositionYes.IsChecked = true;
                }
                else
                {
                    radioButton_RoundHolesPositionNo.IsChecked = true;
                }

                if (GloryHoleRefreshElevationsSettingsItem.RoundHolePositionIncrementValue != null)
                {
                    textBox_RoundHolePositionIncrement.Text = GloryHoleRefreshElevationsSettingsItem.RoundHolePositionIncrementValue;
                }
                else
                {
                    textBox_RoundHolePositionIncrement.Text = "5";
                }


                if (GloryHoleRefreshElevationsSettingsItem.RoundHolesLocationButtonName == "radioButton_RoundHolesLocationYes")
                {
                    radioButton_RoundHolesLocationYes.IsChecked = true;
                }
                else
                {
                    radioButton_RoundHolesLocationNo.IsChecked = true;
                }

                if (!string.IsNullOrEmpty(GloryHoleRefreshElevationsSettingsItem.RoundHoleLocationIncrementValue))
                {
                    textBox_RoundHoleLocationIncrement.Text = GloryHoleRefreshElevationsSettingsItem.RoundHoleLocationIncrementValue;
                }
                else
                {
                    textBox_RoundHoleLocationIncrement.Text = "10";
                }

                checkBox_UpdaterOn.IsChecked = GloryHoleRefreshElevationsSettingsItem.UpdaterOn;
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
        private void radioButton_RoundHolesPosition_Checked(object sender, RoutedEventArgs e)
        {
            RoundHolesPositionButtonName = (this.groupBox_RoundHolesPosition.Content as Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;
            if (RoundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
            {
                if (label_RoundHolePosition != null)
                {
                    label_RoundHolePosition.IsEnabled = true;
                    textBox_RoundHolePositionIncrement.IsEnabled = true;
                    label_RoundHolePositionMM.IsEnabled = true;
                }
            }
            else if (RoundHolesPositionButtonName == "radioButton_RoundHolesPositionNo")
            {
                if (label_RoundHolePosition != null)
                {
                    label_RoundHolePosition.IsEnabled = false;
                    textBox_RoundHolePositionIncrement.IsEnabled = false;
                    label_RoundHolePositionMM.IsEnabled = false;
                }
            }
        }
        private void radioButton_RoundHolesLocation_Checked(object sender, RoutedEventArgs e)
        {
            RoundHolesLocationButtonName = (this.groupBox_RoundHolesLocation.Content as Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;

            bool isEnabled = RoundHolesLocationButtonName == "radioButton_RoundHolesLocationYes";

            if (label_RoundHoleLocation != null)
            {
                label_RoundHoleLocation.IsEnabled = isEnabled;
                textBox_RoundHoleLocationIncrement.IsEnabled = isEnabled;
                label_RoundHoleLocationMM.IsEnabled = isEnabled;
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

            GloryHoleRefreshElevationsSettingsItem.RoundHolesPositionButtonName = RoundHolesPositionButtonName;

            double.TryParse(textBox_RoundHolePositionIncrement.Text, out RoundHolePositionIncrement);
            GloryHoleRefreshElevationsSettingsItem.RoundHolePositionIncrementValue = textBox_RoundHolePositionIncrement.Text;


            RoundHolesLocationButtonName = (groupBox_RoundHolesLocation.Content as Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;

            GloryHoleRefreshElevationsSettingsItem.RoundHolesLocationButtonName = RoundHolesLocationButtonName;

            double.TryParse(textBox_RoundHoleLocationIncrement.Text, out RoundHoleLocationIncrement);
            GloryHoleRefreshElevationsSettingsItem.RoundHoleLocationIncrementValue = textBox_RoundHoleLocationIncrement.Text;


            GloryHoleRefreshElevationsSettingsItem.UpdaterOn = checkBox_UpdaterOn.IsChecked ?? false;

            GloryHoleRefreshElevationsSettingsItem.SaveSettings();
        }
    }
}
