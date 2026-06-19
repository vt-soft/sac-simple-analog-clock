using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace sac;


/// <summary>
/// Enum representing different global settings that can be changed for all clocks.
/// </summary>
internal enum GlobalSettingType
{
    ClockSize,
    ShowFlag,
    ShowDayAndFullTime,
    ShowSecondHand,
    ShowWorkingHours,
    WorkingHoursStart,
    WorkingHoursStop,
}

// This class is responsible for managing the collection of clocks, including their positions, visibility,
// and updates based on global/local settings changes.
// It provides methods to add, remove, edit, and move clocks within the collection,
// as well as to calculate time and draw all clocks on the form.

internal class ClockManager
{
    private List<Clock> clocks;
    private Settings settings;
    public IReadOnlyList<Clock> Clocks => clocks; // Expose clocks as read-only

    private bool visibility = true;
	private int maxXClocks;            // max number of clocks in X direction
    private int maxYClocks;            // max number of clocks in Y direction
    private int xClockPositionInitial; // starting position of first clock in the grid
    private int xClockPosition;        // starting position of first clock in the grid
    private int yClockPosition;        // starting position of first clock in the grid
    private int deltaXClockPosition;   // distance between clocks in X direction
    private int deltaYClockPosition;   // distance between clocks in Y direction
	private int counterX, counterY;
    private int clockFormWidth, clockFormHeight;
    

	public ClockManager(Settings settings)
    {
        this.settings = settings;
        clocks = settings.Clocks; // link to the clocks stored in global settings
    }

    // Just some helper method to prepare the initial position and distance between clocks according to the clock size setting.
    private void ClockPositionPreparation()
    {
        if (settings.ClockSize == 1)
        {
            xClockPositionInitial = 080; 
            xClockPosition        = 080; yClockPosition        = 130;
            deltaXClockPosition   = 160; deltaYClockPosition   = 188;
        }
        if (settings.ClockSize == 2)
        {
            xClockPositionInitial = 110; 
            xClockPosition        = 110; yClockPosition        = 160;
            deltaXClockPosition   = 190; deltaYClockPosition   = 218;
        }
        if (settings.ClockSize == 3)
        {
            xClockPositionInitial = 140; 
            xClockPosition        = 140; yClockPosition        = 190;
            deltaXClockPosition   = 220; deltaYClockPosition   = 248;
        }
    }

    // ***************************************************************************************************************************
    // ***  PUBLIC METHODS  ******************************************************************************************************

    /// <summary>
    /// Update positions of all clocks according to the clockForm size.
    /// </summary>
    /// <param name="clockFormWidht"></param>
    /// <param name="clockFormHeight"></param>
    public void UpdatePositionsAllClocks(int clockFormWidht, int clockFormHeight)
	{
        // We have to store such data because we are calling this method also directly form this class
        // (when clock size is changed in settings) not only from ClockForm.cs when form size is changed.
        this.clockFormWidth = clockFormWidht; 
        this.clockFormHeight = clockFormHeight;
        
		counterX=1; counterY=1;
        visibility = true;
        ClockPositionPreparation();

        maxXClocks = clockFormWidht  / deltaXClockPosition; // maximum number of clocks in X direction
        maxYClocks = clockFormHeight / (deltaYClockPosition+5); // maximum number of clocks in Y direction   - already no idea why +5  :)

        foreach (Clock clock in clocks)
		{     
            if (counterX > maxXClocks) // going to next row
			{
                counterY++;
                if (counterY > maxYClocks)
                {
                    visibility = false;   // we are too far in Y-axis, so from now all remaining clocks will be invisible
                }

                xClockPosition = xClockPositionInitial;
                yClockPosition += deltaYClockPosition;
                clock.ClockIsVisible = visibility;
                clock.Center = new PointF(xClockPosition, yClockPosition);
                xClockPosition += deltaXClockPosition;  // prepare X-position for the next clock
                counterX=2;
                
            }
            else // adding clocks to the same row
            {
                clock.ClockIsVisible = visibility;            
                clock.Center = new PointF(xClockPosition, yClockPosition);
                xClockPosition += deltaXClockPosition;  // continue in the same row - prepare X-position for the next clock
                counterX++;
            }

            clock.Recalculate();
        }
    }


