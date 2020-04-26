using Godot;
using System;
using System.Collections;

#if TOOLS

[Tool]
public class PathGizmoPlugin : EditorSpatialGizmoPlugin {
    public EditorInterface editor;
    public CustomPathEditorPlugin plugin;

    public PathGizmoPlugin() {

    }

    public PathGizmoPlugin(CustomPathEditorPlugin plugin, EditorInterface editor) {
        this.editor = editor;
        this.plugin = plugin;
        object pathCol = editor.GetEditorSettings().GetSetting("editors/3d_gizmos/gizmo_colors/path");
        Color pathColor = new Color(0.5f,0.5f,1.0f,0.8f);
        if(pathCol is Color col) {
            pathColor = col;
        }
        CreateMaterial("path_material", pathColor);
        CreateMaterial("path_thin_material",new Color(pathColor.r,pathColor.g,pathColor.b,0.5f));
        CreateHandleMaterial("handles");
    }

    public override EditorSpatialGizmo CreateGizmo(Spatial spatial) {
        GD.Print("Create Gizmo");
        if(spatial is CustomPath pth) {
            plugin.path = pth;
            return new PathGizmo(plugin,this,editor, pth);
        }
        return null;
    }

    public override string GetName() {
        return "CustomPath";
    }

    public override string GetPriority() {
        return "-1";
    }
}

#endif