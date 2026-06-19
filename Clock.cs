using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using sac.Helpers;
using TimeZoneConverter;

namespace sac;

// This class is drawing (one particular) analog clock on the ClockForm screen.
// There are 3 main methods for several parts (hands, working hours, date ... ) of the clock:
// 1) Init  - called only once in constructor to initialize pens, brushes, fonts, etc. 
// 2) Recalculate - called in constructor and when radius or clock position has changed.
// 3) Draw - called each second to draw the clock.

public class Clock : IDisposable
{
    //*********************************** properties ***************************************************

    public PointF Center { get; set; } = new PointF(0, 0); // real X,Y clock center positions are set in ClockManager in UpdatePositionsAllClocks()
    public bool ClockIsVisible { get; set; } = true; // if clockForm is not big enough then ClockManger will set it to False for some clocks.
    public string UtcZone { get; set; }

    // constructor parameters
    public string ClockName { get; set; }    // your own name for the clock if you want (However you will probably use same name as in CityName)
    public string CountryName { get; set; }  // like "United States"
    public string CityName { get; set; }     // like "Los Angeles"
    public string TimeZoneIANA { get; set; } // IANA timezone, like "America/Los_Angeles"
    public string FlagCode { get; set; }

    // rest of constructor parameters which are also in Global Settings
    public int ClockSize { get; set; }
    public bool ShowFlag { get; set; }
    public bool ShowDayAndFullTime { get; set; }
    public bool ShowSecondHand { get; set; }
    public bool ShowWorkingHours { get; set; }
    public TimeOnly WorkingHoursStart { get; set; }
    public TimeOnly WorkingHoursStop { get; set; }


    // ******************************** private data  fields *******************************************

    private float radius;
    private bool isCorrectTimezone = true; // flag to indicate if timezone is valid

    // for WorkingHours methods
    private float whOuterR;
    private float whInnerR;
    private RectangleF whOuterRect;
    private RectangleF whInnerRect;
    private Color workingHoursColor;
    private SolidBrush? whHoursBrush;
    private TimeOnly now;
    private TimeOnly whStartMinus1;
    private TimeOnly whStopMinus1;
    private TimeOnly whStopPlus1;

    // for Flag methods
    private Image? flagImg;
    private RectangleF flagImgRect;
    private Pen? flagBlackPen;

    // for DialAndDate methods
    private float ddFontSize;
    private Font? ddFont;
    private Pen? ddBlackPen;
    private PointF ddTip;
    private PointF ddBaseCenter;
    private PointF ddP1;
    private PointF ddP2;
    private PointF[]? ddTriangle;
    private RectangleF ddDayRect;


    // for ClockHands methods
    private TimeZoneInfo? selectedWinTz;
    private DateTime utcNow, customTime;
    private double secondAngle, minuteAngle, hourAngle;
    private int hour, minute, second, previousSecond = -1;
    private float chCenterCap;
    private float chHourHandLength;
    private float chMinuteHandLength;
    private float chSecondHandLength;
    private Pen? chHandPen, chInnerHandPen, chSecondPen;
    private SolidBrush? chHandBrush, chSecondBrush;
    private PointF chSecondTailP1;
    private PointF chSecondTailP2;
    private PointF chSecondCenterP1;
    private PointF chSecondCenterP2;
    private PointF[]? chSecondTailPoly;
    private PointF chSecondTip;
    private PointF chSecondTail;

    // for ClockName methods
    private float cnFontSize;
    private Font? cnFont;
    private SolidBrush? cnFillBrush;
    private SolidBrush? cnShadowBrush;
    private StringFormat? cnStringFormat;


    public Clock(string clockName, string countryName, string cityName, string timeZoneIANA, string flagCode,
                  int clockSize, bool showFlag, bool showDayAndFullTime, bool showSecondHand, bool showWorkingHours,
                  TimeOnly workingHoursStart, TimeOnly workingHoursStop)
    {
        this.ClockName = clockName;
        this.CountryName = countryName;
        this.CityName = cityName;

        this.TimeZoneIANA = timeZoneIANA;
        this.FlagCode = flagCode;

        this.ClockSize = clockSize;
        this.ShowFlag = showFlag;
        this.ShowDayAndFullTime = showDayAndFullTime;
        this.ShowSecondHand = showSecondHand;
        this.ShowWorkingHours = showWorkingHours;
        this.WorkingHoursStart = workingHoursStart;
        this.WorkingHoursStop = workingHoursStop;

        Init();
        Recalculate();
    }



