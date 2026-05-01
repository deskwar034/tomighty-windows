//
//  Tomighty - http://www.tomighty.org
//
//  This software is licensed under the Apache License Version 2.0:
//  http://www.apache.org/licenses/LICENSE-2.0.txt
//

using System;
using System.Drawing;
using System.Windows.Forms;
using Tomighty.Events;

namespace Tomighty.Windows.Timer
{
    internal class TimerWindowPresenter
    {
        private readonly ICountdownClock countdownClock;
        private readonly IWindowState idleState;
        private readonly IWindowState pomodoroState;
        private readonly IWindowState shortBreakState;
        private readonly IWindowState longBreakState;
        private readonly IWindowState pomodoroPausedState;
        private readonly IWindowState shortBreakPausedState;
        private readonly IWindowState longBreakPausedState;
        private readonly IWindowState pomodoroCompletedState;
        private readonly IWindowState breakFinishedState;
        private readonly IWindowState pomodoroInterruptedState;
        private readonly IWindowState breakInterruptedState;

        private IWindowState currentState;
        private Duration lastKnownRemainingTime = Duration.Zero;
        private TimerWindow window;
        private Taskbar _taskbar;
        private bool _isPinned;

        public TimerWindowPresenter(IPomodoroEngine pomodoroEngine, ICountdownClock countdownClock, IEventHub eventHub)
        {
            this.countdownClock = countdownClock;

            idleState = new IdleState(pomodoroEngine);
            pomodoroState = new PomodoroState(pomodoroEngine);
            shortBreakState = new ShortBreakState(pomodoroEngine);
            longBreakState = new LongBreakState(pomodoroEngine);
            pomodoroPausedState = new PausedState("Pomodoro Paused", TimerWindow.Red, pomodoroEngine);
            shortBreakPausedState = new PausedState("Short Break Paused", TimerWindow.Green, pomodoroEngine);
            longBreakPausedState = new PausedState("Long Break Paused", TimerWindow.Blue, pomodoroEngine);
            pomodoroCompletedState = new PomodoroCompletedState(pomodoroEngine);
            breakFinishedState = new BreakFinishedState(pomodoroEngine);
            pomodoroInterruptedState = new TimerInterruptedState("Pomodoro Interrupted", pomodoroEngine);
            breakInterruptedState = new TimerInterruptedState("Break Interrupted", pomodoroEngine);

            currentState = idleState;
            lastKnownRemainingTime = Duration.Zero;

            eventHub.Subscribe<TimerStarted>(OnTimerStarted);
            eventHub.Subscribe<TimeElapsed>(OnTimeElapsed);
            eventHub.Subscribe<TimerStopped>(OnTimerStopped);
            eventHub.Subscribe<TimerPaused>(OnTimerPaused);
            eventHub.Subscribe<TimerResumed>(OnTimerResumed);
        }

        private TimerWindow CreateTimerWindow()
        {
            var window = new TimerWindow();

            foreach (var control in window.Controls)
                ((Control)control).LostFocus += OnLostFocus;

            window.LostFocus += OnLostFocus;
            window.VisibleChanged += OnWindowVisibleChanged;
            window.PinButton.Click += OnPinButtonClick;
            window.CloseButton.Click += OnCloseButtonClick;

            new DragAroundController(window, () => IsPinned);

            return window;
        }
        
        public void Toggle(Point approximateLocation)
        {
            var shouldCreateWindow = window == null;

            if (shouldCreateWindow)
            {
                window = CreateTimerWindow();
            }

            if (window.Visible)
            {
                if (!IsPinned)
                    window.Hide();
            }
            else
            {
                Point location = GetLocationNearTrayIcon(approximateLocation);
                window.Show(location);

                if (shouldCreateWindow)
                {
                    currentState.Apply(window, lastKnownRemainingTime);
                }
            }
        }

        private void OnTimerStarted(TimerStarted @event)
        {
            EnterState(GetRunningTimerStateFor(@event.IntervalType), @event.RemainingTime);
            ApplyStateToWindow();
        }

        private void OnTimerStopped(TimerStopped @event)
        {
            if (@event.IsIntervalCompleted)
            {
                EnterState(GetCompletedIntervalStateFor(@event.IntervalType), @event.RemainingTime);
            }
            else
            {
                EnterState(GetTimerInterruptedStateFor(@event.IntervalType), @event.RemainingTime);
            }
            ApplyStateToWindow();
        }

        private void OnTimerPaused(TimerPaused @event)
        {
            EnterState(GetPausedTimerStateFor(@event.IntervalType), @event.RemainingTime);
            ApplyStateToWindow();
        }

