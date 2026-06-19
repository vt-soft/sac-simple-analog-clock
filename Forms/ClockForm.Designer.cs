namespace sac;

partial class ClockForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClockForm));
        moveButton = new Button();
        settingsButton = new Button();
        hideButton = new Button();
        resizeButton = new Button();
        clockTimer = new System.Windows.Forms.Timer(components);
        settingsSaveTimer = new System.Windows.Forms.Timer(components);
        notifyIcon1 = new NotifyIcon(components);
        SuspendLayout();
        // 
        // moveButton
        // 
        moveButton.BackColor = Color.Linen;
        moveButton.FlatStyle = FlatStyle.Flat;
        moveButton.ForeColor = Color.Linen;
        moveButton.Image = Properties.Resources.move;
        moveButton.Location = new Point(9, 10);
        moveButton.Name = "moveButton";
        moveButton.Size = new Size(48, 52);
        moveButton.TabIndex = 0;
        moveButton.UseVisualStyleBackColor = false;
        moveButton.Visible = false;
        // 
        // settingsButton
        // 
        settingsButton.BackColor = Color.Linen;
        settingsButton.FlatStyle = FlatStyle.Flat;
        settingsButton.ForeColor = Color.Linen;
        settingsButton.Image = Properties.Resources.cogwheel;
        settingsButton.Location = new Point(57, 10);
        settingsButton.Name = "settingsButton";
        settingsButton.Size = new Size(48, 52);
        settingsButton.TabIndex = 2;
        settingsButton.UseVisualStyleBackColor = false;
        settingsButton.Visible = false;
        settingsButton.Click += settingsButton_Click;
        // 
        // hideButton
        // 
        hideButton.BackColor = Color.Linen;
        hideButton.FlatStyle = FlatStyle.Flat;
        hideButton.ForeColor = Color.Linen;
        hideButton.Image = Properties.Resources.hide;
        hideButton.Location = new Point(105, 10);
        hideButton.Name = "hideButton";
        hideButton.Size = new Size(48, 52);
        hideButton.TabIndex = 4;
        hideButton.UseVisualStyleBackColor = false;
        hideButton.Visible = false;
        hideButton.Click += HideButton_Click;
        // 
        // resizeButton
        // 
        resizeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        resizeButton.BackColor = Color.Linen;
        resizeButton.FlatStyle = FlatStyle.Flat;
        resizeButton.ForeColor = Color.Black;
        resizeButton.Image = Properties.Resources.resize;
        resizeButton.Location = new Point(303, 623);
        resizeButton.Name = "resizeButton";
        resizeButton.Size = new Size(64, 64);
        resizeButton.TabIndex = 5;
        resizeButton.UseVisualStyleBackColor = false;
        resizeButton.Visible = false;
        // 
        // clockTimer
        // 
        clockTimer.Tick += clockTimer_Tick;
        // 
        // settingsSaveTimer
        // 
        settingsSaveTimer.Interval = 7000;
        settingsSaveTimer.Tick += settingsSaveTimer_Tick;
        // 
        // notifyIcon1
        // 
        notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
        notifyIcon1.Text = "notifyIcon1";
        notifyIcon1.Visible = true;
        // 
        // ClockForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.LightGray;
        ClientSize = new Size(367, 687);
        Controls.Add(moveButton);
        Controls.Add(hideButton);
        Controls.Add(resizeButton);
        Controls.Add(settingsButton);
        DoubleBuffered = true;
        ForeColor = Color.Black;
        FormBorderStyle = FormBorderStyle.None;
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "ClockForm";
        Text = "SAC - Simple Analog Clock";
        TransparencyKey = Color.Magenta;
        FormClosed += ClockForm_FormClosed_1;
        ResumeLayout(false);
    }

    #endregion

    private Button moveButton;
    private Button settingsButton;
    private Button hideButton;
    private Button resizeButton;
    private System.Windows.Forms.Timer clockTimer;
    private System.Windows.Forms.Timer settingsSaveTimer;
    private NotifyIcon notifyIcon1;
}
