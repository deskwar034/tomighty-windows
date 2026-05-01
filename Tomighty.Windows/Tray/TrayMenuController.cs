//
//  Tomighty - http://www.tomighty.org
//
//  This software is licensed under the Apache License Version 2.0:
//  http://www.apache.org/licenses/LICENSE-2.0.txt
//

using System;
using System.Windows.Forms;
using Tomighty.Events;

namespace Tomighty.Windows.Tray
{
    internal class TrayMenuController
    {
        private readonly ITrayMenu menu;
        private readonly ApplicationContext app;
        private readonly IPomodoroEngine pomodoroEngine;

        public TrayMenuController(ITrayMenu menu, ApplicationContext app, IPomodoroEngine pomodoroEngine, IEventHub eventHub)
        {
            this.menu = menu;
            this.app = app;
            this.pomodoroEngine = pomodoroEngine;

            menu.OnStartPomodoroClick(OnStartPomodoroClick);
            menu.OnStartLongBreakClick(OnStartLongBreakClick);
            menu.OnStartShortBreakClick(OnStartShortBreakClick);
            menu.OnPauseTimerClick(OnPauseTimerClick);
            menu.OnResumeTimerClick(OnResumeTimerClick);
            menu.OnStopTimerClick(OnStopTimerClick);
            menu.OnResetPomodoroCountClick(OnResetPomodoroCountClick);
            menu.OnExitClick(OnExitClick);

            eventHub.Subscribe<TimerStarted>(OnTimerStarted);
            eventHub.Subscribe<TimerStopped>(OnTimerStopped);
            eventHub.Subscribe<TimerPaused>(OnTimerPaused);
            eventHub.Subscribe<TimerResumed>(OnTimerResumed);
            eventHub.Subscribe<TimeElapsed>(OnTimeElasped);
            eventHub.Subscribe<PomodoroCountChanged>(OnPomodoroCountChanged);

            menu.Update(mutator =>
            {
                mutator.UpdateRemainingTime(Duration.Zero.ToTimeString());
                mutator.UpdatePomodoroCount(0);
                mutator.EnableStopTimerItem(false);
                mutator.EnablePauseTimerItem(false);
                mutator.EnableResumeTimerItem(false);
            });
        }

        private void OnStartPomodoroClick(object sender, EventArgs e) => StartTimer(IntervalType.Pomodoro);
        private void OnStartLongBreakClick(object sender, EventArgs e) => StartTimer(IntervalType.LongBreak);
        private void OnStartShortBreakClick(object sender, EventArgs e) => StartTimer(IntervalType.ShortBreak);
        private void OnStopTimerClick(object sender, EventArgs e) => pomodoroEngine.StopTimer();
        private void OnPauseTimerClick(object sender, EventArgs e) => pomodoroEngine.PauseTimer();
        private void OnResumeTimerClick(object sender, EventArgs e) => pomodoroEngine.ResumeTimer();
        private void OnResetPomodoroCountClick(object sender, EventArgs e) => pomodoroEngine.ResetPomodoroCount();

        private void OnExitClick(object sender, EventArgs e)
        {
            app.ExitThread();
        }

        private void OnTimerStarted(TimerStarted @event)
        {
            menu.Update(mutator =>
            {
                mutator.UpdateRemainingTime(@event.RemainingTime.ToTimeString());
                mutator.EnableStartPomodoroItem(false);
                mutator.EnableStartShortBreakItem(false);
                mutator.EnableStartLongBreakItem(false);
                mutator.EnableStopTimerItem(true);
                mutator.EnablePauseTimerItem(true);
                mutator.EnableResumeTimerItem(false);
            });
        }

        private void OnTimerStopped(TimerStopped timerStopped)
        {
            menu.Update(mutator =>
            {
                mutator.UpdateRemainingTime(Duration.Zero.ToTimeString());
                mutator.EnableStartPomodoroItem(true);
                mutator.EnableStartShortBreakItem(true);
                mutator.EnableStartLongBreakItem(true);
                mutator.EnableStopTimerItem(false);
                mutator.EnablePauseTimerItem(false);
                mutator.EnableResumeTimerItem(false);
            });
        }

        private void OnTimerPaused(TimerPaused @event)
        {
            menu.Update(mutator =>
            {
                mutator.UpdateRemainingTime(@event.RemainingTime.ToTimeString());
                mutator.EnablePauseTimerItem(false);
                mutator.EnableResumeTimerItem(true);
                mutator.EnableStopTimerItem(true);
            });
        }

        private void OnTimerResumed(TimerResumed @event)
        {
            menu.Update(mutator =>
            {
                mutator.UpdateRemainingTime(@event.RemainingTime.ToTimeString());
                mutator.EnablePauseTimerItem(true);
                mutator.EnableResumeTimerItem(false);
                mutator.EnableStopTimerItem(true);
            });
        }

        private void OnTimeElasped(TimeElapsed @event)
        {
            menu.Update(mutator =>
            {
                mutator.UpdateRemainingTime(@event.RemainingTime.ToTimeString());
            });
        }
        
        private void OnPomodoroCountChanged(PomodoroCountChanged @event)
        {
            menu.Update(mutator =>
            {
                var count = @event.PomodoroCount;
                mutator.UpdatePomodoroCount(count);
                mutator.EnableResetPomodoroCountItem(count > 0);
            });
        }
        
        private void StartTimer(IntervalType intervalType)
        {
            pomodoroEngine.StartTimer(intervalType);
        }
    }
}
