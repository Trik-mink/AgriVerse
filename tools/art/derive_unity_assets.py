"""Create runtime derivatives without modifying the authorized source FBXs.

Run with:
  Blender --background --factory-startup \
    --python tools/art/derive_unity_assets.py -- /absolute/path/to/AgriVerse
"""

import math
import os
import sys

import bpy
from mathutils import Vector


PROJECT_ROOT = os.path.abspath(sys.argv[sys.argv.index("--") + 1])
UNITY_ART = os.path.join(
    PROJECT_ROOT, "unity", "Assets", "AgriVerse", "Art", "Environment"
)

DERIVATIVES = (
    {
        "id": "RiceClump_A",
        "source": (
            "Vegetation/Rice/RiceClump_A/Source/"
            "tripo_convert_e8561bbf-fc38-435d-8d07-e588426c24cb.fbx"
        ),
        "output": (
            "Vegetation/Rice/RiceClump_A/Derived/"
            "RiceClump_A_LOD0.fbx"
        ),
        "card": (
            "Vegetation/Rice/RiceClump_A/Derived/"
            "RiceClump_A_Card.png"
        ),
        "target_triangles": 1500,
        "physical_scale": 1.0,
    },
    {
        "id": "Grass_A",
        "source": (
            "Vegetation/Banks/Grass_A/Source/"
            "tripo_convert_e7a73c36-4b88-4f05-a995-7fdc7ccda835.fbx"
        ),
        "output": (
            "Vegetation/Banks/Grass_A/Derived/Grass_A_LOD0.fbx"
        ),
        "card": (
            "Vegetation/Banks/Grass_A/Derived/Grass_A_Card.png"
        ),
        "target_triangles": 1900,
        "physical_scale": 0.8,
        "card_cluster_planes": 8,
    },
    {
        "id": "FanPalm_A",
        "source": (
            "Vegetation/Trees/FanPalm_A/Source/"
            "tripo_convert_586e9138-0554-46ad-882a-61ff00cffd3c.fbx"
        ),
        "output": (
            "Vegetation/Trees/FanPalm_A/Derived/"
            "FanPalm_A_Optimized.fbx"
        ),
        "target_triangles": 11500,
        "physical_scale": 9.0,
    },
    {
        "id": "Boat_A",
        "source": (
            "Props/Boat_A/Source/"
            "tripo_convert_445684dd-d127-4ca1-aa99-0c51df883e29.fbx"
        ),
        "output": "Props/Boat_A/Derived/Boat_A_Optimized.fbx",
        "target_triangles": 19000,
        "physical_scale": 6.0,
    },
)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.armatures,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def import_main_mesh(source_path):
    bpy.ops.import_scene.fbx(filepath=source_path)
    meshes = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and obj.name != "Cube"
    ]
    if len(meshes) != 1:
        raise RuntimeError(
            f"Expected one authored mesh in {source_path}; found "
            f"{[obj.name for obj in meshes]}"
        )
    main = meshes[0]
    for obj in list(bpy.context.scene.objects):
        if obj != main:
            bpy.data.objects.remove(obj, do_unlink=True)
    return main


