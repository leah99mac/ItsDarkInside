using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode.Components;
using Unity.Netcode;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class NetworkPositionTracker : NetworkBehaviour
{
    /// <summary>
    /// The default position change threshold value.
    /// Any changes above this threshold will be replicated.
    /// </summary>
    public const float PositionThresholdDefault = 0.001f;

    /// <summary>
    /// The default velocity change threshold value.
    /// Any changes above this threshold will be replicated.
    /// </summary>
    public const float VelocityThresholdDefault = 0.001f;

    // The rigidbody attached to this object
    private Rigidbody2D rb;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// The handler delegate type that takes client requested changes and returns resulting changes handled by the server.
    /// </summary>
    /// <param name="pos">The position requested by the client.</param>
    /// <param name="rot">The rotation requested by the client.</param>
    /// <param name="scale">The scale requested by the client.</param>
    /// <returns>The resulting position, rotation and scale changes after handling.</returns>
    public delegate (Vector3 pos, Vector3 vel) OnClientRequestChangeDelegate(Vector3 pos, Vector3 vel);

    /// <summary>
    /// The handler that gets invoked when server receives a change from a client.
    /// This handler would be useful for server to modify pos/rot/scale before applying client's request.
    /// </summary>
    public OnClientRequestChangeDelegate OnClientRequestChange;

    internal struct NetworkPositionTrackerState : INetworkSerializable
    {
        private const int k_InLocalSpaceBit = 0;
        private const int k_PositionXBit = 1;
        private const int k_PositionYBit = 2;
        private const int k_PositionZBit = 3;
        private const int k_VelocityXBit = 4;
        private const int k_VelocityYBit = 5;
        private const int k_VelocityZBit = 6;
        private const int k_TeleportingBit = 7;
        // bit 7 unused

        private byte m_Bitset;

        internal bool InLocalSpace
        {
            get => (m_Bitset & (1 << k_InLocalSpaceBit)) != 0;
            set
            {
                if (value) { m_Bitset = (byte)(m_Bitset | (1 << k_InLocalSpaceBit)); }
                else { m_Bitset = (byte)(m_Bitset & ~(1 << k_InLocalSpaceBit)); }
            }
        }

        // Position
        internal bool HasPositionX
        {
            get => (m_Bitset & (1 << k_PositionXBit)) != 0;
            set
            {
                if (value) { m_Bitset = (byte)(m_Bitset | (1 << k_PositionXBit)); }
                else { m_Bitset = (byte)(m_Bitset & ~(1 << k_PositionXBit)); }
            }
        }

        internal bool HasPositionY
        {
            get => (m_Bitset & (1 << k_PositionYBit)) != 0;
            set
            {
                if (value) { m_Bitset = (byte)(m_Bitset | (1 << k_PositionYBit)); }
                else { m_Bitset = (byte)(m_Bitset & ~(1 << k_PositionYBit)); }
            }
        }

        internal bool HasPositionZ
        {
            get => (m_Bitset & (1 << k_PositionZBit)) != 0;
            set
            {
                if (value) { m_Bitset = (byte)(m_Bitset | (1 << k_PositionZBit)); }
                else { m_Bitset = (byte)(m_Bitset & ~(1 << k_PositionZBit)); }
            }
        }

        internal bool HasPositionChange
        {
            get
            {
                return HasPositionX | HasPositionY | HasPositionZ;
            }
        }

        // Velocity
        internal bool HasVelocityX
        {
            get => (m_Bitset & (1 << k_VelocityXBit)) != 0;
            set
            {
                if (value) { m_Bitset = (byte)(m_Bitset | (1 << k_VelocityXBit)); }
                else { m_Bitset = (byte)(m_Bitset & ~(1 << k_VelocityXBit)); }
            }
        }

        internal bool HasVelocityY
        {
            get => (m_Bitset & (1 << k_VelocityYBit)) != 0;
            set
            {
                if (value) { m_Bitset = (byte)(m_Bitset | (1 << k_VelocityYBit)); }
                else { m_Bitset = (byte)(m_Bitset & ~(1 << k_VelocityYBit)); }
            }
        }

        internal bool HasVelocityZ
        {
            get => (m_Bitset & (1 << k_VelocityZBit)) != 0;
            set
            {
                if (value) { m_Bitset = (byte)(m_Bitset | (1 << k_VelocityZBit)); }
                else { m_Bitset = (byte)(m_Bitset & ~(1 << k_VelocityZBit)); }
            }
        }

        internal bool HasVelocityChange
        {
            get
            {
                return HasVelocityX | HasVelocityY | HasVelocityZ;
            }
        }

        // Teleporting
        internal bool IsTeleportingNextFrame
        {
            get => (m_Bitset & (1 << k_TeleportingBit)) != 0;
            set
            {
                if (value) { m_Bitset = (byte)(m_Bitset | (1 << k_TeleportingBit)); }
                else { m_Bitset = (byte)(m_Bitset & ~(1 << k_TeleportingBit)); }
            }
        }

        internal float PositionX, PositionY, PositionZ;
        internal float VelocityX, VelocityY, VelocityZ;
        internal double SentTime;

        // Authoritative and non-authoritative sides use this to determine if a NetworkPositionTrackerState is
        // dirty or not.
        internal bool IsDirty;

        // Non-Authoritative side uses this for ending extrapolation of the last applied state
        internal int EndExtrapolationTick;

        /// <summary>
        /// This will reset the NetworkTransform BitSet
        /// </summary>
        internal void ClearBitSetForNextTick()
        {
            // We need to preserve the local space settings for the current state
            m_Bitset &= (byte)(m_Bitset & (1 << k_InLocalSpaceBit));
            IsDirty = false;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SentTime);
            // InLocalSpace + HasXXX Bits
            serializer.SerializeValue(ref m_Bitset);
            // Position Values
            if (HasPositionX)
            {
                serializer.SerializeValue(ref PositionX);
            }

            if (HasPositionY)
            {
                serializer.SerializeValue(ref PositionY);
            }

            if (HasPositionZ)
            {
                serializer.SerializeValue(ref PositionZ);
            }

            // Velocity Values
            if (HasVelocityX)
            {
                serializer.SerializeValue(ref VelocityX);
            }

            if (HasVelocityY)
            {
                serializer.SerializeValue(ref VelocityY);
            }

            if (HasVelocityZ)
            {
                serializer.SerializeValue(ref VelocityZ);
            }

            // Only if we are receiving state
            if (serializer.IsReader)
            {
                // Go ahead and mark the local state dirty or not dirty as well
                /// <see cref="TryCommitTransformToServer"/>
                IsDirty = HasPositionChange || HasVelocityChange;
            }
        }
    }

    /// <summary>
    /// Whether or not x component of position will be replicated
    /// </summary>
    public bool SyncPositionX = true;
    /// <summary>
    /// Whether or not y component of position will be replicated
    /// </summary>
    public bool SyncPositionY = true;
    /// <summary>
    /// Whether or not z component of position will be replicated
    /// </summary>
    public bool SyncPositionZ = true;

    private bool SynchronizePosition
    {
        get
        {
            return SyncPositionX || SyncPositionY || SyncPositionZ;
        }
    }

    /// <summary>
    /// Whether or not x component of velocity will be replicated
    /// </summary>
    public bool SyncVelocityX = true;
    /// <summary>
    /// Whether or not y component of velocity will be replicated
    /// </summary>
    public bool SyncVelocityY = true;
    /// <summary>
    /// Whether or not z component of velocity will be replicated
    /// </summary>
    public bool SyncVelocityZ = true;

    private bool SynchronizeVelocity
    {
        get
        {
            return SyncVelocityX || SyncVelocityY || SyncVelocityZ;
        }
    }

    /// <summary>
    /// The current position threshold value
    /// Any changes to the position that exceeds the current threshold value will be replicated
    /// </summary>
    public float PositionThreshold = PositionThresholdDefault;

    /// <summary>
    /// The current velocity threshold value
    /// Any changes to the velocity that exceeds the current threshold value will be replicated
    /// </summary>
    public float VelocityThreshold = VelocityThresholdDefault;

    /// <summary>
    /// Sets whether the transform should be treated as local (true) or world (false) space.
    /// </summary>
    /// <remarks>
    /// This should only be changed by the authoritative side during runtime. Non-authoritative
    /// changes will be overridden upon the next state update.
    /// </remarks>
    [Tooltip("Sets whether this transform should sync in local space or in world space")]
    public bool InLocalSpace = false;

    // Whether to interpolate or not
    public bool interpolate = true;

    // Use snapshot interpolation
    public bool snapshotInterpolation = true;

    // The interpolation type to use. Can be updated dynamically to change interpolation type
    // Will do nothing server-side.
    public DynamicInterpolatorFloat.InterpolationType interpolationType;



    /// <summary>
    /// Used to determine who can write to this transform. Server only for this transform.
    /// Changing this value alone in a child implementation will not allow you to create a NetworkTransform which can be written to by clients. See the ClientNetworkTransform Sample
    /// in the package samples for how to implement a NetworkTransform with client write support.
    /// If using different values, please use RPCs to write to the server. Netcode doesn't support client side network variable writing
    /// </summary>
    public bool CanCommitToTransform { get; protected set; }

    /// <summary>
    /// Internally used by <see cref="NetworkTransform"/> to keep track of whether this <see cref="NetworkBehaviour"/> derived class instance
    /// was instantiated on the server side or not.
    /// </summary>
    protected bool m_CachedIsServer;

    /// <summary>
    /// Internally used by <see cref="NetworkTransform"/> to keep track of the <see cref="NetworkManager"/> instance assigned to this
    /// this <see cref="NetworkBehaviour"/> derived class instance.
    /// </summary>
    protected NetworkManager m_CachedNetworkManager;

    /// <summary>
    /// We have two internal NetworkVariables.
    /// One for server authoritative and one for "client/owner" authoritative.
    /// </summary>
    private readonly NetworkVariable<NetworkPositionTrackerState> m_ReplicatedNetworkStateServer = new NetworkVariable<NetworkPositionTrackerState>(new NetworkPositionTrackerState(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<NetworkPositionTrackerState> m_ReplicatedNetworkStateOwner = new NetworkVariable<NetworkPositionTrackerState>(new NetworkPositionTrackerState(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    internal NetworkVariable<NetworkPositionTrackerState> ReplicatedNetworkState
    {
        get
        {
            if (!IsServerAuthoritative())
            {
                return m_ReplicatedNetworkStateOwner;
            }
            return m_ReplicatedNetworkStateServer;
        }
    }

    // Used by both authoritative and non-authoritative instances.
    // This represents the most recent local authoritative state.
    private NetworkPositionTrackerState m_LocalAuthoritativeNetworkState;

    private ClientRpcParams m_ClientRpcParams = new ClientRpcParams() { Send = new ClientRpcSendParams() };
    private List<ulong> m_ClientIds = new List<ulong>() { 0 };

    // NEW- dynamic interpolators used instead of linear.
    private DynamicInterpolatorFloat m_PositionXInterpolator;
    private DynamicInterpolatorFloat m_PositionYInterpolator;
    private DynamicInterpolatorFloat m_PositionZInterpolator;
    private readonly List<DynamicInterpolatorFloat> m_AllFloatInterpolators = new List<DynamicInterpolatorFloat>(3);

    // Used by integration test
    private NetworkPositionTrackerState m_LastSentState;

    internal NetworkPositionTrackerState GetLastSentState()
    {
        return m_LastSentState;
    }

    /// <summary>
    /// This will try to send/commit the current transform delta states (if any)
    /// </summary>
    /// <remarks>
    /// Only client owners or the server should invoke this method
    /// </remarks>
    /// <param name="transformToCommit">the transform to be committed</param>
    /// <param name="dirtyTime">time it was marked dirty</param>
    protected void TryCommitTransformToServer(Transform transformToCommit, Rigidbody2D rbToCommit, double dirtyTime)
    {
        // Only client owners or the server should invoke this method
        if (!IsOwner && !m_CachedIsServer)
        {
            NetworkLog.LogError($"Non-owner instance, {name}, is trying to commit a transform!");
            return;
        }

        // If we are authority, update the authoritative state
        if (CanCommitToTransform)
        {
            UpdateAuthoritativeState(transform, rb);
        }
        else // Non-Authority
        {
            var position = InLocalSpace ? transformToCommit.localPosition : transformToCommit.position;
            var velocity = rbToCommit.velocity;
            // We are an owner requesting to update our state
            if (!m_CachedIsServer)
            {
                // Send velocity only if we need it
                if (interpolationType == DynamicInterpolator<float>.InterpolationType.CUBIC_SPLINE) 
                {
                    SetStateVelocityServerRpc(position, velocity, false);
                } else {
                    SetStateNoVelocityServerRpc(position, false);
                }
            }
            else // Server is always authoritative (including owner authoritative)
            {
                SetStateClientRpc(position, velocity, false);
            }
        } // 497
    }

    /// <summary>
    /// Authoritative side only
    /// If there are any transform delta states, this method will synchronize the
    /// state with all non-authority instances.
    /// </summary>
    private void TryCommitTransform(Transform transformToCommit, Rigidbody2D rbToCommit, double dirtyTime)
    {
        if (!CanCommitToTransform && !IsOwner)
        {
            NetworkLog.LogError($"[{name}] is trying to commit the transform without authority!");
            return;
        }

        // If the transform has deltas (returns dirty) then...
        if (ApplyTransformToNetworkState(ref m_LocalAuthoritativeNetworkState, dirtyTime, transformToCommit, rbToCommit))
        {
            // ...commit the state
            // This does run correctly (and only on server)
            ReplicatedNetworkState.Value = m_LocalAuthoritativeNetworkState;
        }
    }

    private void ResetInterpolatedStateToCurrentAuthoritativeState()
    {
        var serverTime = NetworkManager.ServerTime.Time;
        var position = InLocalSpace ? transform.localPosition : transform.position;
        var velocity = rb.velocity;
        m_PositionXInterpolator.ResetTo(position.x, velocity.x, serverTime);
        m_PositionYInterpolator.ResetTo(position.y, velocity.y, serverTime);
        m_PositionZInterpolator.ResetTo(position.z, 0f, serverTime); // No 3d velocity with 2d rigid body
    }

    /// <summary>
    /// Used for integration testing:
    /// Will apply the transform to the LocalAuthoritativeNetworkState and get detailed dirty information returned
    /// in the <see cref="NetworkPositionTrackerState"/> returned.
    /// </summary>
    /// <param name="transform">transform to apply</param>
    /// <returns>NetworkPositionTrackerState</returns>
    internal NetworkPositionTrackerState ApplyLocalNetworkState(Transform transform, Rigidbody2D rb)
    {
        // Since we never commit these changes, we need to simulate that any changes were committed previously and the bitset
        // value would already be reset prior to having the state applied
        m_LocalAuthoritativeNetworkState.ClearBitSetForNextTick();

        // Now check the transform for any threshold value changes
        ApplyTransformToNetworkStateWithInfo(ref m_LocalAuthoritativeNetworkState, m_CachedNetworkManager.LocalTime.Time, transform, rb);

        // Return the entire state to be used by the integration test
        return m_LocalAuthoritativeNetworkState;
    }

    /// <summary>
    /// Used for integration testing
    /// </summary>
    internal bool ApplyTransformToNetworkState(ref NetworkPositionTrackerState networkState, double dirtyTime, Transform transformToUse, Rigidbody2D rb)
    {
        return ApplyTransformToNetworkStateWithInfo(ref networkState, dirtyTime, transformToUse, rb);
    }

    /// <summary>
    /// Applies the transform to the <see cref="NetworkPositionTrackerState"/> specified.
    /// </summary>
    private bool ApplyTransformToNetworkStateWithInfo(ref NetworkPositionTrackerState networkState, double dirtyTime, Transform transformToUse, Rigidbody2D rbToUse)
    {
        var isDirty = false;
        var isPositionDirty = false;
        var isVelocityDirty = false;

        var position = InLocalSpace ? transformToUse.localPosition : transformToUse.position;
        var velocity = rbToUse.velocity;

        if (InLocalSpace != networkState.InLocalSpace)
        {
            networkState.InLocalSpace = InLocalSpace;
            isDirty = true;
        }

        if (SyncPositionX && (Mathf.Abs(networkState.PositionX - position.x) >= PositionThreshold || networkState.IsTeleportingNextFrame))
        {
            networkState.PositionX = position.x;
            networkState.HasPositionX = true;
            isPositionDirty = true;
        }

        if (SyncPositionY && (Mathf.Abs(networkState.PositionY - position.y) >= PositionThreshold || networkState.IsTeleportingNextFrame))
        {
            networkState.PositionY = position.y;
            networkState.HasPositionY = true;
            isPositionDirty = true;
        }

        if (SyncPositionZ && (Mathf.Abs(networkState.PositionZ - position.z) >= PositionThreshold || networkState.IsTeleportingNextFrame))
        {
            networkState.PositionZ = position.z;
            networkState.HasPositionZ = true;
            isPositionDirty = true;
        }

        if (SyncVelocityX && (Mathf.Abs(networkState.VelocityX - velocity.x) >= VelocityThreshold || networkState.IsTeleportingNextFrame))
        {
            networkState.VelocityX = velocity.x;
            networkState.HasVelocityX = true;
            isVelocityDirty = true;
        }

        if (SyncVelocityY && (Mathf.Abs(networkState.VelocityY - velocity.y) >= VelocityThreshold || networkState.IsTeleportingNextFrame))
        {
            networkState.VelocityY = velocity.y;
            networkState.HasVelocityY = true;
            isVelocityDirty = true;
        }

        isDirty |= isPositionDirty || isVelocityDirty;

        if (isDirty)
        {
            networkState.SentTime = dirtyTime;
        }

        /// We need to set this in order to know when we can reset our local authority state <see cref="Update"/>
        /// If our state is already dirty or we just found deltas (i.e. isDirty == true)
        networkState.IsDirty |= isDirty;
        return isDirty;
    }

    /// <summary>
    /// Applies the authoritative state to the transform
    /// </summary>
    private void ApplyAuthoritativeState()
    {
        var networkState = ReplicatedNetworkState.Value;
        var adjustedPosition = networkState.InLocalSpace ? transform.localPosition : transform.position;
        var adjustedVelocity = rb.velocity;

        // InLocalSpace Read:
        InLocalSpace = networkState.InLocalSpace;

        // NOTE ABOUT INTERPOLATING AND THE CODE BELOW:
        // We always apply the interpolated state for any axis we are synchronizing even when the state has no deltas
        // to assure we fully interpolate to our target even after we stop extrapolating 1 tick later.
        var useInterpolatedValue = !networkState.IsTeleportingNextFrame && interpolate;
        if (useInterpolatedValue)
        {
            if (SyncPositionX)
            {
                adjustedPosition.x = m_PositionXInterpolator.GetInterpolatedValue();
                adjustedVelocity.x = m_PositionXInterpolator.GetInterpolatedVelocity();
            }
            if (SyncPositionY)
            {
                adjustedPosition.y = m_PositionYInterpolator.GetInterpolatedValue();
                adjustedVelocity.y = m_PositionYInterpolator.GetInterpolatedVelocity();
            }
            if (SyncPositionZ)
            {
                adjustedPosition.z = m_PositionZInterpolator.GetInterpolatedValue();
                // There is no z component of velocity, ignore
            }
        }
        else
        {
            if (networkState.HasPositionX) { adjustedPosition.x = networkState.PositionX; }
            if (networkState.HasPositionY) { adjustedPosition.y = networkState.PositionY; }
            if (networkState.HasPositionZ) { adjustedPosition.z = networkState.PositionZ; }

            //if (networkState.HasVelocityX) { adjustedVelocity.x = networkState.VelocityX; }
            //if (networkState.HasVelocityY) { adjustedVelocity.y = networkState.VelocityY; }
        }

        // NOTE: The below conditional checks for applying axial values are required in order to
        // prevent the non-authoritative side from making adjustments when interpolation is off.

        // TODO: Determine if we want to enforce, frame by frame, the non-authoritative transform values.
        // We would want save the position, rotation, and scale (each individually) after applying each
        // authoritative transform state received. Otherwise, the non-authoritative side could make
        // changes to an axial value (if interpolation is turned off) until authority sends an update for
        // that same axial value. When interpolation is on, the state's values being synchronized are
        // always applied each frame.

        // Apply the new position if it has changed or we are interpolating and synchronizing position
        if (networkState.HasPositionChange || (useInterpolatedValue && SynchronizePosition))
        {
            if (InLocalSpace)
            {
                transform.localPosition = adjustedPosition;
            }
            else
            {
                transform.position = adjustedPosition;
            }
        }

        // Apply the new velocity if it has changed or we are interpolating and synchronizing velocity
        if (networkState.HasVelocityChange || (useInterpolatedValue && SynchronizeVelocity))
        {
            rb.velocity = adjustedVelocity;
        }
    }

    /// <summary>
    /// Only non-authoritative instances should invoke this
    /// </summary>
    private void AddInterpolatedState(NetworkPositionTrackerState newState)
    {
        var sentTime = newState.SentTime;
        var currentPosition = newState.InLocalSpace ? transform.localPosition : transform.position;
        var currentVelocity = rb.velocity;

        // When there is a change in interpolation or if teleporting, we reset
        if (!interpolate || (newState.InLocalSpace != InLocalSpace) || newState.IsTeleportingNextFrame)
        {
            InLocalSpace = newState.InLocalSpace;

            // we should clear our float interpolators
            foreach (var interpolator in m_AllFloatInterpolators)
            {
                interpolator.Clear();
            }

            // Adjust based on which axis changed
            if (newState.HasPositionX && newState.HasVelocityX)
            {
                m_PositionXInterpolator.ResetTo(newState.PositionX, newState.VelocityX, sentTime);
                currentPosition.x = newState.PositionX;
                currentVelocity.x = newState.VelocityX;
            }
            else if (newState.HasPositionX)
            {
                m_PositionXInterpolator.ResetTo(newState.PositionX, currentVelocity.x, sentTime);
            }

            if (newState.HasPositionY && newState.HasVelocityY)
            {
                m_PositionYInterpolator.ResetTo(newState.PositionY, newState.VelocityY, sentTime);
                currentPosition.y = newState.PositionY;
                currentVelocity.y = newState.VelocityY;
            }
            else if (newState.HasPositionY)
            {
                m_PositionYInterpolator.ResetTo(newState.PositionY, currentVelocity.y, sentTime);
            }

            if (newState.HasPositionZ)
            {
                m_PositionZInterpolator.ResetTo(newState.PositionZ, 0f, sentTime);
                currentPosition.z = newState.PositionZ;
            }

            // Apply the position
            if (newState.InLocalSpace)
            {
                transform.localPosition = currentPosition;
            }
            else
            {
                transform.position = currentPosition;
            }

            // Apply the velocity
            rb.velocity = currentVelocity;

            return;
        }

        // Apply axial changes from the new state
        if (newState.HasPositionX && newState.HasVelocityX)
        {
            m_PositionXInterpolator.AddMeasurement(newState.PositionX, newState.VelocityX, sentTime);
        }
        else if (newState.HasPositionX)
        {
            m_PositionXInterpolator.AddMeasurement(newState.PositionX, currentVelocity.x, sentTime);
        }

        if (newState.HasPositionY && newState.HasVelocityY)
        {
            m_PositionYInterpolator.AddMeasurement(newState.PositionY, newState.VelocityY, sentTime);
        }
        else if (newState.HasPositionY)
        {
            m_PositionYInterpolator.AddMeasurement(newState.PositionY, currentVelocity.y, sentTime);
        }

        if (newState.HasPositionZ)
        {
            m_PositionZInterpolator.AddMeasurement(newState.PositionZ, 0f, sentTime);
        }
    }

    /// <summary>
    /// Only non-authoritative instances should invoke this method
    /// </summary>
    private void OnNetworkStateChanged(NetworkPositionTrackerState oldState, NetworkPositionTrackerState newState)
    {
        if (!NetworkObject.IsSpawned)
        {
            return;
        }

        if (CanCommitToTransform)
        {
            // we're the authority, we ignore incoming changes
            return;
        }

        // Add measurements for the new state's deltas
        AddInterpolatedState(newState);
    }

    /// <summary>
    /// Will set the maximum interpolation boundary for the interpolators of this <see cref="NetworkTransform"/> instance.
    /// This value roughly translates to the maximum value of 't' in <see cref="Mathf.Lerp(float, float, float)"/> and
    /// <see cref="Mathf.LerpUnclamped(float, float, float)"/> for all transform elements being monitored by
    /// <see cref="NetworkTransform"/> (i.e. Position, Rotation, and Scale)
    /// </summary>
    /// <param name="maxInterpolationBound">Maximum time boundary that can be used in a frame when interpolating between two values</param>
    public void SetMaxInterpolationBound(float maxInterpolationBound)
    {
        m_PositionXInterpolator.MaxInterpolationBound = maxInterpolationBound;
        m_PositionYInterpolator.MaxInterpolationBound = maxInterpolationBound;
        m_PositionZInterpolator.MaxInterpolationBound = maxInterpolationBound;
    }

    /// <summary>
    /// Create interpolators when first instantiated to avoid memory allocations if the
    /// associated NetworkObject persists (i.e. despawned but not destroyed or pools)
    /// </summary>
    private void Awake()
    {

        // All other interpolators are BufferedLinearInterpolatorFloats
        m_PositionXInterpolator = new DynamicInterpolatorFloat();
        m_PositionYInterpolator = new DynamicInterpolatorFloat();
        m_PositionZInterpolator = new DynamicInterpolatorFloat();

        // Used to quickly iteration over the BufferedLinearInterpolatorFloat
        // instances
        if (m_AllFloatInterpolators.Count == 0)
        {
            if (SyncPositionX) m_AllFloatInterpolators.Add(m_PositionXInterpolator);
            if (SyncPositionY) m_AllFloatInterpolators.Add(m_PositionYInterpolator);
            if (SyncPositionZ) m_AllFloatInterpolators.Add(m_PositionZInterpolator);
        }
    }

    /// <inheritdoc/>
    public override void OnNetworkSpawn()
    {
        m_CachedIsServer = IsServer;
        m_CachedNetworkManager = NetworkManager;

        Initialize();

        // This assures the initial spawning of the object synchronizes all connected clients
        // with the current transform values. This should not be placed within Initialize since
        // that can be invoked when ownership changes.
        if (CanCommitToTransform)
        {
            rb = GetComponent<Rigidbody2D>();
            var currentPosition = InLocalSpace ? transform.localPosition : transform.position;
            var currentVelocity = rb.velocity;
            // Teleport to current position
            SetStateInternal(currentPosition, currentVelocity, true);

            // Force the state update to be sent
            TryCommitTransform(transform, rb, m_CachedNetworkManager.LocalTime.Time);
        }
    }

    /// <inheritdoc/>
    public override void OnNetworkDespawn()
    {
        ReplicatedNetworkState.OnValueChanged -= OnNetworkStateChanged;
    }

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        base.OnDestroy();
        m_ReplicatedNetworkStateServer.Dispose();
        m_ReplicatedNetworkStateOwner.Dispose();
    }

    /// <inheritdoc/>
    public override void OnGainedOwnership()
    {
        Initialize();
    }

    /// <inheritdoc/>
    public override void OnLostOwnership()
    {
        Initialize();
    }

    /// <summary>
    /// Initializes NetworkTransform when spawned and ownership changes.
    /// </summary>
    private void Initialize()
    {
        if (!IsSpawned)
        {
            return;
        }


        CanCommitToTransform = IsServerAuthoritative() ? IsServer : IsOwner;
        var replicatedState = ReplicatedNetworkState;
        m_LocalAuthoritativeNetworkState = replicatedState.Value;

        if (CanCommitToTransform)
        {
            replicatedState.OnValueChanged -= OnNetworkStateChanged;
        }
        else
        {
            replicatedState.OnValueChanged += OnNetworkStateChanged;

            // In case we are late joining
            ResetInterpolatedStateToCurrentAuthoritativeState();
        }
    }

    /// <summary>
    /// Directly sets a state on the authoritative transform.
    /// Owner clients can directly set the state on a server authoritative transform
    /// This will override any changes made previously to the transform
    /// This isn't resistant to network jitter. Server side changes due to this method won't be interpolated.
    /// The parameters are broken up into pos / rot / scale on purpose so that the caller can perturb
    ///  just the desired one(s)
    /// </summary>
    /// <param name="posIn"></param> new position to move to.  Can be null
    /// <param name="velIn"></param> new velocity to move to.  Can be null
    /// <param name="shouldGhostsInterpolate">Should other clients interpolate this change or not. True by default</param>
    /// new scale to scale to.  Can be null
    /// <exception cref="Exception"></exception>
    public void SetState(Vector3? posIn = null, Vector3? velIn = null, bool shouldGhostsInterpolate = true)
    {
        if (!IsSpawned)
        {
            return;
        }

        // Only the server or owner can invoke this method
        if (!IsOwner && !m_CachedIsServer)
        {
            throw new System.Exception("Non-owner client instance cannot set the state of the NetworkTransform!");
        }

        Vector3 pos = posIn == null ? InLocalSpace ? transform.localPosition : transform.position : posIn.Value;
        Vector3 vel = velIn == null ? rb.velocity : velIn.Value;

        if (!CanCommitToTransform)
        {
            // Preserving the ability for owner authoritative mode to accept state changes from server
            if (m_CachedIsServer)
            {
                m_ClientIds[0] = OwnerClientId;
                m_ClientRpcParams.Send.TargetClientIds = m_ClientIds;
                SetStateClientRpc(pos, vel, !shouldGhostsInterpolate, m_ClientRpcParams);
            }
            else // Preserving the ability for server authoritative mode to accept state changes from owner
            {
                // Send velocity only if we need it
                if (interpolationType == DynamicInterpolator<float>.InterpolationType.CUBIC_SPLINE) 
                {
                    SetStateVelocityServerRpc(pos, vel, !shouldGhostsInterpolate);
                } else {
                    SetStateNoVelocityServerRpc(pos, !shouldGhostsInterpolate);
                }
            }
            return;
        }

        SetStateInternal(pos, vel, !shouldGhostsInterpolate);
    }

    /// <summary>
    /// Authoritative only method
    /// Sets the internal state (teleporting or just set state) of the authoritative
    /// transform directly.
    /// </summary>
    private void SetStateInternal(Vector3 pos, Vector3 vel, bool shouldTeleport)
    {
        if (InLocalSpace)
        {
            transform.localPosition = pos;
        }
        else
        {
            transform.position = pos;
        }
        rb.velocity = vel;
        m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame = shouldTeleport;

        TryCommitTransform(transform, rb, m_CachedNetworkManager.LocalTime.Time);
    }

    /// <summary>
    /// Invoked by <see cref="SetState"/>, allows a non-owner server to update the transform state
    /// </summary>
    /// <remarks>
    /// Continued support for client-driven server authority model
    /// </remarks>
    [ClientRpc]
    private void SetStateClientRpc(Vector3 pos, Vector3 vel, bool shouldTeleport, ClientRpcParams clientRpcParams = default)
    {
        // Server dictated state is always applied
        SetStateInternal(pos, vel, shouldTeleport);
    }

    /// <summary>
    /// Invoked by <see cref="SetState"/>, allows an owner-client update the transform state
    /// </summary>
    /// <remarks>
    /// Continued support for client-driven server authority model
    /// </remarks>
    [ServerRpc]
    private void SetStateVelocityServerRpc(Vector3 pos, Vector3 vel, bool shouldTeleport)
    {
        // server has received this RPC request to move change transform. give the server a chance to modify or even reject the move
        if (OnClientRequestChange != null)
        {
            (pos, vel) = OnClientRequestChange(pos, vel);
        }
        SetStateInternal(pos, vel, shouldTeleport);
    }

    // An alternative RPC that does not take velocity, so network bandwidth is reduced
    [ServerRpc]
    private void SetStateNoVelocityServerRpc(Vector3 pos, bool shouldTeleport)
    {
        Vector3 vel = Vector3.zero;
        if (OnClientRequestChange != null)
        {
            (pos, vel) = OnClientRequestChange(pos, vel);
        }
        SetStateInternal(pos, vel, shouldTeleport);
    }

    /// <summary>
    /// Will update the authoritative transform state if any deltas are detected.
    /// This will also reset the m_LocalAuthoritativeNetworkState if it is still dirty
    /// but the replicated network state is not.
    /// </summary>
    /// <param name="transformSource">transform to be updated</param>
    private void UpdateAuthoritativeState(Transform transformSource, Rigidbody2D rbSource)
    {
        // If our replicated state is not dirty and our local authority state is dirty, clear it.
        if (!ReplicatedNetworkState.IsDirty() && m_LocalAuthoritativeNetworkState.IsDirty)
        {
            m_LastSentState = m_LocalAuthoritativeNetworkState;
            // Now clear our bitset and prepare for next network tick state update
            m_LocalAuthoritativeNetworkState.ClearBitSetForNextTick();
        }

        TryCommitTransform(transformSource, rbSource, m_CachedNetworkManager.LocalTime.Time);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// If you override this method, be sure that:
    /// - Non-owners always invoke this base class method when using interpolation.
    /// - Authority can opt to use <see cref="TryCommitTransformToServer"/> in place of invoking this base class method.
    /// - Non-authority owners can use <see cref="TryCommitTransformToServer"/> but should still invoke the this base class method when using interpolation.
    /// </remarks>
    protected virtual void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        // If we are authority, update the authoritative state
        if (CanCommitToTransform)
        {
            UpdateAuthoritativeState(transform, rb);
        }
        else // Non-Authority
        {
            if (interpolate)
            {
                var serverTime = NetworkManager.ServerTime;
                var cachedDeltaTime = Time.deltaTime;
                var cachedServerTime = serverTime.Time;
                var cachedRenderTime = serverTime.TimeTicksAgo(1).Time;
                foreach (var interpolator in m_AllFloatInterpolators)
                {
                    interpolator.interpolationType = interpolationType;
                    interpolator.SnapshotInterpolation = snapshotInterpolation;
                    interpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
                }
            }

            // Apply the current authoritative state
            ApplyAuthoritativeState();
        }
    }

    /// <summary>
    /// Teleport the transform to the given values without interpolating
    /// </summary>
    /// <param name="newPosition"></param> new position to move to.
    /// <param name="newVelocity"></param> new velocity to move to.
    /// <exception cref="Exception"></exception>
    public void Teleport(Vector3 newPosition, Vector3 newVelocity)
    {
        if (!CanCommitToTransform)
        {
            throw new System.Exception("Teleporting on non-authoritative side is not allowed!");
        }

        // Teleporting now is as simple as setting the internal state and passing the teleport flag
        SetStateInternal(newPosition, newVelocity, true);
    }

    /// <summary>
    /// Override this method and return false to switch to owner authoritative mode
    /// </summary>
    /// <returns>(<see cref="true"/> or <see cref="false"/>) where when false it runs as owner-client authoritative</returns>
    protected virtual bool OnIsServerAuthoritative()
    {
        return true;
    }

    /// <summary>
    /// Used by <see cref="NetworkRigidbody"/> to determines if this is server or owner authoritative.
    /// </summary>
    internal bool IsServerAuthoritative()
    {
        return OnIsServerAuthoritative();
    }
}
