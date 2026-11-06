using UnityEngine.InputSystem;

namespace Gliding.Infrastructure
{
    /// <summary>
    /// Reads the keyboard for the demo controls:
    /// - Space: the jumps start (one after another).
    /// - R: everybody back to the ledge.
    /// - W / Up arrow: dive. S / Down arrow: pull back. Only the methods that can dive listen.
    /// - P: pause / resume.
    /// - 1, 2, 3, 4: speed x1, x0.5, x0.25, x0.1.
    /// - T: trails (and landing marks) on / off.
    /// - V: arrows (velocity and gravity) on / off.
    /// - C: next shot: side (following the character) → overview (the whole lane) → chase (behind it).
    /// - U: updrafts on / off (only the paraglider rides them).
    /// </summary>
    public static class DemoInput
    {
        /// <summary>Optional input that replaces the keyboard while it is active.</summary>
        public static IInputOverride Override { get; set; }

        static bool Overridden => Override != null && Override.Active;

        static Keyboard Keys => Keyboard.current;

        public static bool LaunchPressed =>
            Overridden ? Override.Launch : Keys != null && Keys.spaceKey.wasPressedThisFrame;

        public static bool ResetPressed =>
            Overridden ? Override.Reset : Keys != null && Keys.rKey.wasPressedThisFrame;

        /// <summary>1 while diving, -1 while pulling back, 0 hands off (or both at once).</summary>
        public static float Dive
        {
            get
            {
                if (Overridden) return Override.Dive;
                if (Keys == null) return 0f;
                float dive = 0f;
                if (Keys.wKey.isPressed || Keys.upArrowKey.isPressed) dive += 1f;
                if (Keys.sKey.isPressed || Keys.downArrowKey.isPressed) dive -= 1f;
                return dive;
            }
        }

        public static bool TogglePausePressed =>
            Overridden ? Override.TogglePause : Keys != null && Keys.pKey.wasPressedThisFrame;

        /// <summary>The speed picked this frame, or 0 if none.</summary>
        public static float TimeScalePressed
        {
            get
            {
                if (Overridden) return Override.TimeScale;
                if (Keys == null) return 0f;
                if (Keys.digit1Key.wasPressedThisFrame) return 1f;
                if (Keys.digit2Key.wasPressedThisFrame) return 0.5f;
                if (Keys.digit3Key.wasPressedThisFrame) return 0.25f;
                if (Keys.digit4Key.wasPressedThisFrame) return 0.1f;
                return 0f;
            }
        }

        public static bool ToggleTrailsPressed =>
            Overridden ? Override.ToggleTrails : Keys != null && Keys.tKey.wasPressedThisFrame;

        public static bool ToggleVectorsPressed =>
            Overridden ? Override.ToggleVectors : Keys != null && Keys.vKey.wasPressedThisFrame;

        public static bool ToggleCameraPressed =>
            Overridden ? Override.ToggleCamera : Keys != null && Keys.cKey.wasPressedThisFrame;

        public static bool ToggleUpdraftsPressed =>
            Overridden ? Override.ToggleUpdrafts : Keys != null && Keys.uKey.wasPressedThisFrame;
    }
}
