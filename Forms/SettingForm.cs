using Microsoft.VisualBasic.Devices;
using sac.Helpers;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace sac
{
    internal partial class SettingForm : Form
    {
        enum EditMode
        {
            None,
            AddClock,
            EditClock
        }

        // This class is responsible for displaying and managing the SettingForm, which allows users to
        // change global settings and manage individual clocks. It interacts with the Settings class
        // to read and update global settings and with the ClockManager class to manage the list of clocks
        // and their properties.


        private Settings settings;

        private EditMode editMode;
        private ClockManager clockManager;
        private Clock selectedClock;
        private bool addingNewClock = false;
        private int selectedListBoxIndex;
        // Suppress global-settings change handlers while initializing the form
        private bool suppressGlobalSettingEvents = false;


        public SettingForm(Settings settings, ClockManager clockManager, ClockForm clockForm)
        {
            InitializeComponent();
            this.settings = settings;
            this.clockManager = clockManager;

            //// Enable double buffering for smoother rendering
            //this.DoubleBuffered = true;

            //// Prevent ListBox from flickering when losing focus - not working :(
            //SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            UpdateScreenPosition(clockForm);

            // GLOBAL SETTINGS tab
            UpdateFormData();

            // CLOCK SETTINGS tab
            DisableClockControls();
            PopulateCountryComboBox();

            tabControl1.Selecting += tabControl1_Selecting;
            this.FormClosing += SettingForm_FormClosing;

        }



        /// <summary>
        /// Set the position of SettingForm to the same screen as ClockForm
        /// </summary>.
        private void UpdateScreenPosition(ClockForm clockForm)
        {
            Screen clockFormScreen = Screen.FromControl(clockForm);
            Rectangle workingArea = clockFormScreen.WorkingArea;

            // Calculate vertical center position
            int centerY = workingArea.Top + (workingArea.Height - this.Height) / 2;

            // Calculate horizontal center position
            int leftX = workingArea.Left + (workingArea.Width - this.Width) / 2;

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(leftX, centerY);
        }

        // ************************************************************************************************************
        // ** Methods related to GLOBAL SETTINGS tab ******************************************************************
        // ************************************************************************************************************

        /// <summary>
        /// Initialize combo boxes / numeric up-downs based on global settings variables.
        /// </summary>
        private void UpdateFormData()
        {
            // Prevent SelectedIndexChanged handlers from reacting to these initial assignments
            suppressGlobalSettingEvents = true;

            clockSizeNumericUpDown.Value = settings.ClockSize;
            showFlagComboBox.SelectedIndex = settings.ShowFlag ? 0 : 1;
            showDayAndFullTimeComboBox.SelectedIndex = settings.ShowDayAndFullTime ? 0 : 1;
            showDaySecondHandComboBox.SelectedIndex = settings.ShowSecondHand ? 0 : 1;
            showWorkingHoursComboBox.SelectedIndex = settings.ShowWorkingHours ? 0 : 1;
            whStartHoursNumericUpDown.Value = settings.WorkingHoursStart.Hour;
            whStartMinutesNumericUpDown.Value = settings.WorkingHoursStart.Minute;

            whStopHoursNumericUpDown.Value = settings.WorkingHoursStop.Hour;
            whStopMinutesNumericUpDown.Value = settings.WorkingHoursStop.Minute;

            runAtStartupComboBox.SelectedIndex = settings.RunAtStartup ? 0 : 1;


            // Re-enable handlers
            suppressGlobalSettingEvents = false;
        }

        // ************************************************************************************************************
        // ** Event Handlers for Global settings tab *************************************************************


        private void clockSizeNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (suppressGlobalSettingEvents) return;
            settings.ClockSize = (int)clockSizeNumericUpDown.Value;
            clockManager.RecalculateItemAllClocks(GlobalSettingType.ClockSize);
        }

        private void showFlagComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressGlobalSettingEvents) return;
            settings.ShowFlag = showFlagComboBox.SelectedIndex == 0;
            clockManager.RecalculateItemAllClocks(GlobalSettingType.ShowFlag);
        }

        private void showDayAndFullTimeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressGlobalSettingEvents) return;
            settings.ShowDayAndFullTime = showDayAndFullTimeComboBox.SelectedIndex == 0;
            clockManager.RecalculateItemAllClocks(GlobalSettingType.ShowDayAndFullTime);
        }

        private void showDaySecondHandComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressGlobalSettingEvents) return;
            settings.ShowSecondHand = showDaySecondHandComboBox.SelectedIndex == 0;
            clockManager.RecalculateItemAllClocks(GlobalSettingType.ShowSecondHand);
        }

        private void showWorkingHoursComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressGlobalSettingEvents) return;
            settings.ShowWorkingHours = showWorkingHoursComboBox.SelectedIndex == 0;
            clockManager.RecalculateItemAllClocks(GlobalSettingType.ShowWorkingHours);

        }


        private void runAtStartupComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressGlobalSettingEvents) return;
            settings.RunAtStartup = runAtStartupComboBox.SelectedIndex == 0;
            // Update Windows registry to add/remove SAC from startup based on the new value of settings.RunAtStartup
            sRegistry.UpdateWindowsRegistry(settings.RunAtStartup);

        }



        // ***********************************************************************************************************
        // *** "VALIDATIONS for numericUpDown controls ** ************************************************************

        /// <summary>
        /// Verify that working hours start is at least 2 hours before working hours stop.
        /// </summary>
        /// <returns></returns>
        private bool GSNumericUpDownValidation()
        {
            bool error = false;

            if ((int)whStartHoursNumericUpDown.Value + 2 >= (int)whStopHoursNumericUpDown.Value)
                error = true;

            if (error)
            {
                errorGS1PictureBox.Visible = true;
                errorGS2PictureBox.Visible = true;
            }
            else
            {
                errorGS1PictureBox.Visible = false;
                errorGS2PictureBox.Visible = false;


                int hours = (int)whStartHoursNumericUpDown.Value;
                int minutes = (int)whStartMinutesNumericUpDown.Value;
                settings.WorkingHoursStart = new TimeOnly(hours, minutes);
                clockManager.RecalculateItemAllClocks(GlobalSettingType.WorkingHoursStart);

                hours = (int)whStopHoursNumericUpDown.Value;
                minutes = (int)whStopMinutesNumericUpDown.Value;
                settings.WorkingHoursStop = new TimeOnly(hours, minutes);
                clockManager.RecalculateItemAllClocks(GlobalSettingType.WorkingHoursStop);
            }

            return error;
        }

        private void whStartHoursNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (suppressGlobalSettingEvents) return;
            GSNumericUpDownValidation();
        }

        private void whStopHoursNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (suppressGlobalSettingEvents) return;
            GSNumericUpDownValidation();
        }


        // ************************************************************************************************************
        // ** Methods related to CLOCK SETTINGS tab  ******************************************************************
        // ************************************************************************************************************

        /// <summary>
        /// Disable all controls (except new, edit, delete, up, down)  in Clock settings tab
        /// </summary>
        private void DisableClockControls()
        {
            countryClockComboBox.Enabled = false;
            cityClockComboBox.Enabled = false;

            clockNameTextBox.Enabled = false;
            clockNameButton.Enabled = false;
            showFlagClockComboBox.Enabled = false;
            showDayAndFullTimeClockComboBox.Enabled = false;
            showSecondHandClockComboBox.Enabled = false;
            showWorkingHoursClockComboBox.Enabled = false;

            whStartHoursClockNumericUpDown.Enabled = false;
            whStartMinutesClockNumericUpDown.Enabled = false;
            whStopHoursClockNumericUpDown.Enabled = false;
            whStopMinutesClockNumericUpDown.Enabled = false;

            saveClockButton.Enabled = false;
            cancelClockButton.Enabled = false;
        }

        /// <summary>
        /// Enable all controls (except new, edit, delete, up, down) in Clock settings tab
        /// </summary>
        private void EnableClockControls()
        {
            countryClockComboBox.Enabled = true;
            cityClockComboBox.Enabled = true;

            clockNameTextBox.Enabled = true;
            clockNameButton.Enabled = true;
            showFlagClockComboBox.Enabled = true;
            showDayAndFullTimeClockComboBox.Enabled = true;
            showSecondHandClockComboBox.Enabled = true;
            showWorkingHoursClockComboBox.Enabled = true;

            whStartHoursClockNumericUpDown.Enabled = true;
            whStartMinutesClockNumericUpDown.Enabled = true;
            whStopHoursClockNumericUpDown.Enabled = true;
            whStopMinutesClockNumericUpDown.Enabled = true;

            saveClockButton.Enabled = true;
            cancelClockButton.Enabled = true;
        }


        // ************************************************************************************************************
        // ** Event Handlers (buttons) in Clock settings tab ********************************************************** 

        /// <summary>
        /// Delete selected clock from the clocks list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteClockButton_Click(object sender, EventArgs e)
        {
            if (clocksListBox.SelectedIndex == -1)
                return;

            if (clocksListBox.Items.Count == 1)
            {
                ShowCenteredMessageBox($"Warning: At least one clock must be present.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selectedClock = clockManager.Clocks[clocksListBox.SelectedIndex];

            DialogResult result = ShowCenteredMessageBox(
                $"Do you really want to delete \"{selectedClock.ClockName}\" clock?",
                "Delete Clock",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                int index = clocksListBox.SelectedIndex;

                clockManager.RemoveClock(selectedClock);
                UpdateClocksListbox();

                if (index == clocksListBox.Items.Count)
                    index--;
                clocksListBox.SelectedIndex = index;
            }
        }

        /// <summary>
        /// Move selected clock up in the clocks list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clockMoveUpButton_Click(object sender, EventArgs e)
        {
            if (clocksListBox.SelectedIndex == -1)
                return;

            // Prevent moving up if the selected clock is already at the top
            if (clocksListBox.SelectedIndex == 0)
                return;

            int selectedIndex = clocksListBox.SelectedIndex;

            selectedClock = clockManager.Clocks[selectedIndex];
            clockManager.MoveClockUp(selectedClock);
            UpdateClocksListbox();

            // Reselect the moved clock in the listbox
            clocksListBox.SelectedIndex = selectedIndex - 1;
        }

        /// <summary>
        /// Move selected clock down in the clocks list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clockMoveDownButton_Click(object sender, EventArgs e)
        {
            if (clocksListBox.SelectedIndex == -1)
                return;

            // Prevent moving down if the selected clock is already at the bottom
            if (clocksListBox.SelectedIndex == clocksListBox.Items.Count - 1)
                return;

            int selectedIndex = clocksListBox.SelectedIndex;

            selectedClock = clockManager.Clocks[selectedIndex];
            clockManager.MoveClockDown(selectedClock);
            UpdateClocksListbox();

            // Reselect the moved clock in the listbox
            clocksListBox.SelectedIndex = selectedIndex + 1;
        }


        // ************************************************************************************************************
        // *** Methods related to clocksListBox in Clock settings tab *************************************************

        private void SettingForm_Load(object sender, EventArgs e)
        {
            UpdateClocksListbox();
            clocksListBox.SelectedIndex = 0;

        }

        /// <summary>
        /// Update the clocksListBox with the current clocks from the list from ClockManager
        /// </summary>
        private void UpdateClocksListbox()
        {
            clocksListBox.Items.Clear();
            foreach (Clock clock in clockManager.Clocks)
            {
                clock.Recalculate(); // Without this line there will be incorect UTC offsets in listbox in the day when DST just started/ended
                clocksListBox.Items.Add(clock.UtcZone + "  " + clock.ClockName);
            }
        }

        /// <summary>
        /// Update Clock settings controls when a different clock is selected in the listbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clocksListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (clocksListBox.SelectedIndex == -1)
                return;

            UpdateClockSettingsLabelsAndComboboxes();
        }

        /// <summary>
        /// Update Clock settings controls based on the selected clock in the listbox
        /// </summary>
        private void UpdateClockSettingsLabelsAndComboboxes()
        {

            selectedClock = clockManager.Clocks[clocksListBox.SelectedIndex];
            if (selectedClock == null)
                return;

            // Set the selected country in countryClockComboBox based on the selected clock (in listBox)
            countryClockComboBox.SelectedIndex = countryClockComboBox.Items.IndexOf(selectedClock.CountryName);

            // Set the selected city in cityComboBox based on the selected clock
            cityClockComboBox.SelectedIndex = cityClockComboBox.Items.IndexOf(InsertUtcBeforeCity(selectedClock.CityName, selectedClock.TimeZoneIANA));

            clockNameTextBox.Text = selectedClock.ClockName;
            showFlagClockComboBox.SelectedIndex = selectedClock.ShowFlag ? 0 : 1;
            showDayAndFullTimeClockComboBox.SelectedIndex = selectedClock.ShowDayAndFullTime ? 0 : 1;
            showSecondHandClockComboBox.SelectedIndex = selectedClock.ShowSecondHand ? 0 : 1;
            showWorkingHoursClockComboBox.SelectedIndex = selectedClock.ShowWorkingHours ? 0 : 1;

            whStartHoursClockNumericUpDown.Value = selectedClock.WorkingHoursStart.Hour;
            whStartMinutesClockNumericUpDown.Value = selectedClock.WorkingHoursStart.Minute;
            whStopHoursClockNumericUpDown.Value = selectedClock.WorkingHoursStop.Hour;
            whStopMinutesClockNumericUpDown.Value = selectedClock.WorkingHoursStop.Minute;
        }

        /// <summary>
        /// Create string with UTC offset before city name for displaying in cityComboBox
        /// </summary>
        /// <param name="city"></param>
        /// <param name="timezone"></param>
        /// <returns></returns>
        private string InsertUtcBeforeCity(string city, string tzIANA)
        {
            string utcZone = "";
            string tzWin = tzIANA;
            try
            {
                // Accept either IANA or Windows IDs. Try IANA->Windows conversion first.
                if (TimeZoneConverter.TZConvert.TryIanaToWindows(tzIANA, out var mappedWindows))
                {
                    tzWin = mappedWindows;
                }

                // Now get TimeZoneInfo using the Windows id (or original if conversion didn't apply).
                TimeZoneInfo selectedTzWin = TimeZoneInfo.FindSystemTimeZoneById(tzWin);
                utcZone = RealUTC.Get(selectedTzWin);
            }
            catch
            {
                MessageBox.Show("E2 - Unknown time zone: " + tzWin + "\n\nApplication will be terminated", "Invalid time zone", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ExitApp.Exit();
            }

            return utcZone + " " + city;
        }

        /// <summary>
        /// Update Clock settings controls when a Global settings were just changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Check if "Clock settings" tab (tabPage2) is selected
            if (tabControl1.SelectedTab == tabPage2)
            {
                // Only update if there's a selected item in the listbox
                if (clocksListBox.SelectedIndex != -1)
                {
                    UpdateClockSettingsLabelsAndComboboxes();
                }
            }
        }


        // ************************************************************************************************************
        // *** Methods related to countryComboBox and cityCombobox in Clock settings tab ******************************

        /// <summary>
        /// Populate countryComboBox with the list of countries from .csv file
        /// </summary>
        private void PopulateCountryComboBox()
        {
            countryClockComboBox.Items.Clear();
            foreach (var country in sData.Countries)
            {
                countryClockComboBox.Items.Add(country.Name);
            }
        }

        /// <summary>
        /// Update cityClockComboBox items when a different country is selected in countryComboBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void countryClockComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {


            if (countryClockComboBox.SelectedIndex == -1)
                return;

            cityClockComboBox.Items.Clear();

            foreach (var country in sData.Countries)
            {
                if (country.Name == countryClockComboBox?.SelectedItem?.ToString())
                {
                    foreach (var city in country.Cities)
                    {
                        cityClockComboBox.Items.Add(InsertUtcBeforeCity(city.Name, city.IANATimeZone));
                    }
                    break;
                }
            }
            // If there are two or more cities then correct index will be set in UpdateClockSettingsLabelsAndComboboxes()
            cityClockComboBox.SelectedIndex = 0;
        }


        // ***********************************************************************************************************
        // *** "Add/Edit New Clock" methods"  *****************************************************************************

        private void editClockButton_Click(object sender, EventArgs e)
        {
            editMode = EditMode.EditClock;
            DisableButtonsAndListbox();
            selectedListBoxIndex = clocksListBox.SelectedIndex;
            EnableClockControls();
            clockNameTextBox.Text = "";
        }
        private void addNewClockButton_Click(object sender, EventArgs e)
        {
            editMode = EditMode.AddClock;
            DisableButtonsAndListbox();

            selectedListBoxIndex = clocksListBox.SelectedIndex;
            //clocksListBox.SelectedIndex = clocksListBox.Items.Count - 1;

            EnableClockControls();
            DeleteClockControlsValues();
        }

        private void DisableButtonsAndListbox()
        {
            addNewClockButton.Enabled = false;
            deleteClockButton.Enabled = false;
            editClockButton.Enabled = false;
            clockMoveDownButton.Enabled = false;
            clockMoveUpButton.Enabled = false;
            clocksListBox.Enabled = false;
        }

        /// <summary>
        /// Extract clock name from (UTC+City) string
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clockNameButton_Click(object sender, EventArgs e)
        {
            clockNameTextBox.Text = cityClockComboBox?.SelectedItem?.ToString()?.Substring(12);
        }

        private void DeleteClockControlsValues()
        {
            countryClockComboBox.SelectedIndex = -1;
            cityClockComboBox.Items.Clear();
            cityClockComboBox.Text = "";

            clockNameTextBox.Text = "";
            showDayAndFullTimeClockComboBox.SelectedIndex = 0;
            showSecondHandClockComboBox.SelectedIndex = 0;
            showWorkingHoursClockComboBox.SelectedIndex = 0;

            whStartHoursClockNumericUpDown.Value = 9;
            whStartMinutesClockNumericUpDown.Value = 0;
            whStopHoursClockNumericUpDown.Value = 17;
            whStopMinutesClockNumericUpDown.Value = 0;
        }

        /// <summary>
        /// Block switching tabs while there are unsaved changes in Clock settings tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // Block switching to tabPage1 and tabPage3 while editing on tabPage2
            bool editing = saveClockButton.Enabled;
            if (editing && (e.TabPage == tabPage1 || e.TabPage == tabPage3))
            {
                e.Cancel = true; // prevents the tab change
            }
        }

        /// <summary>
        /// Prevent closing SettingForm if there are unsaved changes in Clock settings tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (saveClockButton.Enabled)
            {
                ShowCenteredMessageBox("Please Save or Cancel your changes before closing the settings.", "Unsaved Changes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        private void cancelClockButton_Click(object sender, EventArgs e)
        {
            addNewClockButton.Enabled = true;
            deleteClockButton.Enabled = true;
            editClockButton.Enabled = true;
            clockMoveDownButton.Enabled = true;
            clockMoveUpButton.Enabled = true;

            clocksListBox.Enabled = true;
            clocksListBox.SelectedIndex = selectedListBoxIndex;
            UpdateClockSettingsLabelsAndComboboxes();

            errorCountryPictureBox.Visible = false;
            errorCityPictureBox.Visible = false;
            errorNamePictureBox.Visible = false;
            errorWHS1PictureBox.Visible = false;
            errorWHS2PictureBox.Visible = false;

            DisableClockControls();
        }


        // ***********************************************************************************************************
        // *** "Add/Edit New Clock" methods - SAVE button"  **********************************************************

        /// <summary>
        /// Create new clock based on user input in Clock settings tab and add it to the clocks list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveClockButton_Click(object sender, EventArgs e)
        {
            // Reset error icons.
            errorCountryPictureBox.Visible = false; errorCityPictureBox.Visible = false; errorNamePictureBox.Visible = false;

            // If something is wrong, show error icons and return.
            if (MissingTextValidation() || NumericUpDownValidation())
                return;


            if (editMode == EditMode.AddClock)  // Create new clock and add it to the clocks list.
            {
                clockManager.AddClock(CreateClock());
                cancelClockButton_Click(sender, e);
                UpdateClocksListbox();
                clocksListBox.SelectedIndex = clocksListBox.Items.Count - 1; // select the newly added clock
            }

            if (editMode == EditMode.EditClock)  // Update selected clock with new settings.
            {
                int index = clocksListBox.SelectedIndex;

                clockManager.EditClock(selectedClock, CreateClock());

                cancelClockButton_Click(sender, e);
                UpdateClocksListbox();

                clocksListBox.SelectedIndex = index; // Reselect the updated clock in the listbox
            }

        }

        /// <summary>
        /// Create new Clock object based on user input in Clock settings tab
        /// </summary>
        /// <returns></returns>
        private Clock CreateClock()
        {
            return new Clock(
                                clockNameTextBox.Text.Trim(),
                                countryClockComboBox.Text.Trim(),
                                cityClockComboBox.Text.Substring(12).Trim(),

                                getTzIANAfromCountryAndCityName(countryClockComboBox.Text.Trim(), cityClockComboBox.Text.Substring(12).Trim()),
                                GetCountryFlagCode(countryClockComboBox.Text.Trim()),
                                settings.ClockSize,

                                showFlagClockComboBox.SelectedIndex == 0,
                                showDayAndFullTimeClockComboBox.SelectedIndex == 0,
                                showSecondHandClockComboBox.SelectedIndex == 0,
                                showWorkingHoursClockComboBox.SelectedIndex == 0,

                                new TimeOnly((int)whStartHoursClockNumericUpDown.Value, (int)whStartMinutesClockNumericUpDown.Value),
                                new TimeOnly((int)whStopHoursClockNumericUpDown.Value, (int)whStopMinutesClockNumericUpDown.Value)
                            );
        }

        /// <summary>
        /// Get IANAtimezone (from sData.Countries) based on selected country and city in ComboBoxes
        /// </summary>
        /// <param name="cityName"></param>
        /// <returns></returns>
        private string getTzIANAfromCountryAndCityName(string countryName, string cityName)
        {
            foreach (var country in sData.Countries)
            {
                if (country.Name == countryName)
                {
                    foreach (var city in country.Cities)
                    {
                        if (city.Name == cityName)
                        {
                            return city.IANATimeZone; // exit as soon as the correct city is found
                        }
                    }
                }
            }

            // Program should never get her,so just in case.
            throw new Exception($"Error: Time zone IANA not found for country: {countryName} and city: {cityName}");
        }

        /// <summary>
        /// Get country flag code (from GlobalTZones.Countries) based on selected country in countryClockComboBox
        /// </summary>
        /// <returns></returns>
        private string GetCountryFlagCode(string countryName)
        {
            string flagCode = "";
            foreach (var country in sData.Countries)
            {
                if (country.Name == countryName)
                {
                    flagCode = country.Code;
                    break;
                }
            }
            return flagCode;
        }


        // ***********************************************************************************************************
        // *** "VALIDATIONS (for creating/editing clock)  ************************************************************

        /// <summary>
        /// Verify that working hours start is at least 2 hours before working hours stop.
        /// </summary>
        /// <returns></returns>
        private bool NumericUpDownValidation()
        {
            bool error = false;

            if ((int)whStartHoursClockNumericUpDown.Value + 2 >= (int)whStopHoursClockNumericUpDown.Value)
                error = true;

            if (error)
            {
                errorWHS1PictureBox.Visible = true;
                errorWHS2PictureBox.Visible = true;
            }
            else
            {
                errorWHS1PictureBox.Visible = false;
                errorWHS2PictureBox.Visible = false;
            }

            return error;
        }

        private void whStartHoursClockNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDownValidation();
        }

        private void whStopHoursClockNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDownValidation();
        }

        /// <summary>
        /// Verify that required text fields are not empty and valid.
        /// </summary>
        /// <returns></returns>
        private bool MissingTextValidation()
        {
            bool error = false;


            // verify if correct country is selected
            bool countryFound = false;
            foreach (var country in sData.Countries)
            {
                if (country.Name == countryClockComboBox.Text.Trim())
                {
                    countryFound = true;
                    break;
                }
            }
            if (!countryFound)
            {
                errorCountryPictureBox.Visible = true;
                error = true;
            }


            // verify if correct city is selected
            bool cityFound = false;
            if (cityClockComboBox.Text.Trim() == "" || cityClockComboBox.Items.Count == 0)
            {
                cityFound = false;
            }
            else
            {
                foreach (String city in cityClockComboBox.Items)
                {
                    if (city == cityClockComboBox.Text)
                    {
                        cityFound = true;
                    }
                }
            }
            if (!cityFound)
            {
                errorCityPictureBox.Visible = true;
                error = true;
            }


            // verify if some city name is selected
            if (clockNameTextBox.Text.Trim() == "")
            {
                errorNamePictureBox.Visible = true;
                error = true;
            }

            return error;
        }

        private void countryClockComboBox_TextChanged(object sender, EventArgs e)
        {
            errorCountryPictureBox.Visible = false;
        }

        private void cityClockComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            errorCityPictureBox.Visible = false;
        }

        private void clockNameTextBox_TextChanged(object sender, EventArgs e)
        {
            errorNamePictureBox.Visible = false;
        }


        // ************************************************************************************************************
        // *** Others *************************************************************************************************

        // Helper: show MessageBox centered over this SettingForm using a CBT hook
        private DialogResult ShowCenteredMessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using (new sac.Helpers.MessageBoxCenterer(this))
            {
                return MessageBox.Show(this, text, caption, buttons, icon);
            }
        }

        private void SettingForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            sFileManager.SaveConfigFile();
        }



        private void linkLabelWeb_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Open official SAC page (fixed URL)
            const string webUrl = "https://www.vt-soft.com/sac-simple-analog-clock";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = webUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabelIcons_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Open Icons8 homepage
            const string iconsUrl = "https://icons8.com";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = iconsUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabelSbr_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Open SBR homepage
            const string iconsUrl = "https://www.vt-soft.com/sbr-simple-break-reminder";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = iconsUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pictureBoxSbr_Click(object sender, EventArgs e)
        {
            // Open SBR homepage
            const string iconsUrl = "https://www.vt-soft.com/sbr-simple-break-reminder";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = iconsUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabelLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Open SAC license page
            const string iconsUrl = "https://www.vt-soft.com/sac-simple-analog-clock-license";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = iconsUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}