    /// <summary>
    /// Draw all clocks on clockForm
    /// </summary>
    /// <param name="g"></param>
    public void DrawAllClocks(Graphics g)
	{
		foreach (var clock in clocks)
		{
			clock.DrawClock(g);
		}
    }


    /// <summary>
    /// Calculate correct time for all clocks
    /// </summary>    
    public bool CalculateTimeAllClocks()
	{
        // if at least one clock needs to be updated, then we will return true and clockForm will be repainted
        bool updateNeeded = false; 

        foreach (var clock in clocks)
		{
			if (clock.CalculateTime()== true)
			{
				updateNeeded = true;
			}
		}
        return updateNeeded;
    }


    /// <summary>
    /// Recalculate particular (settings) item for all clocks after global settings change.
    /// </summary>
    /// <param name="settingType">The specific setting that was changed.</param>
    public void RecalculateItemAllClocks(GlobalSettingType settingType)
    {
        foreach (var clock in clocks)
        {
            if (settingType == GlobalSettingType.ClockSize)
            {
                clock.ClockSize = settings.ClockSize;
                UpdatePositionsAllClocks(clockFormWidth, clockFormHeight); 
            }
            else if (settingType == GlobalSettingType.ShowFlag)
            {
                clock.ShowFlag = settings.ShowFlag;
            }
            else if (settingType == GlobalSettingType.ShowDayAndFullTime)
            {
                clock.ShowDayAndFullTime = settings.ShowDayAndFullTime;
            }
            else if (settingType == GlobalSettingType.ShowSecondHand)
            {
                clock.ShowSecondHand = settings.ShowSecondHand;
            }
            else if (settingType == GlobalSettingType.ShowWorkingHours)
            {
                clock.ShowWorkingHours = settings.ShowWorkingHours;
            }
            else if (settingType == GlobalSettingType.WorkingHoursStart)
            {
                clock.WorkingHoursStart = settings.WorkingHoursStart;
            }
            else if (settingType == GlobalSettingType.WorkingHoursStop)
            {
                clock.WorkingHoursStop = settings.WorkingHoursStop;
            }

            clock.Recalculate();
        }
    }


    /// <summary>
    /// Adds the specified clock to the collection of managed clocks.
    /// </summary>
    /// <remarks>After adding the clock, the positions of all clocks are updated to reflect the new layout.</remarks>
    /// <param name="clock">The clock to add to the collection. Cannot be null.</param>
    public void AddClock(Clock clock)
	{
        clocks.Add(clock);
        UpdatePositionsAllClocks(clockFormWidth, clockFormHeight);
    }


    /// <summary>
    /// Deletes the specified clock from the collection of managed clocks.
    /// </summary>
    /// <param name="clock"></param>
	public void RemoveClock(Clock clock)
	{
        clocks.Remove(clock);
        UpdatePositionsAllClocks(clockFormWidth, clockFormHeight);
    }


    /// <summary>
    /// Edits the specified clock by replacing it with a new clock instance.
    /// </summary>
    /// <param name="selectedClock"></param>
    /// <param name="newClock"></param>
	public void EditClock(Clock selectedClock,Clock newClock)
	{
        int index = clocks.IndexOf(selectedClock);
        if (index != -1)
        {
            clocks[index] = newClock; // newClock will be placed instead of selectedClock
            UpdatePositionsAllClocks(clockFormWidth, clockFormHeight);
        }
    }


    /// <summary>
    /// Move the specified clock up in the list, adjusting its position accordingly.
    /// </summary>
    /// <param name="clock"></param>
    public void MoveClockUp(Clock clock)
    {
        int index = clocks.IndexOf(clock);
        if (index > 0)
        {
            clocks.RemoveAt(index);
            clocks.Insert(index - 1, clock);
            UpdatePositionsAllClocks(clockFormWidth, clockFormHeight);
        }
    }


    /// <summary>
    /// Move the specified clock down in the list, adjusting its position accordingly.
    /// </summary>
    /// <param name="clock"></param>
    public void MoveClockDown(Clock clock)
    {
        int index = clocks.IndexOf(clock);
        if (index < clocks.Count - 1)
        {
            clocks.RemoveAt(index);
            clocks.Insert(index + 1, clock);
            UpdatePositionsAllClocks(clockFormWidth, clockFormHeight);
        }
    }


    public void DisposeAllClocks()
	{
		foreach (var clock in clocks)
		{
			clock.Dispose();
		}
		clocks.Clear();
    }


}


