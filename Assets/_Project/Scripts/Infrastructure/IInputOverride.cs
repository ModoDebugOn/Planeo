namespace Gliding.Infrastructure
{
    /// <summary>
    /// Another source of input that can take over from the keyboard: the recording kit, a test...
    /// While it is Active, <see cref="DemoInput"/> ignores the real devices.
    /// Nothing in the demo uses it; it is there so other tools can drive the demo.
    /// </summary>
    public interface IInputOverride
    {
        bool Active { get; }

        /// <summary>True on the frame everybody should jump (from the start).</summary>
        bool Launch { get; }

        /// <summary>True on the frame everybody should go back to the ledge.</summary>
        bool Reset { get; }

        /// <summary>1 dive … 0 hands off … -1 pull back (only the methods that can dive listen).</summary>
        float Dive { get; }

        /// <summary>True on the frame the pause should toggle.</summary>
        bool TogglePause { get; }

        /// <summary>The time scale to switch to this frame, or 0 to keep the current one.</summary>
        float TimeScale { get; }

        /// <summary>True on the frame the trails should switch on or off.</summary>
        bool ToggleTrails { get; }

        /// <summary>True on the frame the velocity arrows should switch on or off.</summary>
        bool ToggleVectors { get; }

        /// <summary>True on the frame the camera should go to the next shot (side → overview → chase).</summary>
        bool ToggleCamera { get; }

        /// <summary>True on the frame the updrafts should switch on or off.</summary>
        bool ToggleUpdrafts { get; }
    }
}
