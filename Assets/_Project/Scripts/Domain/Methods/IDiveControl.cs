namespace Gliding.Domain.Methods
{
    /// <summary>
    /// The player's hand on the dive: only the methods that can dive receive it (in their
    /// constructor). Who reads the keyboard, a recorded take or a test decides where it comes from.
    /// </summary>
    public interface IDiveControl
    {
        /// <summary>1 = dive (nose down) … 0 = normal glide … -1 = pull up (nose up).</summary>
        float Dive { get; }
    }

    /// <summary>A dive control whose position is simply set from outside (0 = hands off).</summary>
    public sealed class DiveStick : IDiveControl
    {
        public float Dive { get; set; }
    }
}
