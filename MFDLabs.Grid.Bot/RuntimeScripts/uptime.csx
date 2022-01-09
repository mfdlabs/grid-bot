using System;
var elapsed = MFDLabs.Logging.Diagnostics.LoggingSystem.Singleton.GlobalLifetimeWatch.Elapsed;
var ticks = elapsed.Ticks;
long time = ticks % TimeSpan.TicksPerDay;
int day = (int)(ticks / TimeSpan.TicksPerDay);
var hours = (int)(time / TimeSpan.TicksPerHour % 24);
var minutes = (int)(time / TimeSpan.TicksPerMinute % 60);
var seconds = (int)(time / TimeSpan.TicksPerSecond % 60);
return $"Bot has been alive for {day} days, {hours} hours, {minutes} minutes and {seconds} seconds";