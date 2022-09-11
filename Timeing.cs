using System;

namespace Microsoft.Xna.Framework
{
    /// <summary>
    /// Timer class based on seconds.
    /// The timer is a non zero based count up timer.
    /// </summary>
    public class Timeing
    {
        private float timer = 0f;
        private float elapsedTime = 0f;
        private float multiplier = 1f;
        bool isTriggered = false;
        const float pi = 3.141592653f;

        public Timeing()
        {
            Timer = 1.0f;
            IsActivelyTiming = false;
            isTriggered = false;
        }
        public Timeing(float seconds)
        {
            Timer = seconds;
            IsActivelyTiming = true;
            isTriggered = false;
        }
        public Timeing(float seconds, bool startingStateActive)
        {
            Timer = seconds;
            IsActivelyTiming = startingStateActive;
            isTriggered = false;
        }

        public bool IsActivelyTiming { get; set; } = false;

        /// <summary>
        /// set or get the timer amount directly.
        /// </summary>
        public float Timer
        {
            set { timer = value; }
            get { return timer; }
        }

        /// <summary>
        /// set or get the elapsedTime from zero directly.
        /// </summary>
        public float Elapsed
        {
            set { elapsedTime = value; }
            get { return elapsedTime; }
        }

        /// <summary>
        /// This value multiplies the amount to add to the elapsed time when updated.
        /// </summary>
        public float Multiplier
        {
            set { multiplier = value; }
            get { return multiplier; }
        }

        public float SetTimerAndStart
        {
            set
            {
                timer = value;
                IsActivelyTiming = true;
                isTriggered = false;
            }
        }

        public float AddToTimer
        {
            set { timer += value; }
        }
        public float AddToElapsed
        {
            set { elapsedTime += value; }
        }
        public float AddToMultiplier
        {
            set { multiplier += value; }
        }

        public void StartTiming()
        {
            IsActivelyTiming = true;
        }
        public void StopTiming()
        {
            IsActivelyTiming = false;
        }

        /// <summary>
        /// Set the elapsed to timer value if actively timing the timer will be triggered.
        /// </summary>
        public void TriggerTimer()
        {
            elapsedTime = timer;
        }

        /// <summary>
        /// Leaves the timer running, retains the timing amount and restarts the timer at zero.
        /// </summary>
        public void ResetElapsedToZeroAndStartTiming()
        {
            elapsedTime = 0;
            isTriggered = false;
            IsActivelyTiming = true;
        }
        /// <summary>
        /// Turns off the timer completely.
        /// </summary>
        public void ClearStopResetTiming()
        {
            elapsedTime = 0;
            isTriggered = false;
            multiplier = 1f;
            IsActivelyTiming = false;
        }

        /// <summary>
        /// When used as a stop watch.
        /// </summary>
        public bool IsTriggered
        {
            get { return isTriggered; }
        }

        /// <summary>
        /// When used as a stop watch.
        /// same as istriggered.
        /// </summary>
        public bool IsTimedAmountReached
        {
            get { return isTriggered; }
        }

        /// <summary>
        /// performs a rotation of time multiplied by 2 pi
        /// </summary>
        public float GetElapsedTimeAsRadianRotation
        {
            get { return GetElapsedPercentageOfTimer * 2 * pi; }
        }
        /// <summary>
        /// Gets back the time in the form of the Sin portion of Sin Cos
        /// </summary>
        public float GetElapsedTimeAsSine
        {
            get { return (float)Math.Sin(GetElapsedPercentageOfTimer * 2 * pi); }
        }
        /// <summary>
        /// Gets back the time in the form of the Cos portion of Sin Cos
        /// </summary>
        public float GetElapsedTimeAsCosine
        {
            get { return (float)Math.Cos(GetElapsedPercentageOfTimer * 2 * pi); }
        }
        /// <summary>
        /// Get the elapsed time as a percentage from o to 1
        /// </summary>
        public float GetElapsedPercentageOfTimer
        {
            get
            {
                if (timer > 0f)
                    return elapsedTime / timer;
                else
                    return 0;
            }
        }
        /// <summary>
        /// Get the elapsed time as a percentage from 1 to 0
        /// </summary>
        public float GetElapsedPercentageOfTimerInverted
        {
            get
            {
                if (timer > 0f)
                    return 1f - (elapsedTime / timer);
                else
                    return 0;
            }
        }
        /// <summary>
        /// performs a linear oscilation on the given time this ranges from 0 to 1 to 0
        /// </summary>
        public float GetElapsedPercentageAsOscillation
        {
            get
            {
                if (timer > 0f)
                {
                    var half = timer * .5f;
                    var n = (elapsedTime - half) / half;
                    if (n < 0f)
                        n = -n;
                    return 1f - n;
                }
                else
                    return 0;
            }
        }
        /// <summary>
        /// performs a linear oscilation on the given time this ranges from 1 to 0 to 1
        /// </summary>
        public float GetElapsedPercentageAsOscillationInverted
        {
            get
            {
                return 1f - GetElapsedPercentageAsOscillation;
            }
        }

        /// <summary>
        /// Returns true if the time reaches its timer amount.
        /// Calls to the float overload.
        /// </summary>
        public bool Update(GameTime gameTime)
        {
            return Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        }
        /// <summary>
        /// Returns true if the time reaches its timer amount.
        /// Timed values increment till they reach the set amount.
        /// </summary>
        public bool Update(float elapsedTimeInSeconds)
        {
            elapsedTime += elapsedTimeInSeconds * multiplier;
            if (elapsedTime >= timer)
            {
                isTriggered = true;
                elapsedTime = timer;
            }
            return isTriggered;
        }
        /// <summary>
        /// Updates continuously typically used for oscillarions or looping algorithms.
        /// If the timer increments to its max amount it wraps around to zero and continues.
        /// Calls to the float overload.
        /// </summary>
        public void UpdateContinuously(GameTime gameTime)
        {
            UpdateContinuously((float)(gameTime.ElapsedGameTime.TotalSeconds));
        }
        /// <summary>
        /// Updates continuously typically used for oscillarions or looping algorithms.
        /// If the timer increments to its max amount it wraps around to zero and continues.
        /// </summary>
        public void UpdateContinuously(float elapsedTimeInSeconds)
        {
            elapsedTime += elapsedTimeInSeconds * multiplier;
            if (elapsedTime >= timer)
            {
                elapsedTime -= timer;
                IsActivelyTiming = true;
            }
        }
    }
}