        private void OnTimerResumed(TimerResumed @event)
        {
            EnterState(GetRunningTimerStateFor(@event.IntervalType), @event.RemainingTime);
            ApplyStateToWindow();
        }

        private void OnTimeElapsed(TimeElapsed @event)
        {
            lastKnownRemainingTime = @event.RemainingTime;
            RunOnUiThread(() => window.UpdateTimeDisplay(lastKnownRemainingTime.ToTimeString()));
        }
        
        private void EnterState(IWindowState newState, Duration remainingTime)
        {
            lastKnownRemainingTime = remainingTime;
            currentState = newState;
        }

        private IWindowState GetRunningTimerStateFor(IntervalType intervalType)
        {
            if (intervalType == IntervalType.Pomodoro) return pomodoroState;
            if (intervalType == IntervalType.ShortBreak) return shortBreakState;
            if (intervalType == IntervalType.LongBreak) return longBreakState;
            throw new ArgumentException($"Unknown interval type: {intervalType}");
        }

        private IWindowState GetCompletedIntervalStateFor(IntervalType intervalType)
        {
            if (intervalType == IntervalType.Pomodoro) return pomodoroCompletedState;
            if (intervalType == IntervalType.ShortBreak || intervalType == IntervalType.LongBreak) return breakFinishedState;
            throw new ArgumentException($"Unknown interval type: {intervalType}");
        }

        private IWindowState GetPausedTimerStateFor(IntervalType intervalType)
        {
            if (intervalType == IntervalType.Pomodoro) return pomodoroPausedState;
            if (intervalType == IntervalType.ShortBreak) return shortBreakPausedState;
            if (intervalType == IntervalType.LongBreak) return longBreakPausedState;
            throw new ArgumentException($"Unknown interval type: {intervalType}");
        }

        private IWindowState GetTimerInterruptedStateFor(IntervalType intervalType)
        {
            if (intervalType == IntervalType.Pomodoro) return pomodoroInterruptedState;
            if (intervalType == IntervalType.ShortBreak || intervalType == IntervalType.LongBreak) return breakInterruptedState;
            throw new ArgumentException($"Unknown interval type: {intervalType}");
        }

        private Point GetLocationNearTrayIcon(Point approximateLocation)
        {
            var screen = Screen.FromPoint(approximateLocation);
            var workingArea = screen.WorkingArea;
            var x = approximateLocation.X - window.Width / 2;
            var y = workingArea.Bottom - window.Height - 2;
            x = Math.Max(workingArea.Left, Math.Min(x, workingArea.Right - window.Width));
            y = Math.Max(workingArea.Top, Math.Min(y, workingArea.Bottom - window.Height));
            return new Point(x, y);
        }

        private void ApplyStateToWindow()
        {
            RunOnUiThread(() => currentState.Apply(window, lastKnownRemainingTime));
        }

        private void RunOnUiThread(Action action)
        {
            if (action == null)
                return;

            var currentWindow = window;
            if (currentWindow == null || currentWindow.IsDisposed || currentWindow.Disposing)
                return;

            if (currentWindow.InvokeRequired)
            {
                currentWindow.BeginInvoke(action);
                return;
            }

            action();
        }

        private void OnPinButtonClick(object sender, System.EventArgs e)
        {
            IsPinned = !IsPinned;
        }

        private void OnCloseButtonClick(object sender, System.EventArgs e)
        {
            window.Hide();
            IsPinned = false;
        }

        private void OnWindowVisibleChanged(object sender, System.EventArgs e)
        {
            if (window.Visible)
                window.Focus();
        }

        private void OnLostFocus(object sender, System.EventArgs e)
        {
            if (!IsPinned && !window.ContainsFocus)
                window.Hide();
        }
        
        private Taskbar Taskbar => _taskbar == null ? _taskbar = new Taskbar() : _taskbar;

        private bool IsPinned
        {
            get { return _isPinned; }
            set
            {
                if (value != _isPinned)
                {
                    _isPinned = value;

                    if (window != null)
                    {
                        window.UpdatePinButtonState(_isPinned);
                    }
                }
            }
        }

        private interface IWindowState
        {
            void Apply(TimerWindow window, Duration remainingTime);
        }

        private class IdleState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;

            public IdleState(IPomodoroEngine pomodoroEngine)
            {
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Idle");
                window.UpdateColorScheme(TimerWindow.DarkGray);
                window.UpdateTimeDisplay(Duration.Zero.ToTimeString());
                window.SetTimerAction("Start Pomodoro", StartTimer);
            }

            private void StartTimer()
            {
                pomodoroEngine.StartTimer(IntervalType.Pomodoro);
            }
        }

        private class PomodoroState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;