    // *** public methods ******************************************************************************

    /// <summary>
    /// Method to calculate the current time and to update angles of the clock hands. 
    /// In case the second has not changed since last call, it returns false.
    /// </summary>
    /// <returns></returns>
    public bool CalculateTime()
    {
        // If timezone is not correct, clock cannot work, so there is no need to continue. Such clock is set to 00:00.
        if (!isCorrectTimezone)
        {
            second = 0;
            minute = 0;
            hour = 0;
            return false;
        }

        customTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, selectedWinTz!);

        second = customTime.Second;    // just to save computer resources a bit
        if (second == previousSecond)
            return false;  // false = no need to redraw the clock, because second has not changed since last call

        minute = customTime.Minute;
        hour = customTime.Hour;

        secondAngle = second * 6.0;
        minuteAngle = (minute + second / 60.0) * 6.0;    // each minute = 6 degrees
        hourAngle = (hour % 12 + minute / 60.0) * 30.0;  // each hour = 30 degrees

        previousSecond = second;

        return true;
    }


    /// <summary>
    /// Method to recalculate pens, brushes and others if Radius, Clock position, Timezone, etc. has changed.  
    /// </summary>    
    public void Recalculate()
    {
        // For ClockSize=1 radius=60, for ClockSize=2 radius=70, for ClockSize=3 radius=80,
        radius = (ClockSize - 1) * 10 + 60.0f;

        SelectTimezone(TimeZoneIANA);

        RecalculateWorkingHours();
        RecalculateFlag();
        RecalculateDialAndDate();
        RecalculateClockHands();
        RecalculateClockName();
    }


    /// <summary>
    /// Method which calls all necessary methods to draw the complete clock
    /// </summary>
    /// <param name="g"></param>
    public void DrawClock(Graphics g)
    {
        if (!ClockIsVisible) return;
        if (g == null) return;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        DrawWorkingHours(g);
        DrawFlag(g);
        DrawDialAndDate(g);
        DrawClockHands(g);
        DrawClockName(g);
    }


    // *** private methods ******************************************************************************

    // Try to select valid time zone
    private void SelectTimezone(string tzIANA)
    {
        string tzWin = tzIANA;
        try
        {

            // Try to map IANA to Windows timezone.
            if (TZConvert.TryIanaToWindows(tzIANA, out var mappedWindows))
            {
                tzWin= mappedWindows;
            }

            // Get the TimeZoneInfo object for the selected Windows timezone ID. 
            selectedWinTz = TimeZoneInfo.FindSystemTimeZoneById(tzWin);
            UtcZone = RealUTC.Get(selectedWinTz);
        }
        catch
        {
            MessageBox.Show("E1 - Unknown time zone: " + tzWin + "\n\nApplication will be terminated", "Invalid time zone", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ExitApp.Exit();
        }
    }



    // Calling Init method (only once) to initialize pens, brushes, fonts, etc.
    private void Init()
    {
        InitWorkingHours();
        InitFlag();
        InitDialAndDate();
        InitClockHands();
        InitClockName();
    }


    public void Dispose()
    {
        chHandPen?.Dispose();
        chSecondPen?.Dispose();
        chHandBrush?.Dispose();
        chSecondBrush?.Dispose();
        chInnerHandPen?.Dispose();
        whHoursBrush?.Dispose();
        ddFont?.Dispose();
        cnFont?.Dispose();
        cnFillBrush?.Dispose();
        cnShadowBrush?.Dispose();
    }

    // ****************************************************************************************************
    // **  "WorkingHours" methods :  **********************************************************************
    private void InitWorkingHours()
    {
        whHoursBrush = new SolidBrush(Color.Silver);
        whOuterRect = new RectangleF(Center.X - whOuterR, Center.Y - whOuterR, whOuterR * 2f, whOuterR * 2f);
        whInnerRect = new RectangleF(Center.X - whInnerR, Center.Y - whInnerR, whInnerR * 2f, whInnerR * 2f);
    }
    private void RecalculateWorkingHours()
    {
        whOuterR = radius * 1.070f;
        whInnerR = radius * 0.980f;

        whOuterRect.X = Center.X - whOuterR;
        whOuterRect.Y = Center.Y - whOuterR;
        whOuterRect.Width = whOuterR * 2f;
        whOuterRect.Height = whOuterR * 2f;

        whInnerRect.X = Center.X - whInnerR;
        whInnerRect.Y = Center.Y - whInnerR;
        whInnerRect.Width = whInnerR * 2f;
        whInnerRect.Height = whInnerR * 2f;

        // compute time boundaries
        whStartMinus1 = WorkingHoursStart.AddHours(-1); 
        whStopMinus1 = WorkingHoursStop.AddHours(-1);
        whStopPlus1 = WorkingHoursStop.AddHours(1);
    }

    /// <summary>
    /// Method will fill in the ring between the outer and inner clock borders with a single color
    /// representing working-hours status based on current time and WorkingHoursStart/Stop.
    /// </summary>
    private void DrawWorkingHours(Graphics g)
    {
        now = TimeOnly.FromDateTime(customTime);

        // Inline time-range method
        // This method handles ranges that cross midnight correctly.
        bool InRange(TimeOnly t, TimeOnly start, TimeOnly end) => start <= end ? t >= start && t < end : t >= start || t < end;

        if (ShowWorkingHours)
        {
            if (InRange(now, whStartMinus1, WorkingHoursStart))
                workingHoursColor = Color.LightSkyBlue;
            else if (InRange(now, WorkingHoursStart, whStopMinus1))
                workingHoursColor = ColorTranslator.FromHtml("#009f2f");
            else if (InRange(now, whStopMinus1, WorkingHoursStop))
                workingHoursColor = Color.Orange;
            else if (InRange(now, WorkingHoursStop, whStopPlus1))
                workingHoursColor = ColorTranslator.FromHtml("#dc3b5b");
            else
                workingHoursColor = Color.Silver; 

            whHoursBrush!.Color = workingHoursColor;

        }
        else
        {
            whHoursBrush!.Color = Color.Silver;
        }



        // Draw yellow, green,red or silver annulus
        g.FillEllipse(whHoursBrush, whOuterRect);

        // Fill in area for dial 
        g.FillEllipse(Brushes.Ivory, Center.X - whInnerR, Center.Y - whInnerR, whInnerR * 2f, whInnerR * 2f);

        // Draw crisp 1px black borders with pixel-aligned settings, isolated from AA via Save/Restore
        // There is problem to draw "nice" edge of the clock on transparent backround.
        // So this is attem to solve this problem somehow.
        var state = g.Save();
        try
        {
            using (var innerPen = new Pen(Color.Black, 1f) { Alignment = PenAlignment.Inset }) 
            {
                g.DrawEllipse(innerPen, Center.X - whInnerR, Center.Y - whInnerR, whInnerR * 2f, whInnerR * 2f);
            }

            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.None;

            using (var outerPen = new Pen(Color.Black, 1.5f) { Alignment = PenAlignment.Inset })
            {
                g.DrawEllipse(outerPen, Center.X - whOuterR, Center.Y - whOuterR, whOuterR * 2f, whOuterR * 2f);
            }


        }
        finally
        {
            g.Restore(state);
        }




    }


    // ****************************************************************************************************
    // **  "Flag" methods :  ******************************************************************************
    private void InitFlag()
    {
        flagImgRect = new RectangleF();
        flagBlackPen = new Pen(Color.Black, 1f);
    }
    private void RecalculateFlag()
    {
        if (string.IsNullOrWhiteSpace(FlagCode)) return;

        flagImg = null;
        flagImg = Properties.Flags.ResourceManager.GetObject(FlagCode) as Image;

        // If flag not found, use "empty" flag
        if (flagImg == null)
        {
            flagImg = Properties.Flags.ResourceManager.GetObject("emptyflag") as Image;
        }


        // Scale flag to fit within a reasonable area
        float maxW = radius * 0.9f;
        float maxH = radius * 0.35f;
        float imgW = flagImg!.Width;
        float imgH = flagImg!.Height;
        float scale = Math.Min(maxW / imgW, maxH / imgH);
        if (scale <= 0f) scale = 1f;
        float drawW = imgW * scale;
        float drawH = imgH * scale;

        // Place the flag a few pixels below the center
        float offsetY = radius * 0.11f; // adjust as needed

        flagImgRect.X = Center.X - drawW / 2f;
        flagImgRect.Y = Center.Y + offsetY;
        flagImgRect.Width = drawW;
        flagImgRect.Height = drawH;

    }

    /// <summary>
    /// This method draws the flag image for the specified country code at the center of the clock. 
    /// </summary>
    /// <param name="g"></param>
    private void DrawFlag(Graphics g)
    {
        if (!ShowFlag) return;
        if (flagImg == null) return;

        try
        {
            g.DrawImage(flagImg, flagImgRect);

            // Draw black rectangle border around the flag
            g.DrawRectangle(flagBlackPen, flagImgRect.X, flagImgRect.Y, flagImgRect.Width, flagImgRect.Height);

        }
        catch
        {
            // ignore drawing errors
        }
    }


    // ****************************************************************************************************
    // **  "DialAndDate" methods :  ***********************************************************************
    private void InitDialAndDate()
    {
        ddFontSize = Math.Max(8f, radius * 0.22f);
        ddFont = new Font(FontFamily.GenericSansSerif, ddFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        ddBlackPen = new Pen(Color.Black, Math.Max(1f, radius * 0.01f));

        ddTip = new PointF();
        ddBaseCenter = new PointF();
        ddP1 = new PointF();
        ddP2 = new PointF();
        ddTriangle = new[] { new PointF(), new PointF(), new PointF() };
        ddDayRect = new RectangleF();
    }
    private void RecalculateDialAndDate()
    {
        ddFontSize = Math.Max(8f, radius * 0.22f);

        if (ddFont != null)
        {
            ddFont.Dispose();
        }
        ddFont = new Font(FontFamily.GenericSansSerif, ddFontSize, FontStyle.Bold, GraphicsUnit.Pixel);

        ddBlackPen!.Width = Math.Max(1f, radius * 0.01f);
    }

    /// <summary>
    /// Mehtod to draw the clock dial with hour and minute marks.
    /// </summary>
    /// <param name="g"></param>
    private void DrawDialAndDate(Graphics g)
    {

        const int totalMarks = 60;
        float smallSize = radius * 0.03f;
        float largeSize = radius * 0.05f;
        float marksRadius = radius * 0.9f; // place marks slightly inside outer radius

        for (int i = 0; i < totalMarks; i++)
        {
            double angleDeg = i * 6.0 - 90.0;
            double angleRad = angleDeg * Math.PI / 180.0;

            float x = Center.X + (float)(Math.Cos(angleRad) * marksRadius);
            float y = Center.Y + (float)(Math.Sin(angleRad) * marksRadius);

            // For 3,6,9,12 draw a triangle pointing outward instead of a dot
            if (i % 15 == 0)
            {
                // draw a larger triangle pointing inward (toward center)
                float dx = (float)Math.Cos(angleRad);
                float dy = (float)Math.Sin(angleRad);
                // tip closer to center, base further out
                float tipRadius = marksRadius - largeSize * 0.9f;
                float baseRadius = marksRadius + largeSize * 0.9f;

                ddTip.X = Center.X + dx * tipRadius;
                ddTip.Y = Center.Y + dy * tipRadius;
                ddBaseCenter.X = Center.X + dx * baseRadius;
                ddBaseCenter.Y = Center.Y + dy * baseRadius;

                float px = -dy;
                float py = dx;
                float half = largeSize * 1.2f; // make triangle wider
                ddP1.X = ddBaseCenter.X + px * half;
                ddP1.Y = ddBaseCenter.Y + py * half;
                ddP2.X = ddBaseCenter.X - px * half;
                ddP2.Y = ddBaseCenter.Y - py * half;

                ddTriangle![0] = ddP1;
                ddTriangle![1] = ddTip;
                ddTriangle![2] = ddP2;

                g.FillPolygon(Brushes.Black, ddTriangle);
            }
            else
            {
                float size = (i % 5 == 0) ? largeSize : smallSize; // hour marks bigger
                g.FillEllipse(Brushes.Black, x - size / 2f, y - size / 2f, size, size);
            }
        }

        // draw numerals 1..12 outside the marks; replace "3" with date box
        for (int h = 1; h <= 12; h++)
        {
            double angleDeg = h * 30.0 - 90.0;
            double angleRad = angleDeg * Math.PI / 180.0;
            float tx = Center.X + (float)(Math.Cos(angleRad) * (marksRadius * 0.7 + largeSize * 1.5));
            float ty = Center.Y + (float)(Math.Sin(angleRad) * (marksRadius * 0.7 + largeSize * 1.5));
            if (h == 3)
            {
                DrawDate(g, tx, ty);
            }
            else
            {
                string s = h.ToString();
                SizeF sz = g.MeasureString(s, ddFont!);
                g.DrawString(s, ddFont!, Brushes.Black, tx - sz.Width / 2f, ty - sz.Height / 2f);
            }
        }

    }

    /// <summary>
    ///  Draw day of month at the three o'clock position
    /// <param name="g"></param>
    /// <param name="cx"></param>
    /// <param name="cy"></param>
    private void DrawDate(Graphics g, float cx, float cy)
    {
        // draw day of month inside a rectangle with border centered at (cx,cy)
        string day = customTime.Day.ToString();
        SizeF sz = g.MeasureString(day, ddFont!);
        // provide padding around text and ensure reasonable minimum for small radii
        // reduce padding slightly so there's less space between border and date
        float padding = ddFont!.Size * 0.02f; // = Math.Max(2f, dialFont.Size * 0.04f);
        float w = sz.Width + padding * 2f;
        float h = sz.Height + padding * 2f;

        // center rectangle on provided coordinates but nudge left a bit so it sits better at 3 o'clock
        float shiftLeft = radius * 0.06f;

        ddDayRect.X = cx - w / 2f - shiftLeft;
        ddDayRect.Y = cy - h / 2f;
        ddDayRect.Width = w;
        ddDayRect.Height = h;

        // fill in and draw border
        g.FillRectangle(Brushes.PeachPuff, ddDayRect);
        g.DrawRectangle(ddBlackPen!, ddDayRect.X, ddDayRect.Y, ddDayRect.Width, ddDayRect.Height);

        // draw day inside with padding from top-left
        g.DrawString(day, ddFont, Brushes.Black, ddDayRect.X + padding, ddDayRect.Y + padding);
    }


    // ****************************************************************************************************
    // **  "ClockHands" methods :  ************************************************************************

    private void InitClockHands()
    {
        chHandPen = new Pen(Color.Black, 0) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        chInnerHandPen = new Pen(Color.White, 0) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };

        chSecondPen = new Pen(Color.Red, 0) { StartCap = System.Drawing.Drawing2D.LineCap.Flat, EndCap = System.Drawing.Drawing2D.LineCap.Round };

        chHandBrush = new SolidBrush(Color.Black);
        chSecondBrush = new SolidBrush(Color.Red);

        chSecondTailP1 = new PointF(0, 0);
        chSecondTailP2 = new PointF(0, 0);
        chSecondCenterP1 = new PointF(0, 0);
        chSecondCenterP2 = new PointF(0, 0);
        chSecondTailPoly = new[] { chSecondCenterP1, chSecondTailP1, chSecondTailP2, chSecondCenterP2 };
        chSecondTip = new PointF(0, 0);
        chSecondTail = new PointF(0, 0);
    }

    private void RecalculateClockHands()
    {
        chHandPen!.Width = Math.Max(3f, radius * 0.08f);
        chInnerHandPen!.Width = Math.Max(1f, chHandPen.Width * 0.35f);

        chSecondPen!.Width = Math.Max(1f, radius * 0.02f);

        chCenterCap = Math.Max(4f, radius * 0.08f); // center cap: big dot where all hands meet

        chHourHandLength = radius * 0.52f;
        chMinuteHandLength = radius * 0.82f;
        chSecondHandLength = radius * 0.9f;
    }

    /// <summary>
    /// Method to draw the clock hands based on the current angles.
    /// </summary>
    /// <param name="g"></param>
    private void DrawClockHands(Graphics g)
    {
        DrawHourOrMinuteHand(g, hourAngle, chHourHandLength, chHandPen!);  // hour hand
        DrawHourOrMinuteHand(g, minuteAngle, chMinuteHandLength, chHandPen!);  // minute hand

        // center cap: big dot where all hands meet
        g.FillEllipse(chHandBrush!, Center.X - chCenterCap, Center.Y - chCenterCap, chCenterCap * 2f, chCenterCap * 2f);

        // second hand (thin with tip)
        DrawSecondHand(g, secondAngle, chSecondHandLength);
    }

    private void DrawHourOrMinuteHand(Graphics g, double angleDeg, float length, Pen pen)
    {
        double angleRad = (angleDeg - 90.0) * Math.PI / 180.0;
        float x = Center.X + (float)(Math.Cos(angleRad) * length);
        float y = Center.Y + (float)(Math.Sin(angleRad) * length);

        // draw outer (black) hand
        g.DrawLine(pen, Center.X, Center.Y, x, y);

        // Inner white pen:
        // start the white stripe farther from center so it begins well away from the center cap
        float innerStartOffset = Math.Max(pen.Width * 0.6f, radius * 0.25f);
        float sx = Center.X + (float)(Math.Cos(angleRad) * innerStartOffset);
        float sy = Center.Y + (float)(Math.Sin(angleRad) * innerStartOffset);
        // leave a small gap at the tip as well (not as big as the center gap)
        float innerEndOffset = Math.Max(pen.Width * 0.3f, radius * 0.05f);
        float ex = Center.X + (float)(Math.Cos(angleRad) * (length - innerEndOffset));
        float ey = Center.Y + (float)(Math.Sin(angleRad) * (length - innerEndOffset));

        g.DrawLine(chInnerHandPen!, sx, sy, ex, ey);
    }

    private void DrawSecondHand(Graphics g, double angleDeg, float length)
    {
        if (!ShowSecondHand) return;

        double angleRad = (angleDeg - 90.0) * Math.PI / 180.0;
        float dx = (float)Math.Cos(angleRad);
        float dy = (float)Math.Sin(angleRad);

        // tip and tail (a bit behind center)
        chSecondTip.X = Center.X + dx * length;
        chSecondTip.Y = Center.Y + dy * length;
        chSecondTail.X = Center.X - dx * (length * 0.30f);
        chSecondTail.Y = Center.Y - dy * (length * 0.30f);

        // draw main shaft
        g.DrawLine(chSecondPen!, chSecondTail, chSecondTip);

        // draw a wider filled tail shape behind the center for visual weight
        float tailHalfWidth = Math.Max(1f, radius * 0.03f);
        float px = -dy;
        float py = dx;

        chSecondTailP1.X = chSecondTail.X + px * tailHalfWidth;
        chSecondTailP1.Y = chSecondTail.Y + py * tailHalfWidth;

        chSecondTailP2.X = chSecondTail.X - px * tailHalfWidth;
        chSecondTailP2.Y = chSecondTail.Y - py * tailHalfWidth;

        chSecondCenterP1.X = Center.X + px * (tailHalfWidth * 0.5f);
        chSecondCenterP1.Y = Center.Y + py * (tailHalfWidth * 0.5f);
        chSecondCenterP2.X = Center.X - px * (tailHalfWidth * 0.5f);
        chSecondCenterP2.Y = Center.Y - py * (tailHalfWidth * 0.5f);

        // make the polygon not include the very end point to avoid tiny dot artifacts
        chSecondTailPoly![0] = chSecondCenterP1;
        chSecondTailPoly![1] = chSecondTailP1;
        chSecondTailPoly![2] = chSecondTailP2;
        chSecondTailPoly![3] = chSecondCenterP2;

        g.FillPolygon(chSecondBrush!, chSecondTailPoly!);

        // center cap: outer dark cap then small white point
        float cr = radius * 0.04f;
        g.FillEllipse(Brushes.Red, Center.X - cr, Center.Y - cr, cr * 2f, cr * 2f);
        float inner = cr * 0.35f;
        g.FillEllipse(Brushes.White, Center.X - inner, Center.Y - inner, inner * 2f, inner * 2f);
    }


    // ****************************************************************************************************
    // **  "ClockHands" methods :  ************************************************************************


    private void InitClockName()
    {
        cnFontSize = Math.Max(8f, radius * 0.22f * 1.4f);
        cnFont = new Font(FontFamily.GenericSansSerif, cnFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        cnFillBrush = new SolidBrush(Color.White);
        cnShadowBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0));
        cnStringFormat = StringFormat.GenericTypographic;
    }

    private void RecalculateClockName()
    {
        if (string.IsNullOrWhiteSpace(ClockName)) return;

        // base font size: 1.4x the dial font size
        float baseFontSize = Math.Max(8f, radius * 0.22f * 1.4f);
        float maxWidth = radius * 2f; // diameter of clock

        // measure text with current font and adjust if too wide
        float currentFontSize = baseFontSize;
        SizeF sz = new SizeF();

        // Use a temporary graphics to measure (we'll dispose it)
        using (var tempImg = new Bitmap(1, 1))
        using (var tempG = Graphics.FromImage(tempImg))
        {
            using (Font tempFont = new Font(FontFamily.GenericSansSerif, currentFontSize, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                sz = tempG.MeasureString(ClockName, tempFont);
            }

            // if text is too wide, reduce font size proportionally
            while (sz.Width > maxWidth && currentFontSize > 8f)
            {
                currentFontSize *= 0.9f; // reduce by 10%
                if (currentFontSize < 8f) currentFontSize = 8f;
                using (Font tempFont = new Font(FontFamily.GenericSansSerif, currentFontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    sz = tempG.MeasureString(ClockName, tempFont);
                }
            }
        }

        // update font if size changed
        if (Math.Abs(cnFontSize - currentFontSize) > 0.1f)
        {
            cnFontSize = currentFontSize;
            cnFont?.Dispose();
            cnFont = new Font(FontFamily.GenericSansSerif, cnFontSize, FontStyle.Bold, GraphicsUnit.Pixel);

            cnFillBrush?.Dispose();
            cnFillBrush = new SolidBrush(Color.White);

            cnShadowBrush?.Dispose();
            cnShadowBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0));
        }
    }


    /// <summary>
    /// Draw the clock name centered a few pixels above the clock.
    /// Text is white with a soft shadow so it remains readable on any background.
    /// If ShowDayAndFullTime is true, also display the time below the clock name.
    /// </summary>
    /// <param name="g"></param>
    private void DrawClockName(Graphics g)
    {
        if (g == null) return;
        if (string.IsNullOrWhiteSpace(ClockName)) return;

        // Measure ClockName and time strings to calculate vertical offset
        SizeF clockNameSize = g.MeasureString(ClockName, cnFont!);
        float timeHeight = 0f;
        string? timeText = null;

        // base font size before any scaling so time text does not shrink with ClockName
        float baseFontSize = Math.Max(8f, radius * 0.22f * 1.4f);
        float smallFontSize = Math.Max(7f, baseFontSize * 0.88f); // ~10% bigger than previous 0.7x

        if (ShowDayAndFullTime)
        {
            timeText = customTime.ToString("ddd, HH:mm");
            using (Font smallFont = new Font(FontFamily.GenericSansSerif, smallFontSize, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                timeHeight = g.MeasureString(timeText, smallFont).Height;
            }
        }

        // position: centered horizontally, a few pixels above the outer circle
        float totalHeight = clockNameSize.Height + timeHeight + (ShowDayAndFullTime ? 2f : 0f);
        float offset = Math.Max(4f, radius * 0.04f);
        float x = Center.X - clockNameSize.Width / 2f;
        float y = Center.Y - whOuterR - totalHeight - offset;

        float shadowOffset = Math.Max(1.5f, radius * 0.01f);

        // draw clock name with shadow then white fill
        DrawTextWithShadow(g, ClockName, cnFont!, x, y, shadowOffset);

        // if ShowDayAndFullTime, draw time below the clock name
        if (ShowDayAndFullTime && timeText != null)
        {
            float timeY = y + clockNameSize.Height + 2f;
            using (Font smallFont = new Font(FontFamily.GenericSansSerif, smallFontSize, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                DrawTextWithShadow(g, timeText, smallFont, Center.X, timeY, shadowOffset, centerHorizontally: true);
            }
        }
    }

    /// <summary>
    /// Helper to draw text with shadow (shadow down-right), white fill.
    /// </summary>
    private void DrawTextWithShadow(Graphics g, string text, Font font, float xOrCenter, float y, float shadowOffset, bool centerHorizontally = false)
    {
        float x = xOrCenter;
        SizeF sz = g.MeasureString(text, font);
        if (centerHorizontally)
        {
            x = Center.X - sz.Width / 2f;
        }

        using (GraphicsPath gp = new GraphicsPath())
        {
            gp.AddString(text, font.FontFamily, (int)font.Style, font.Size, new PointF(x, y), cnStringFormat);

            // shadow
            using (GraphicsPath shadow = (GraphicsPath)gp.Clone())
            {
                var m = new Matrix();
                m.Translate(shadowOffset, shadowOffset);
                shadow.Transform(m);
                g.FillPath(cnShadowBrush!, shadow);
            }

            // fill
            g.FillPath(cnFillBrush!, gp);
        }
    }






}
