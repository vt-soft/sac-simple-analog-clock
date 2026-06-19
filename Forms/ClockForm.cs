// **************************************************************************************************************************
// *  Project information:                                                                                                  *
// *                                                                                                                        *
// *  Name        :  SAC - simple analog clock                                                                             *
// *  Description :  Analog clocks for your desktop. Useful for people working with colleagues/companies                    *
// *                 from different time zones.                                                                             *
// *  Language    :  C# 14                                                                                                  *
// *  Framework   :  .NET 10.0                                                                                              *
// *  UI          :  WinForms                                                                                               *
// *  NuGet       :  TimeZoneConverter by Matt Johnson-Pint (https://www.nuget.org/packages/TimeZoneConverter/)             *
// *  Icons       :  By Icons8 (https://icons8.com/)                                                                        *
// *  Web         :  https://www.vt-soft.com/sac-simple-analog-clock                                                     *
// *                                                                                                                        *
// *  Please be aware that I am not professional developer so this code is not perfect                                      *
// *  and it is probably not following all best programming practices.                                                      *
// *  However I still hope that you will find it useful and that it will help you to create your own application.           *
// *  For more projects please check https://www.vt-soft.com/                                                               *
// *  Any link to this site is highly appreciated. Enjoy the code! :)                                                       *
// *                                                                                                                        *
// *  Copyright(c) 2026, vt-soft                                                                                            *
// *  All rights reserved.                                                                                                  *
// *                                                                                                                        *
// *  This source code is licensed under the MIT-style license.                                                             *
// *  More info in the license.txt file in the root directory of this source tree.                                          *
// **************************************************************************************************************************

using sac.Helpers;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Net.Security;
using System.Security.Policy;
using System.Windows.Forms;

namespace sac;


// Entry point of the application. Main form with all the analog clocks.
// Here is the timer (clockTimer) which is ticking every 100 ms to update the time on the clocks. 

public partial class ClockForm : Form
{
    private int resCounter = 0;
    private bool dragging = false;
    private Point dragCursorPoint;
    private Point dragFormPoint;
    private Point diff;
    private ClockManager clockManager;
    private Settings settings;
    private bool suppressSaving = true;


    // Resizing state
    private bool isResizing = false; // True when resizing of the clockForm is in progress
    private Size dragFormSize;

    private Rectangle borderRect1; // Form border rectangles which are visible only during mouse hover over buttons.
    private Rectangle borderRect2;

    // Low-level mouse hook fields
    private IntPtr _hookID = IntPtr.Zero;
    private readonly NativeMethods.LowLevelMouseProc _proc; // keep delegate reference alive


    public ClockForm()
    {
        InitializeComponent();

        // Don't show app icon in taskbar
        this.ShowInTaskbar = false;
        // Keep tray icon visible at all times
        notifyIcon1.Visible = true;
        // Setup context menu for system tray icon
        SetupNotifyIconContextMenu();


        this.DoubleBuffered = true;
        this.Paint += ClockForm_Paint;
        suppressSaving = true;

        _proc = HookCallback;

        ClockFormInit();

        settings = new Settings();
        clockManager = new ClockManager(settings);
        clockTimer.Enabled = true;
        sFileManager.CheckFiles(settings);


        // Update Windows registry to add/remove SAC from startup based on the value of settings.RunAtStartup
        sRegistry.UpdateWindowsRegistry(settings.RunAtStartup);

        // Ensure Windows/WinForms won't override our saved location
        this.StartPosition = FormStartPosition.Manual;

        // Update form position based on saved settings
        this.Left = settings.ClockFormXPosition;
        this.Top = settings.ClockFormYPosition;
        this.Width = settings.ClockFormWidth;
        this.Height = settings.ClockFormHeight;

      
        borderRect1 = new Rectangle(1, 1, this.ClientSize.Width - 2, this.ClientSize.Height - 2);
        borderRect2 = new Rectangle(4, 4, this.ClientSize.Width - 8, this.ClientSize.Height - 8);

        clockManager.UpdatePositionsAllClocks(this.Width, this.Height);

        this.LocationChanged += OnFormLocationOrSizeChanged;
        this.SizeChanged += OnFormLocationOrSizeChanged;
    }