            public PomodoroState(IPomodoroEngine pomodoroEngine)
            {
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Pomodoro");
                window.UpdateColorScheme(TimerWindow.Red);
                window.UpdateTimeDisplay(remainingTime.ToTimeString());
                window.SetTimerActions("Pause", pomodoroEngine.PauseTimer, "Interrupt", pomodoroEngine.StopTimer);
            }
        }

        private class ShortBreakState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;

            public ShortBreakState(IPomodoroEngine pomodoroEngine)
            {
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Short Break");
                window.UpdateColorScheme(TimerWindow.Green);
                window.UpdateTimeDisplay(remainingTime.ToTimeString());
                window.SetTimerActions("Pause", pomodoroEngine.PauseTimer, "Interrupt", pomodoroEngine.StopTimer);
            }
        }

        private class LongBreakState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;

            public LongBreakState(IPomodoroEngine pomodoroEngine)
            {
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Long Break");
                window.UpdateColorScheme(TimerWindow.Blue);
                window.UpdateTimeDisplay(remainingTime.ToTimeString());
                window.SetTimerActions("Pause", pomodoroEngine.PauseTimer, "Interrupt", pomodoroEngine.StopTimer);
            }
        }

        private class PausedState : IWindowState
        {
            private readonly string title;
            private readonly Color color;
            private readonly IPomodoroEngine pomodoroEngine;

            public PausedState(string title, Color color, IPomodoroEngine pomodoroEngine)
            {
                this.title = title;
                this.color = color;
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle(title);
                window.UpdateColorScheme(color);
                window.UpdateTimeDisplay(remainingTime.ToTimeString());
                window.SetTimerActions("Resume", pomodoroEngine.ResumeTimer, "Interrupt", pomodoroEngine.StopTimer);
            }
        }

        private class PomodoroCompletedState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;

            public PomodoroCompletedState(IPomodoroEngine pomodoroEngine)
            {
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Pomodoro Completed");
                window.UpdateColorScheme(TimerWindow.DarkGray);
                window.UpdateTimeDisplay(Duration.Zero.ToTimeString());
                window.SetTimerAction($"Start {pomodoroEngine.SuggestedBreakType.GetName()}", StartTimer);
            }

            private void StartTimer()
            {
                pomodoroEngine.StartTimer(pomodoroEngine.SuggestedBreakType);
            }
        }

        private class BreakFinishedState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;

            public BreakFinishedState(IPomodoroEngine pomodoroEngine)
            {
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Break Finished");
                window.UpdateColorScheme(TimerWindow.DarkGray);
                window.UpdateTimeDisplay(Duration.Zero.ToTimeString());
                window.SetTimerAction("Start Pomodoro", StartTimer);
            }

            private void StartTimer()
            {
                pomodoroEngine.StartTimer(IntervalType.Pomodoro);
            }
        }

        private class TimerInterruptedState : IWindowState
        {
            private readonly string title;
            private readonly IPomodoroEngine pomodoroEngine;

            public TimerInterruptedState(string title, IPomodoroEngine pomodoroEngine)
            {
                this.title = title;
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle(title);
                window.UpdateColorScheme(TimerWindow.DarkGray);
                window.UpdateTimeDisplay(remainingTime.ToTimeString());
                window.SetTimerAction("Start Pomodoro", StartTimer);
            }

            private void StartTimer()
            {
                pomodoroEngine.StartTimer(IntervalType.Pomodoro);
            }
        }

        private class DragAroundController
        {
            private readonly Form form;
            private readonly Func<bool> canStartDraggingAround;
            private bool dragAround;
            private int offsetX, offsetY;

            public DragAroundController(Form form, Func<bool> canStartDraggingAround)
            {
                this.form = form;
                this.canStartDraggingAround = canStartDraggingAround;

                form.MouseDown += OnMouseDown;
                form.MouseUp += OnMouseUp;
                form.MouseMove += OnMouseMove;
            }

            private void OnMouseDown(object sender, MouseEventArgs e)
            {
                if (canStartDraggingAround())
                {
                    dragAround = true;
                    offsetX = e.Location.X;
                    offsetY = e.Location.Y;
                }
            }

            private void OnMouseUp(object sender, MouseEventArgs e)
            {
                dragAround = false;
            }

            private void OnMouseMove(object sender, MouseEventArgs e)
            {
                if (dragAround)
                {
                    var relativeX = e.Location.X;
                    var relativeY = e.Location.Y;

                    var deltaX = relativeX - offsetX;
                    var deltaY = relativeY - offsetY;

                    form.Location = new Point(
                        form.Location.X + deltaX,
                        form.Location.Y + deltaY
                    );
                }
            }
        }
    }
}
