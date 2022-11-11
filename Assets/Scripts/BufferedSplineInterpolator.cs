using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class DynamicInterpolator<T> where T : struct
{

    internal float MaxInterpolationBound = 3.0f;

    // The interpolation type to use. This allows quick switching
    // between interpolation methods :)
    public enum InterpolationType {LINEAR, CUBIC_SPLINE};
    public InterpolationType interpolationType;

    private struct BufferedItem
    {
        // The data received
        public T Item;
        // The derivative of the data received, at the time sent
        public T Velocity;
        // The time the data was sent from the server
        public double TimeSent;

        public BufferedItem(T item, T velocity, double timeSent)
        {
            Item = item;
            Velocity = velocity;
            TimeSent = timeSent;
        }

    }

    /// <summary>
    /// There's two factors affecting interpolation: buffering (set in NetworkManager's NetworkTimeSystem) and interpolation time, which is the amount of time it'll take to reach the target. This is to affect the second one.
    /// </summary>
    public float MaximumInterpolationTime = 0.1f;

    // If this is true, interpolation is calculated separately for each buffer entry
    // This connects all data points together and is more accurate to the server data
    // However, this comes at a cost of computation time.
    public bool SnapshotInterpolation = false;

    private const double k_SmallValue = 9.999999439624929E-11; // copied from Vector3's equal operator

    private T m_InterpStartValue;
    private T m_InterpStartVelocity;

    private T m_CurrentInterpValue;
    private T m_CurrentInterpVelocity; // TODO do we need this

    private T m_InterpEndValue;
    private T m_InterpEndVelocity;

    private double m_EndTimeConsumed;
    private double m_StartTimeConsumed;

    private readonly List<BufferedItem> m_Buffer = new List<BufferedItem>(k_BufferCountLimit);

    // Constant absolute value for max buffer count instead of dynamic time based value. This is in case we have very low tick rates, so
    // that we don't have a very small buffer because of this.
    private const int k_BufferCountLimit = 100;

    private BufferedItem m_LastBufferedItemReceived;
    private int m_NbItemsReceivedThisFrame;
    private int m_LifetimeConsumedCount;

    private bool InvalidState => m_Buffer.Count == 0 && m_LifetimeConsumedCount == 0;

    /// <summary>
    /// Resets interpolator to initial state
    /// </summary>
    public void Clear()
    {
        m_Buffer.Clear();
        m_EndTimeConsumed = 0.0d;
        m_StartTimeConsumed = 0.0d;
    }

    /// <summary>
    /// Teleports current interpolation value to targetValue.
    /// </summary>
    /// <param name="targetValue">The target value to teleport instantly</param>
    /// <param name="serverTime">The current server time</param>
    public void ResetTo(T targetValue, T targetVelocity, double serverTime)
    {
        m_LifetimeConsumedCount = 1;
        m_InterpStartValue = targetValue;
        m_InterpStartValue = targetVelocity;
        m_InterpEndValue = targetValue;
        m_InterpEndVelocity = targetVelocity;
        m_CurrentInterpValue = targetValue;
        m_CurrentInterpVelocity = targetVelocity;
        m_Buffer.Clear();
        m_EndTimeConsumed = 0.0d;
        m_StartTimeConsumed = 0.0d;

        Update(0, serverTime, serverTime);
    }

    private void TryConsumeFromBuffer(double renderTime, double serverTime)
    {
        int consumedCount = 0;

        if (renderTime >= m_EndTimeConsumed)
        {
            BufferedItem? itemToInterpolateTo = null;
            int count = m_Buffer.Count;
            for (int i = m_Buffer.Count - 1; i >= 0; i--)
            {
                BufferedItem bufferedValue = m_Buffer[i];

                if (SnapshotInterpolation)
                {
                    // Snapshot interpolation
                    // End points are (current state, next state received by server)
                    // Can throw away all buffer entries older than current entry

                    // TODO implement snapshot interpolation with cubic splines here
                    throw new NotImplementedException("Snapshot interpolation has not been implemented yet!");
                }
                else
                {
                    // No snapshot interpolation
                    // End points are (current state, newest state received by server)
                    // Can throw away all buffer entries older than newest entry

                    // Find the buffer entry after serverTime if it exists
                    if (bufferedValue.TimeSent <= serverTime)
                    {
                        if (!itemToInterpolateTo.HasValue || bufferedValue.TimeSent > itemToInterpolateTo.Value.TimeSent)
                        {
                            // Newer entry in buffer, use this one

                            // Find start values
                            if (m_LifetimeConsumedCount == 0)
                            {
                                // Interpolator is not initialized, teleport
                                m_StartTimeConsumed = bufferedValue.TimeSent;
                                m_InterpStartValue = bufferedValue.Item;
                                m_InterpStartVelocity = bufferedValue.Velocity;
                            }
                            else if (consumedCount == 0)
                            {
                                // Interpolating to new value, end becomes start
                                m_StartTimeConsumed = m_EndTimeConsumed;
                                m_InterpStartValue = m_InterpEndValue;
                                m_InterpStartVelocity = m_InterpEndVelocity;
                            }

                            // Find end values 
                            if (bufferedValue.TimeSent > m_EndTimeConsumed)
                            {
                                itemToInterpolateTo = bufferedValue;
                                m_EndTimeConsumed = bufferedValue.TimeSent;
                                m_InterpEndValue = bufferedValue.Item;
                                m_InterpEndVelocity = bufferedValue.Velocity;
                            }
                        }

                        m_Buffer.RemoveAt(i);
                        consumedCount++;
                        m_LifetimeConsumedCount++;
                    }
                }
            }
        }
    }

    public T Update(float deltaTime, double renderTime, double serverTime)
    {
        TryConsumeFromBuffer(renderTime, serverTime);

        if (InvalidState)
        {
            // This keeps firing, no idea why so I just got rid of it
            //throw new InvalidOperationException("trying to update spline interpolator when no data has been added to it yet");
        }

        if (m_LifetimeConsumedCount >= 1)
        {
            float t = 1.0f;
            double range = m_EndTimeConsumed - m_StartTimeConsumed;
            // Only interpolate if difference in time is large enough
            if (range > k_SmallValue)
            {

                if (t < 0.0f)
                {
                    // There is no mechanism to guarantee renderTime to not be before m_StartTimeConsumed
                    // This clamps t to a minimum of 0 and fixes issues with longer frames and pauses
                    t = 0.0f;
                }

                // Unsure what this does??? -Quintin
                if (t > MaxInterpolationBound) // max extrapolation
                {
                    // TODO this causes issues with teleport, investigate
                    t = 1.0f;
                }
            }

            // Interpolate
            m_CurrentInterpValue = Interpolate(m_InterpStartValue, m_InterpStartVelocity, m_InterpEndValue, m_InterpEndVelocity, t, out m_CurrentInterpVelocity);
        }

        m_NbItemsReceivedThisFrame = 0;
        return m_CurrentInterpValue;
    }

    /// <summary>
    /// Add measurements to be used during interpolation. These will be buffered before being made available to be displayed as "latest value".
    /// </summary>
    /// <param name="newMeasurement">The new measurement value to use</param>
    /// <param name="sentTime">The time to record for measurement</param>
    public void AddMeasurement(T newMeasurement, T newVelocity, double sentTime)
    {
        m_NbItemsReceivedThisFrame++;

        // This situation can happen after a game is paused. When starting to receive again, the server will have sent a bunch of messages in the meantime
        // instead of going through thousands of value updates just to get a big teleport, we're giving up on interpolation and teleporting to the latest value
        if (m_NbItemsReceivedThisFrame > k_BufferCountLimit)
        {
            if (m_LastBufferedItemReceived.TimeSent < sentTime)
            {
                m_LastBufferedItemReceived = new BufferedItem(newMeasurement, newVelocity, sentTime);
                ResetTo(newMeasurement, newVelocity, sentTime);
                // Next line keeps renderTime above m_StartTimeConsumed. Fixes pause/unpause issues
                m_Buffer.Add(m_LastBufferedItemReceived);
            }

            return;
        }

        // Part the of reason for disabling extrapolation is how we add and use measurements over time.
        // TODO: Add detailed description of this area in Jira ticket
        if (sentTime > m_EndTimeConsumed || m_LifetimeConsumedCount == 0) // treat only if value is newer than the one being interpolated to right now
        {
            m_LastBufferedItemReceived = new BufferedItem(newMeasurement, newVelocity, sentTime);
            m_Buffer.Add(m_LastBufferedItemReceived);
        }
    }

    /// <summary>
    /// Gets latest value from the interpolator. This is updated every update as time goes by.
    /// </summary>
    /// <returns>The current interpolated value of type 'T'</returns>
    public T GetInterpolatedValue()
    {
        return m_CurrentInterpValue;
    }

    /// <summary>
    /// Gets latest velocity from the interpolator. This is updated every update as time goes by.
    /// </summary>
    /// <returns>The current interpolated velocity of type 'T'</returns>
    public T GetInterpolatedVelocity()
    {
        return m_CurrentInterpVelocity;
    }

    /// <summary>
    /// Method to override and adapted to the generic type. This assumes interpolation for that value will be clamped.
    /// </summary>
    /// <param name="start">The start value (min)</param>
    /// <param name="vel_start"> The start velocity</param>
    /// <param name="end">The end value (max)</param>
    /// <param name="vel_end"> The end velocity</param>
    /// <param name="time">The time value used to interpolate between start and end values (pos)</param>
    /// <returns>The interpolated value</returns>
    protected abstract T Interpolate(T start, T vel_start, T end, T vel_end, float time, out T vel_out);
}

public class DynamicInterpolatorFloat : DynamicInterpolator<float>
{
    protected override float Interpolate(float start, float vel_start, float end, float vel_end, float time, out float vel_out)
    {
        if (interpolationType == DynamicInterpolator<float>.InterpolationType.CUBIC_SPLINE) {
            // Implementation of cubic spline interpolation
            float a = 2f * start + vel_start - 2f * end + vel_end;
            float b = -3f * start + 3f * end - 2f * vel_start - vel_end;
            float c = vel_start;
            float d = start;
            vel_out = 3 * a * time * time + 2 * b * time + c;
            return a * time * time * time + b * time * time + c * time + d;
        } else {
            // Default- Linear interpolation

            // TODO how should we work with velocity here?
            vel_out = vel_start;

            return Mathf.Lerp(start, end, time);

        }
    }
}

// TODO implement quaternion spline interpolation??? Unsure if necessary