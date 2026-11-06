using Gliding.Domain.Flight;
using UnityEngine;

namespace Gliding.Infrastructure.Profiles
{
    /// <summary>
    /// A method with its numbers, as an asset: the sliders of the video live here. Changing them
    /// in Play mode changes the glide on the spot (the composition root rebuilds the model).
    /// </summary>
    public abstract class GlideProfile : ScriptableObject
    {
        [Tooltip("Name shown on screen (in Spanish).")]
        [SerializeField] string displayName = "Método";
        [Tooltip("Colour of its trail and its arrows.")]
        [SerializeField] Color color = Color.white;

        [Header("Stamina (any method can tire)")]
        [Tooltip("On: gliding spends stamina and, when it runs out, the character lets go and falls (Zelda). " +
                 "Off: it glides until it lands.")]
        [SerializeField] bool tires;
        [Tooltip("Seconds of glide a full stamina wheel lasts.")]
        [SerializeField, Range(1f, 30f)] float enduranceSeconds = 7f;

        public string DisplayName => displayName;
        public Color Color => color;

        /// <summary>Goes up every time a value changes in the inspector.</summary>
        public int Version { get; private set; }

        public abstract IGlideModel CreateModel();

        /// <summary>A full stamina bar for the character, or null if this profile never tires.</summary>
        public Stamina CreateStamina() => tires ? new Stamina(enduranceSeconds) : null;

        protected virtual void OnValidate() => Version++;
    }
}