    /// <summary>
    /// This method is responsible for all the drawing on the form. 
    /// It is called whenever the form needs to be repainted (e.g., after Invalidate() or during resizing).
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ClockForm_Paint(object? sender, PaintEventArgs e)
    {
        // Ensure background is cleared to avoid trails during resize/hover
        e.Graphics.Clear(this.BackColor);

        // Draw clocks
        clockManager.DrawAllClocks(e.Graphics);

        // Draw crisp borders only when mouse is over any button (hit-test)
        if (isResizing || moveButton.Visible || settingsButton.Visible || hideButton.Visible || resizeButton.Visible)
        {
            e.Graphics.SmoothingMode = SmoothingMode.None;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.None;
            e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;

            // Form black & white borders 
            borderRect1.X = 1; borderRect1.Y = 1;
            borderRect1.Width = this.ClientSize.Width - 2;
            borderRect1.Height = this.ClientSize.Height - 2;

            borderRect2.X = 4; borderRect2.Y = 4;
            borderRect2.Width = this.ClientSize.Width - 8;
            borderRect2.Height = this.ClientSize.Height - 8;

            e.Graphics.DrawRectangle(Pens.Black, borderRect1);
            e.Graphics.DrawRectangle(Pens.WhiteSmoke, borderRect2);
        }
    }

    /// <summary>
    /// This method initializes the ClockForm by setting up transparency, styles for smooth resizing and painting,
    /// </summary>
    private void ClockFormInit()
    {
        // Make the whole ClockForm transparent except where controls are shown
        this.TransparencyKey = Color.Gray;  // Gray is working fine here.
        this.BackColor = Color.Gray;
        // this.BackColor = Color.Magenta;

        // Redraw while resizing and perform all painting in WM_PAINT to avoid artifacts
        this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        this.UpdateStyles();

        // Show the hidden buttons when the mouse is moved over their area.
        // Transparent parts of the form (TransparencyKey) do not receive mouse events,
        // so install a low-level mouse hook to get global mouse move events.
        _hookID = NativeMethods.SetHook(_proc);
        this.FormClosed += ClockForm_FormClosed;

        // moveButton drag handlers
        moveButton.MouseDown += MoveButton_MouseDown;
        moveButton.MouseUp   += MoveButton_MouseUp;
        moveButton.MouseMove += MoveButton_MouseMove;

        // resizeButton drag handlers
        resizeButton.MouseDown += ResizeButton_MouseDown;
        resizeButton.MouseUp   += ResizeButton_MouseUp;
        resizeButton.MouseMove += ResizeButton_MouseMove;

    }

    // **********************************************************************************************************
    // ** Low-level mouse hook methods **************************************************************************

    // Clean up the mouse hook when the form is closed
    private void ClockForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (_hookID != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }

