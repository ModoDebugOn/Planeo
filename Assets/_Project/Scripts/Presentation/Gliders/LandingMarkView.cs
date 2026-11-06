using UnityEngine;

namespace Gliding.Presentation.Gliders
{
    /// <summary>
    /// Where a glider landed: a disc on the ground and a small sign in the method's colour with the
    /// distance flown (the Z of the landing: the lane starts at the edge of the ledge). It stays
    /// until that glider jumps again, so the marks of every method can be compared.
    /// </summary>
    public sealed class LandingMarkView : MonoBehaviour
    {
        [SerializeField, Range(0.2f, 3f)] float discRadius = 1.2f;
        [SerializeField, Range(1f, 10f)] float postHeight = 3.5f;
        [SerializeField] Color textColor = new Color(0.98f, 0.96f, 0.9f);

        System.Func<Vector3?> source;
        GameObject mark;
        TextMesh label;
        Vector3? shown;

        /// <summary>The landing spot (feet), or null while it has not landed.</summary>
        public void Bind(System.Func<Vector3?> end) => source = end;

        public void Configure(Material material, Color color)
        {
            mark = new GameObject("Mark");
            mark.transform.SetParent(transform, false);
            var block = new MaterialPropertyBlock();
            block.SetColor(Shader.PropertyToID("_BaseColor"), color);

            Part(PrimitiveType.Cylinder, "Disc", material, block, new Vector3(0f, 0.03f, 0f), new Vector3(discRadius * 2f, 0.03f, discRadius * 2f));
            Part(PrimitiveType.Cube, "Post", material, block, new Vector3(0f, postHeight / 2f, 0f), new Vector3(0.12f, postHeight, 0.12f));
            Part(PrimitiveType.Cube, "Board", material, block, new Vector3(0f, postHeight + 0.8f, 0f), new Vector3(0.1f, 1.6f, 4.2f));

            // On the +X face of the board, facing the side camera, reading towards +Z.
            label = new GameObject("Label").AddComponent<TextMesh>();
            label.transform.SetParent(mark.transform, false);
            label.transform.localPosition = new Vector3(0.06f, postHeight + 0.8f, 0f);
            label.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            label.fontSize = 96;
            label.characterSize = 0.13f;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = textColor;
            mark.SetActive(false);
        }

        void Part(PrimitiveType type, string name, Material material, MaterialPropertyBlock block, Vector3 position, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(type);
            Destroy(part.GetComponent<Collider>());
            part.name = name;
            part.transform.SetParent(mark.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.SetPropertyBlock(block);
        }

        void LateUpdate()
        {
            if (mark == null) return;
            var end = source?.Invoke();
            if (end == shown) return;
            shown = end;
            mark.SetActive(end.HasValue);
            if (!end.HasValue) return;
            mark.transform.position = end.Value;
            label.text = $"{end.Value.z:0} m";
        }
    }
}
