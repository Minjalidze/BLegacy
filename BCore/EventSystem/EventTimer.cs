using System;
using System.Diagnostics;
using System.Timers;

namespace BCore.EventSystem;

public class EventTimer : Timer
{
    public string Command = null;
    public NetUser Sender = null;
    public NetUser Target = null;
    private readonly Stopwatch Watch = new();

    public double TimeLeft
    {
        get
        {
            Watch.Stop();
            var elapsedTime = Watch.ElapsedMilliseconds;
            Watch.Start();
            return Math.Round((Interval - elapsedTime) / 1000);
        }
    }

    public new void Start()
    {
        EventSystem.Events.Timer.Add(this);
        Watch.Start();
        base.Start();
    }

    public new void Dispose()
    {
        EventSystem.Events.Timer.Remove(this);
        Watch.Stop();
        base.Dispose();
    }
}