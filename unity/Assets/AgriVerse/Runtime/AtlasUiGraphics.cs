using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    internal enum AtlasSurfaceKind
    {
        FieldPaper,
        SmokedGlass,
        AtlasLabel
    }

    internal enum AtlasDecorationKind
    {
        PaperGrain,
        Contours,
        SurveyRule
    }

    /// <summary>
    /// A code-native clipped surface. It keeps the atlas presentation independent from
    /// flattened UI images while giving paper, temporary glass, and survey labels distinct
    /// silhouettes.
    /// </summary>
    internal sealed class AtlasSurfaceGraphic : MaskableGraphic
    {
        [SerializeField] private AtlasSurfaceKind surfaceKind;
        [SerializeField] private float cornerCut;

        internal AtlasSurfaceKind SurfaceKind => surfaceKind;
        internal float CornerCut => cornerCut;

        internal void Configure(
            AtlasSurfaceKind kind,
            Color tint,
            float cut)
        {
            surfaceKind = kind;
            color = tint;
            cornerCut = Mathf.Max(0f, cut);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = rectTransform.rect;
            float cut = Mathf.Min(
                cornerCut,
                Mathf.Min(rect.width, rect.height) * .24f);
            Vector2[] points =
            {
                new Vector2(rect.xMin + cut, rect.yMin),
                new Vector2(rect.xMax - cut, rect.yMin),
                new Vector2(rect.xMax, rect.yMin + cut),
                new Vector2(rect.xMax, rect.yMax - cut),
                new Vector2(rect.xMax - cut, rect.yMax),
                new Vector2(rect.xMin + cut, rect.yMax),
                new Vector2(rect.xMin, rect.yMax - cut),
                new Vector2(rect.xMin, rect.yMin + cut)
            };
            UIVertex center = UIVertex.simpleVert;
            center.position = rect.center;
            center.color = color;
            helper.AddVert(center);
            for (int index = 0; index < points.Length; index++)
            {
                UIVertex vertex = UIVertex.simpleVert;
                vertex.position = points[index];
                vertex.color = color;
                helper.AddVert(vertex);
            }
            for (int index = 0; index < points.Length; index++)
            {
                helper.AddTriangle(
                    0,
                    index + 1,
                    ((index + 1) % points.Length) + 1);
            }
        }
    }

    /// <summary>
    /// Lightweight procedural paper grain, contour marks, and survey rules. The geometry is
    /// intentionally sparse so it remains restrained and cheap in overlay canvases.
    /// </summary>
    internal sealed class AtlasDecorationGraphic : MaskableGraphic
    {
        [SerializeField] private AtlasDecorationKind decorationKind;

        internal void Configure(
            AtlasDecorationKind kind,
            Color tint)
        {
            decorationKind = kind;
            color = tint;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = rectTransform.rect;
            switch (decorationKind)
            {
                case AtlasDecorationKind.PaperGrain:
                    AddPaperGrain(helper, rect);
                    break;
                case AtlasDecorationKind.Contours:
                    AddContours(helper, rect);
                    break;
                default:
                    AddSurveyRule(helper, rect);
                    break;
            }
        }

        private void AddPaperGrain(
            VertexHelper helper,
            Rect rect)
        {
            for (int index = 0; index < 18; index++)
            {
                float x = Mathf.Lerp(
                    rect.xMin,
                    rect.xMax,
                    Mathf.Repeat(index * .381966f, 1f));
                float y = Mathf.Lerp(
                    rect.yMin,
                    rect.yMax,
                    Mathf.Repeat(index * .618034f, 1f));
                float length = 4f + index % 5 * 2f;
                AddQuad(
                    helper,
                    new Vector2(x, y),
                    new Vector2(x + length, y + (index % 2 == 0 ? .5f : -.5f)),
                    .45f);
            }
        }

        private void AddContours(
            VertexHelper helper,
            Rect rect)
        {
            Vector2 center = new Vector2(
                rect.xMax - rect.width * .13f,
                rect.yMax - rect.height * .22f);
            for (int ring = 0; ring < 3; ring++)
            {
                float radiusX = 15f + ring * 9f;
                float radiusY = 7f + ring * 5f;
                Vector2 previous = center +
                                   new Vector2(radiusX, 0f);
                for (int segment = 1; segment <= 12; segment++)
                {
                    float angle =
                        segment / 12f * Mathf.PI * 2f;
                    Vector2 next = center + new Vector2(
                        Mathf.Cos(angle) * radiusX,
                        Mathf.Sin(angle) * radiusY);
                    AddQuad(helper, previous, next, .5f);
                    previous = next;
                }
            }
        }

        private void AddSurveyRule(
            VertexHelper helper,
            Rect rect)
        {
            float y = rect.yMin + 1.5f;
            AddQuad(
                helper,
                new Vector2(rect.xMin, y),
                new Vector2(rect.xMax, y),
                .65f);
            for (int index = 0; index <= 8; index++)
            {
                float x = Mathf.Lerp(
                    rect.xMin,
                    rect.xMax,
                    index / 8f);
                AddQuad(
                    helper,
                    new Vector2(x, y),
                    new Vector2(
                        x,
                        y + (index % 2 == 0 ? 5f : 3f)),
                    .55f);
            }
        }

        private void AddQuad(
            VertexHelper helper,
            Vector2 start,
            Vector2 end,
            float halfWidth)
        {
            Vector2 direction = end - start;
            if (direction.sqrMagnitude < .001f) return;
            Vector2 normal =
                new Vector2(-direction.y, direction.x)
                    .normalized * halfWidth;
            int baseIndex = helper.currentVertCount;
            AddVertex(helper, start - normal);
            AddVertex(helper, start + normal);
            AddVertex(helper, end + normal);
            AddVertex(helper, end - normal);
            helper.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
            helper.AddTriangle(baseIndex + 2, baseIndex + 3, baseIndex);
        }

        private void AddVertex(
            VertexHelper helper,
            Vector2 position)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            helper.AddVert(vertex);
        }
    }

    internal sealed class AtlasInstrumentGraphic : MaskableGraphic
    {
        [SerializeField, Range(0f, 1f)]
        private float measuredNormalized;
        [SerializeField] private int tickCount = 7;

        internal float MeasuredNormalized => measuredNormalized;
        internal int TickCount => tickCount;

        internal void SetMeasurement(float normalized)
        {
            measuredNormalized = Mathf.Clamp01(normalized);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = rectTransform.rect;
            float stemX = rect.xMin + rect.width * .54f;
            float bottom = rect.yMin + 18f;
            float top = rect.yMax - 18f;
            AddLine(
                helper,
                new Vector2(stemX, bottom),
                new Vector2(stemX, top),
                1.2f,
                EpisodeUiFactory.OffWhite);
            for (int index = 0; index < tickCount; index++)
            {
                float amount =
                    index / (float)(tickCount - 1);
                float y = Mathf.Lerp(bottom, top, amount);
                float length = index % 2 == 0 ? 15f : 9f;
                AddLine(
                    helper,
                    new Vector2(stemX - length, y),
                    new Vector2(stemX, y),
                    .8f,
                    new Color(
                        EpisodeUiFactory.OffWhite.r,
                        EpisodeUiFactory.OffWhite.g,
                        EpisodeUiFactory.OffWhite.b,
                        .82f));
            }

            float measuredY =
                Mathf.Lerp(bottom, top, measuredNormalized);
            Color water = new Color(
                EpisodeUiFactory.NetworkTeal.r,
                EpisodeUiFactory.NetworkTeal.g,
                EpisodeUiFactory.NetworkTeal.b,
                .84f);
            AddFilledRect(
                helper,
                new Rect(
                    stemX + 6f,
                    bottom,
                    Mathf.Max(10f, rect.width * .16f),
                    Mathf.Max(2f, measuredY - bottom)),
                water);
            AddLine(
                helper,
                new Vector2(stemX - 23f, measuredY),
                new Vector2(stemX + 27f, measuredY),
                1.8f,
                EpisodeUiFactory.BrightAmber);
        }

        private static void AddFilledRect(
            VertexHelper helper,
            Rect rect,
            Color tint)
        {
            int start = helper.currentVertCount;
            AddVertex(helper, new Vector2(rect.xMin, rect.yMin), tint);
            AddVertex(helper, new Vector2(rect.xMin, rect.yMax), tint);
            AddVertex(helper, new Vector2(rect.xMax, rect.yMax), tint);
            AddVertex(helper, new Vector2(rect.xMax, rect.yMin), tint);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start + 2, start + 3, start);
        }

        private static void AddLine(
            VertexHelper helper,
            Vector2 start,
            Vector2 end,
            float halfWidth,
            Color tint)
        {
            Vector2 direction = end - start;
            if (direction.sqrMagnitude < .001f) return;
            Vector2 normal =
                new Vector2(-direction.y, direction.x)
                    .normalized * halfWidth;
            int baseIndex = helper.currentVertCount;
            AddVertex(helper, start - normal, tint);
            AddVertex(helper, start + normal, tint);
            AddVertex(helper, end + normal, tint);
            AddVertex(helper, end - normal, tint);
            helper.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
            helper.AddTriangle(baseIndex + 2, baseIndex + 3, baseIndex);
        }

        private static void AddVertex(
            VertexHelper helper,
            Vector2 position,
            Color tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = tint;
            helper.AddVert(vertex);
        }
    }

    internal sealed class AtlasRouteGraphic : MaskableGraphic
    {
        [SerializeField] private Vector2[] normalizedNodes =
            System.Array.Empty<Vector2>();
        [SerializeField] private float lineWidth = 2f;

        internal int NodeCount =>
            normalizedNodes?.Length ?? 0;

        internal void SetRoute(
            Vector2[] nodes,
            Color tint,
            float width = 2f)
        {
            normalizedNodes =
                nodes == null
                    ? System.Array.Empty<Vector2>()
                    : (Vector2[])nodes.Clone();
            color = tint;
            lineWidth = Mathf.Max(.5f, width);
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            if (normalizedNodes == null ||
                normalizedNodes.Length == 0)
            {
                return;
            }
            Rect rect = rectTransform.rect;
            Vector2 previous = Position(rect, normalizedNodes[0]);
            AddNode(helper, previous, 4.5f);
            for (int index = 1;
                 index < normalizedNodes.Length;
                 index++)
            {
                Vector2 next =
                    Position(rect, normalizedNodes[index]);
                AddLine(helper, previous, next, lineWidth);
                AddNode(helper, next, 4.5f);
                previous = next;
            }
        }

        private Vector2 Position(Rect rect, Vector2 normalized) =>
            new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalized.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalized.y));

        private void AddNode(
            VertexHelper helper,
            Vector2 center,
            float radius)
        {
            int centerIndex = helper.currentVertCount;
            AddVertex(helper, center);
            for (int index = 0; index < 8; index++)
            {
                float angle = index / 8f * Mathf.PI * 2f;
                AddVertex(
                    helper,
                    center +
                    new Vector2(
                        Mathf.Cos(angle),
                        Mathf.Sin(angle)) * radius);
            }
            for (int index = 0; index < 8; index++)
            {
                helper.AddTriangle(
                    centerIndex,
                    centerIndex + index + 1,
                    centerIndex + ((index + 1) % 8) + 1);
            }
        }

        private void AddLine(
            VertexHelper helper,
            Vector2 start,
            Vector2 end,
            float halfWidth)
        {
            Vector2 direction = end - start;
            if (direction.sqrMagnitude < .001f) return;
            Vector2 normal =
                new Vector2(-direction.y, direction.x)
                    .normalized * halfWidth;
            int baseIndex = helper.currentVertCount;
            AddVertex(helper, start - normal);
            AddVertex(helper, start + normal);
            AddVertex(helper, end + normal);
            AddVertex(helper, end - normal);
            helper.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
            helper.AddTriangle(baseIndex + 2, baseIndex + 3, baseIndex);
        }

        private void AddVertex(
            VertexHelper helper,
            Vector2 position)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            helper.AddVert(vertex);
        }
    }
}