    // Low-level mouse hook callback
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_MOUSEMOVE)
            {
                // Get current cursor position in screen coordinates
                Point screenPt = Cursor.Position;
                // Convert to client coordinates
                Point clientPt = this.PointToClient(screenPt);

                // Marshal UI update to the UI thread
                _ = this.BeginInvoke(() =>
                {
                    if ((!moveButton.Visible || !settingsButton.Visible || !hideButton.Visible || !resizeButton.Visible)
                        &&
                        (moveButton.Bounds.Contains(clientPt)
                         || settingsButton.Bounds.Contains(clientPt)
                         || hideButton.Bounds.Contains(clientPt)
                         || resizeButton.Bounds.Contains(clientPt)))
                    {
                        ShowButtons();
                    }
                    else if ((moveButton.Visible && settingsButton.Visible && hideButton.Visible && resizeButton.Visible)
                             &&
                             (!moveButton.Bounds.Contains(clientPt)
                              && !settingsButton.Bounds.Contains(clientPt)
                              && !hideButton.Bounds.Contains(clientPt)
                              && !resizeButton.Bounds.Contains(clientPt)))
                    {

                        HideButtons();

                        // resize button up
                        isResizing = false;
                        resizeButton.Capture = false;
                        resizeButton.Cursor = Cursors.Default;
                    }

                });
            }
        }
        catch (ObjectDisposedException)
        {
            // form is closing; ignore
        }

        return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
    }


    private void ShowButtons()
    {
        moveButton.Visible = true;
        settingsButton.Visible = true;
        hideButton.Visible = true;
        resizeButton.Visible = true;
        this.Invalidate();
        this.Refresh();
    }

    private void HideButtons()
    {
        moveButton.Visible = false;
        settingsButton.Visible = false;
        hideButton.Visible = false;
        resizeButton.Visible = false;

        // Trigger a clean repaint after UI state change
        // (There was a problem with rect1 and rect2 borders.
        // They didn't disappear if mouse was not on any button. And this solution fixed it.)
        this.Invalidate();
        this.Refresh();
    }


    // **********************************************************************************************************
    // ** Methods for moving the form by dragging the moveButton ************************************************
    private void MoveButton_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            dragging = true;
            dragCursorPoint = Cursor.Position; // Current position of the cursor
            dragFormPoint = this.Location;     // Current location of the form
            moveButton.Capture = true;
            moveButton.Cursor = Cursors.SizeAll;
        }
    }

    private void MoveButton_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!dragging) return;
        diff = new Point(Cursor.Position.X - dragCursorPoint.X, Cursor.Position.Y - dragCursorPoint.Y);
        this.Location = new Point(dragFormPoint.X + diff.X, dragFormPoint.Y + diff.Y);

        settings.ClockFormXPosition = this.Left; // Save form position to settings
        settings.ClockFormYPosition = this.Top;
    }

    private void MoveButton_MouseUp(object? sender, MouseEventArgs e)
    {
        dragging = false;
        moveButton.Capture = false;
        moveButton.Cursor = Cursors.Default;
    }

    // **********************************************************************************************************
    // ** Methods for resizing the form by dragging the resizeButton ********************************************

    private void ResizeButton_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            isResizing = true;
            dragCursorPoint = Cursor.Position; // reuse for start cursor
            dragFormSize = this.Size;          // start size
            resizeButton.Capture = true;
            resizeButton.Cursor = Cursors.SizeNWSE;
        }
    }

    private void ResizeButton_MouseMove(object? sender, MouseEventArgs e)
    {
        if (isResizing==false) return;

        // Calculate delta from drag start (screen coordinates)
        int deltaX = Cursor.Position.X - dragCursorPoint.X;
        int deltaY = Cursor.Position.Y - dragCursorPoint.Y;

        int newWidth = dragFormSize.Width + deltaX;
        int newHeight = dragFormSize.Height + deltaY;

        // Respect MinimumSize of the clockForm
        int minW = Math.Max(this.MinimumSize.Width, 160);
        int minH = Math.Max(this.MinimumSize.Height, 210);

        newWidth = Math.Max(minW, newWidth);
        newHeight = Math.Max(minH, newHeight);

        this.Size = new Size(newWidth, newHeight);

        settings.ClockFormWidth = this.Width;   // Save form size to settings
        settings.ClockFormHeight = this.Height;

        // Update clock positions based on new form size
        // clockManager.UpdatePositionsAllClocks(this.Width, this.Height);  - I am rather calling this method in clockTimer_Tick()

        this.Invalidate();
      
    }

    private void ResizeButton_MouseUp(object? sender, MouseEventArgs e)
    {
        isResizing = false;
        resizeButton.Capture = false;
        resizeButton.Cursor = Cursors.Default;

        // Force a final repaint
        clockManager.UpdatePositionsAllClocks(this.Width, this.Height); // just in case
        this.Invalidate();
    }


    // ***********************************************************************************************************
    // ** Saving config file methods *****************************************************************************


    /// <summary>
    /// Updates the stored form position and size settings when the form's location or size changes, 
    /// and schedules the settings to be saved.
    /// (Settings is saved to config file 7 seconds after the Form was resized/repositioned)
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data.</param>
    private void OnFormLocationOrSizeChanged(object? sender, EventArgs e)
    {
        if (suppressSaving) // Without this flag, settings would be saved during form initialization.
        {
            suppressSaving = false;
            return;
        }

        settings.ClockFormXPosition = this.Left;
        settings.ClockFormYPosition = this.Top;
        settings.ClockFormWidth = this.Width;
        settings.ClockFormHeight = this.Height;
        ScheduleSettingsSave();
    }

    /// <summary>
    /// Start / reset the timer.
    /// </summary>
    private void ScheduleSettingsSave()
    {
        // Reset the timer
        if (settingsSaveTimer.Enabled)
            settingsSaveTimer.Stop();

        settingsSaveTimer.Start();
    }

    /// <summary>
    /// Saved the settings to config file after timer tick.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void settingsSaveTimer_Tick(object sender, EventArgs e)
    {

        settingsSaveTimer?.Stop();
        sFileManager.SaveConfigFile();
        Debug.Print("Settings saved to config file - 7 seconds after the Form was resized/repositioned.");
    }

    private void ClockForm_FormClosed_1(object sender, FormClosedEventArgs e)
    {
        sFileManager.SaveConfigFile();
        Debug.Print("Settings saved to config file - Form Closed.");
    }


    // ***********************************************************************************************************
    // ** Other event handlers ************************************************************************************



    /// <summary>
    /// Main timer tick event handler. Callinng every 100 ms to update clocks time.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void clockTimer_Tick(object sender, EventArgs e)
    {

        if (clockManager.CalculateTimeAllClocks()==true)
        {
            this.Invalidate(); // this will trigger Paint event
        }


        // No need to update clock's possitions (during resizing) so often
        if (isResizing && resCounter>=3) 
        {
            clockManager.UpdatePositionsAllClocks(this.Width, this.Height);
            resCounter = 0;
            this.Invalidate();
        }
        resCounter++;
        
    }



    private void settingsButton_Click(object sender, EventArgs e)
    {
        SettingForm settingsForm = new SettingForm(settings, clockManager, this);
        settingsForm.ShowDialog();
    }

    private void HideButton_Click(object sender, EventArgs e)
    {
        this.Hide();
    }

    // *** System Tray Icon methods  *******************************************************************************************


    /// <summary>
    /// This method creates and sets up the context menu for the system tray icon
    /// </summary>
    private void SetupNotifyIconContextMenu()
    {
        var contextMenu = new ContextMenuStrip();

        var showClockFormItem = new ToolStripMenuItem("Show clocks");
        showClockFormItem.Click += ShowClockFormItem_Click;

        var hideClockFormItem = new ToolStripMenuItem("Hide clocks");
        hideClockFormItem.Click += HideClockFormItem_Click;

        var showSettingsItem = new ToolStripMenuItem("Settings");
        showSettingsItem.Click += ShowSettingsItem_Click;
        showSettingsItem.Image = Properties.Resources.cogwheel;

        var showHelpItem = new ToolStripMenuItem("Help");
        showHelpItem.Click += ShowHelpItem_Click;

        var exitMenuItem = new ToolStripMenuItem("Exit application");
        exitMenuItem.Click += ExitMenuItem_Click;

        contextMenu.Items.Add(showClockFormItem);
        contextMenu.Items.Add(hideClockFormItem);
        contextMenu.Items.Add(showSettingsItem);
        contextMenu.Items.Add(showHelpItem);
        contextMenu.Items.Add(exitMenuItem);

        // Update visibility based on current form state
        contextMenu.Opening += (s, e) => UpdateContextMenuVisibility(showClockFormItem, hideClockFormItem);

        notifyIcon1.ContextMenuStrip = contextMenu;
        notifyIcon1.Text = "SAC - Simple Analog Clock";

        // Show/hide ClockForm when clicking on the tray icon
        notifyIcon1.MouseClick += NotifyIcon1_MouseClick;
    }


   
    /// <summary>
    /// Updates the visibility of Show/Hide menu items based on ClockForm visibility
    /// </summary>
    private void UpdateContextMenuVisibility(ToolStripMenuItem showItem, ToolStripMenuItem hideItem)
    {
        showItem.Visible = !this.Visible;
        hideItem.Visible = this.Visible;
    }

    private void ShowClockFormItem_Click(object? sender, EventArgs e)
    {
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.Activate();
    }

    private void HideClockFormItem_Click(object? sender, EventArgs e)
    {
        this.Hide();
    }

    private void ShowSettingsItem_Click(object? sender, EventArgs e)
    {
        SettingForm settingsForm = new SettingForm(settings, clockManager, this);
        settingsForm.ShowDialog();
    }

    private void ShowHelpItem_Click(object? sender, EventArgs e)
    {
        string url = "https://www.vt-soft.com/sac-simple-analog-clock";
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        Application.Exit();
    }


    /// <summary>
    /// Handle left-click on the system tray icon to toggle form visibility
    /// </summary>
    private void NotifyIcon1_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (this.Visible)
            {
                this.Hide();
             
            }
            else
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            
            }
        }
    }

}
