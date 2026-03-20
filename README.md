# Collider 2D Tools

<p align="center">
<img width="820" alt="SVG → COLLIDER" src="https://github.com/user-attachments/assets/0624682b-1ee2-47c3-9baf-f944d20d2c91">
</p>

Collider 2D Tools exposes two tools for working with 2D colliders in Unity. **SvgCollider2D** converts SVG shape data into Unity 2D colliders. **ColliderVisualizer2D** creates meshes for 2D colliders which enables rendering based solely on physics collider data.

## Installation

In the Unity Package Manager, select Install Package from Git URL and paste the following: `https://github.com/ashtonmeuser/collider-2d-tools.git`. Click Install.

## SvgCollider2D

`SvgCollider2D` parses SVG XML and creates colliders for supported elements (`circle`, `rect`, `polygon`, `polyline`, `line`, and `path`).

### Basic Usage

You can simply add the shapes from a static SVG asset without extending the class.

1. Add `SvgCollider2D` to a GameObject.
2. Assign an SVG `TextAsset` to **Static Svg**.
3. Press Play.

### Inspector Fields

- **Static Svg**: SVG file parsed on `Awake`.
- **Scale**: Global XY scale applied before collider creation.
- **Curve Unit Resolution**: Curve sampling interval for bezier path segments.

### Extending Behavior

In a real world project, you will likely want to modify the behavior of `SvgCollider2D`. This includes optionally skipping shapes, routing colliders to specific objects, or applying post-processing to the created collider components. Additionally, it allows parsing SVG XML content from several different data types.

Call `Walk` with a `string`, `TextAsset`, or `Stream` to begin parsing. In the example configuration using a static SVG, `Walk` is called with the SVG content in Awake.

Override these methods in a subclass:

- `GetColliderTarget`: Choose which GameObject receives each collider component. Return `null` to skip creating a collider for this shape.
- `OnColliderCreated`: Apply post-creation logic (layer/material/tag-based setup).

#### GetColliderTarget

Signature: `GameObject GetColliderTarget(SvgShapeInfo shape, IReadOnlyCollection<string> tags, IReadOnlyDictionary<string, string> attributes, string groupId)`

`GetColliderTarget` is provided with arguments to allow you to decide if and where a collider component should be routed. The following context is supplied:
- `shape`: The parsed and baked shape (`SvgCircleInfo`, `SvgRectInfo`, `SvgPolygonInfo`, or `SvgPolylineInfo`). Use this to branch by shape type and inspect geometry values like `Center`, `Radius`, `Size`, or `Points`.
- `tags`: A deduplicated collection built from SVG `id` and `class` values up the traversal path. Tags are lowercased. Note that tags pulled from `id` will have underscore + number suffixes trimmed e.g. `My_ID_1234` → `my_id`. Tags are intended to provide convenience for rule-style filtering.
- `attributes`: A read-only dictionary of the source element's attributes (for example `id`, `class`, `fill`, `stroke`, `data-*`, custom metadata). Use this when tags are not enough, or when you want fine-grained behavior based on authoring data from the SVG.
- `groupId`: The nearest parent `<g>` id for the current shape, or `null` when unavailable.

> [!NOTE]
> The collider component may not be added directly to the target Game Object. Some cases require a child Game Object containing the collider component to be added to the target Game Object. See below.

#### OnColliderCreated

Signature: `void OnColliderCreated(Collider2D collider, IReadOnlyCollection<string> tags, IReadOnlyDictionary<string, string> attributes, string groupId)`

`OnColliderCreated` is called after a shape has been parsed, a target Game Object has been determined, and a collider component has been added. Use this to perform post processing on the target Game Object or collider component. The following context is supplied:
- `collider`: The collider component added. This will, in typical cases, have been added to the target Game Object determined by `GetColliderTarget`. However, some cases require adding a child Game Object. See below.
- `tags`: Tags for the parsed shape. See `GetColliderTarget` documentation.
- `attributes`: Attributes of the parsed SVG element. See `GetColliderTarget` documentation.
- `groupId`: The nearest parent `<g>` id for the parsed shape, or `null` when unavailable.

#### Example

The following is a simple example of extending SvgCollider2D. It parses an SVG on Awake, skips any element containing the tag "ignore", and logs to the console when colliders are created.

```csharp
using Collider2DTools;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ExampleSvgCollider2D : SvgCollider2D
{
    override protected void Awake()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "level.svg");
        string contents = File.ReadAllText(path);
        Walk(contents);
    }

    override protected GameObject GetColliderTarget(SvgShapeInfo shape, IReadOnlyCollection<string> tags, IReadOnlyDictionary<string, string> attributes, string groupId)
    {
        if (tags.Contains("ignore")) return null;
        return gameObject;
    }

    override protected void OnColliderCreated(Collider2D collider, IReadOnlyCollection<string> tags, IReadOnlyDictionary<string, string> attributes, string groupId)
    {
        Debug.Log($"Created collider for shape {collider.GetType().Name} in group '{groupId}' with tags [{string.Join(", ", tags)}]");
    }
}
```

#### When A Child Game Object Is Needed

In simple cases, a collider is added directly to the Game Object returned by `GetColliderTarget`. However, some cases require an intermediate child Game Object that contains the collider component.

When an axis-aligned shape with a rotational transform is parsed from the SVG, it must be given a bespoke transform. This is because simply applying an offset will not accurately model the parsed shape and applying a transform to the parent Game Object will affect all collider components attached to it. For example, an SVG `rect` element is parsed and a BoxCollider2D component is created as its representation. If the SVG element has a rotation transform applied, a child Game Object is added to the target, the transform is applied, and the collider component is added to the child Game Object.

For circles, polygons, and polylines, geometry is baked i.e. transform data including scale, translation, and rotation is parsed and applied to the collider geometry directly, thereby precluding the need for a child Game Object.

## ColliderVisualizer2D

`ColliderVisualizer2D` builds a mesh visualization from `Collider2D` components and draws edge colliders by copying a `LineRenderer` component.

### Basic Usage

1. Add `ColliderVisualizer2D` to a GameObject. `MeshFilter`, `MeshRenderer`, and `LineRenderer` components should be added automatically.
2. Set **Root** to the object containing generated colliders (optional, defaults to current object).
3. Configure your renderers.
4. Press Play to generate the visualization mesh.

### Inspector Fields

- **Root**: Collider search root.
- **Line Renderer**: Prefab used for edge/polyline visualization.

## Notes

- `SvgCollider2D` executes early (`DefaultExecutionOrder(-100)`) so colliders exist before most consumer scripts.
- `ColliderVisualizer2D` executes late (`DefaultExecutionOrder(100)`) so it can visualize colliders added at runtime.