def triangle_count(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def decimate_to_budget(obj, target):
    current = triangle_count(obj)
    while current > target:
        modifier = obj.modifiers.new("AgriVerse_LOD", "DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = max(0.005, min(0.99, target / current * 0.97))
        modifier.use_collapse_triangulate = True
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        next_count = triangle_count(obj)
        if next_count >= current:
            raise RuntimeError(
                f"Decimation stalled at {current} triangles for {obj.name}"
            )
        current = next_count
    return current


def replace_with_card_cluster(source, plane_count):
    minimum, maximum = bounds(source)
    width = maximum.x - minimum.x
    depth = maximum.y - minimum.y
    height = maximum.z - minimum.z
    radius = max(width, depth) * 0.48
    vertices = []
    faces = []
    uvs = []
    for index in range(plane_count):
        angle = math.pi * index / plane_count
        direction = Vector((math.cos(angle), math.sin(angle), 0.0))
        half = direction * radius
        start = len(vertices)
        vertices.extend(
            (
                tuple(-half),
                tuple(half),
                tuple(half + Vector((0.0, 0.0, height))),
                tuple(-half + Vector((0.0, 0.0, height))),
            )
        )
        faces.extend(
            (
                (start, start + 1, start + 2),
                (start, start + 2, start + 3),
            )
        )
        uvs.extend(
            (
                (0.0, 0.0),
                (1.0, 0.0),
                (1.0, 1.0),
                (0.0, 1.0),
            )
        )

    mesh = bpy.data.meshes.new("Grass_A_LOD0_Cards")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for polygon in mesh.polygons:
        for loop_index in polygon.loop_indices:
            vertex_index = mesh.loops[loop_index].vertex_index
            uv_layer.data[loop_index].uv = uvs[vertex_index]
    result = bpy.data.objects.new("Grass_A_LOD0", mesh)
    bpy.context.scene.collection.objects.link(result)
    bpy.data.objects.remove(source, do_unlink=True)
    return result


def normalize_for_unity(obj, physical_scale):
    obj.scale *= physical_scale
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(
        location=False, rotation=False, scale=True
    )

    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    minimum = Vector(
        (
            min(point.x for point in corners),
            min(point.y for point in corners),
            min(point.z for point in corners),
        )
    )
    maximum = Vector(
        (
            max(point.x for point in corners),
            max(point.y for point in corners),
            max(point.z for point in corners),
        )
    )
    obj.location -= Vector(
        (
            (minimum.x + maximum.x) * 0.5,
            (minimum.y + maximum.y) * 0.5,
            minimum.z,
        )
    )
    bpy.ops.object.transform_apply(
        location=True, rotation=False, scale=False
    )


def export_fbx(obj, output_path):
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_NONE",
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        mesh_smooth_type="OFF",
        use_tspace=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )


def bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    minimum = Vector(
        (
            min(point.x for point in points),
            min(point.y for point in points),
            min(point.z for point in points),
        )
    )
    maximum = Vector(
        (
            max(point.x for point in points),
            max(point.y for point in points),
            max(point.z for point in points),
        )
    )
    return minimum, maximum


def render_card(obj, output_path):
    minimum, maximum = bounds(obj)
    center = (minimum + maximum) * 0.5
    width = maximum.x - minimum.x
    height = maximum.z - minimum.z

    camera_data = bpy.data.cameras.new("CardCamera")
    camera = bpy.data.objects.new("CardCamera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = Vector(
        (center.x, minimum.y - max(width, height) * 3.0, center.z)
    )
    camera.rotation_euler = (
        center - camera.location
    ).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = max(width, height) * 1.12
    camera.data.lens = 60
    bpy.context.scene.camera = camera

    key_data = bpy.data.lights.new("CardKey", "AREA")
    key_data.energy = 550
    key_data.shape = "DISK"
    key_data.size = max(width, height) * 2.5
    key = bpy.data.objects.new("CardKey", key_data)
    bpy.context.scene.collection.objects.link(key)
    key.location = center + Vector((-width, -height, height * 1.8))
    key.rotation_euler = (
        center - key.location
    ).to_track_quat("-Z", "Y").to_euler()

    fill_data = bpy.data.lights.new("CardFill", "AREA")
    fill_data.energy = 260
    fill_data.size = max(width, height) * 2.0
    fill = bpy.data.objects.new("CardFill", fill_data)
    bpy.context.scene.collection.objects.link(fill)
    fill.location = center + Vector((width, -height, height))
    fill.rotation_euler = (
        center - fill.location
    ).to_track_quat("-Z", "Y").to_euler()

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = True
    scene.render.filepath = output_path
    scene.view_settings.look = "AgX - Medium High Contrast"
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    bpy.ops.render.render(write_still=True)


def build_derivative(spec):
    clear_scene()
    source = os.path.join(UNITY_ART, spec["source"])
    output = os.path.join(UNITY_ART, spec["output"])
    obj = import_main_mesh(source)

    if "card" in spec:
        render_card(obj, os.path.join(UNITY_ART, spec["card"]))

    if spec.get("card_cluster_planes"):
        obj = replace_with_card_cluster(
            obj, spec["card_cluster_planes"]
        )
        triangles = triangle_count(obj)
    else:
        triangles = decimate_to_budget(
            obj, spec["target_triangles"]
        )
    normalize_for_unity(obj, spec["physical_scale"])
    obj.name = os.path.splitext(os.path.basename(output))[0]
    export_fbx(obj, output)
    print(
        f"AGRIVERSE_DERIVED {spec['id']} triangles={triangles} "
        f"output={output}"
    )


for derivative in DERIVATIVES:
    build_derivative(derivative)

print("AGRIVERSE_DERIVATION_COMPLETE")
