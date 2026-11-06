namespace Gliding.Domain.Flight
{
    /// <summary>
    /// A way of gliding: how the state changes in one small step of time while the glide is open.
    /// This is the only thing that changes between methods. The jump before it, the landing after
    /// it and everything drawn on screen are shared, so any character can glide with any method.
    ///
    /// It only gets what every method needs. A method that listens to the player (diving, for
    /// instance) receives that control in its constructor: the others do not even know it exists.
    /// </summary>
    public interface IGlideModel
    {
        /// <param name="state">The state now.</param>
        /// <param name="gravity">The world's gravity (m/s², positive). A method may use it… or not.</param>
        /// <param name="dt">Length of the step, in seconds.</param>
        /// <returns>The state dt seconds later.</returns>
        FlightState Step(FlightState state, float gravity, float dt);
    }
}
