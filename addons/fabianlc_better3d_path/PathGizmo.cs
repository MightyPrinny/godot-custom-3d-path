using Godot;
using System;
using System.Collections.Generic;
using FLCCustom3DPath;

#if TOOLS

[Tool]
public class PathGizmo : EditorSpatialGizmo {
    public CustomPathEditorPlugin plugin;
    public CustomPath path;
    public EditorInterface editor;
    public PathGizmoPlugin gizmoPlugin;
    public Vector3 original;
    public float origInLength;
    public float origOutLenght;

    public PathGizmo() {}

    public PathGizmo(CustomPathEditorPlugin plugin,PathGizmoPlugin gizmoPlug, EditorInterface editor, CustomPath path) {
        this.editor = editor;
        this.path = path;
        this.plugin = plugin;
        gizmoPlugin = gizmoPlug;
    }

    public override string GetHandleName(int index) {
        Curve3D c = path.curve;
        if (!IsInstanceValid(c)) {
            GD.Print("Invalid Curve");
            return "";
        }

        if (index < c.GetPointCount()) {

            return ("Curve Point #") + index;
        }

        index = index - c.GetPointCount() + 1;

        int idx = index / 2;
        int t = index % 2;
        String n = ("Curve Point #") + idx;
        if (t == 0) {
            n += " In";
        }
        else {
            n += " Out";
        }

        return n;
    }

    public override object GetHandleValue(int index) {
        Curve3D c = path.curve;
        if (!IsInstanceValid(c)) {
            GD.Print("Invalid curve");
            return null;
        }

        if (index < c.GetPointCount()) {
            original = c.GetPointPosition(index);
            return original;
        }

        index = index - c.GetPointCount() + 1;

        int idx = index / 2;
        int t = index % 2;

        Vector3 ofs;
        if (t == 0) {
            ofs = c.GetPointIn(idx);
        }
        else {
            ofs = c.GetPointOut(idx);
        }

        original = ofs + c.GetPointPosition(idx);

        return ofs;
    }

    public override void SetHandle(int index, Camera camera, Vector2 point) {
        Curve3D c = path.curve;
        if (!IsInstanceValid(c)) {
            GD.Print("Invalid curve");
            return;
        }

        Transform gt = path.GlobalTransform;
        Transform gi = gt.AffineInverse();
        Vector3 ray_from = camera.ProjectRayOrigin(point);
        Vector3 ray_dir = camera.ProjectRayNormal(point);

        // Setting curve point positions
        if (index < c.GetPointCount()) {
            var org = gt.Xform(original);
            Plane pp = new Plane();
            pp.Normal = camera.Transform.basis.GetColumn(2);
            pp.D = pp.Normal.Dot(org);

            Vector3? _inters = pp.IntersectRay(ray_from, ray_dir);

            if (_inters != null) {
                Vector3 _nnInters = (Vector3) _inters;
                if (plugin.snapEnabled) {
                    _nnInters = _nnInters.Snapped(new Vector3(plugin.snapLength,plugin.snapLength,plugin.snapLength));
                }

                _nnInters = plugin.RestrictPoint(org, _nnInters);

                Vector3 local = gi.Xform(_nnInters);
                c.SetPointPosition(index, local);
            }
            return;
        }

        index = index - c.GetPointCount() + 1;

        int idx = index / 2;
        int t = index % 2;

        var porig = gt.Xform(original);
        Vector3 basePos = c.GetPointPosition(idx);

        Plane p = new Plane();
        p.Normal = camera.Transform.basis.GetColumn(2);
        p.D = p.Normal.Dot(porig);

        Vector3? inters = p.IntersectRay(ray_from, ray_dir);

        // Setting curve in/out positions
        if (inters != null) {
            Vector3 nnInters = (Vector3)inters;
            if (!plugin.handleClicked) {
                origInLength = c.GetPointIn(idx).Length();
                origOutLenght = c.GetPointOut(idx).Length();
                plugin.SetHandleClicked(true);
            }
            Vector3 orig = gi.Xform(porig) - basePos;
            Vector3 local = gi.Xform(nnInters) - basePos;
            if (plugin.snapEnabled) {
                local = local.Snapped(new Vector3(plugin.snapLength,plugin.snapLength,plugin.snapLength));
            }

            local = plugin.RestrictPoint(orig,local);

            if (t == 0) {
                c.SetPointIn(idx, local);
                if (plugin.mirrorHandleAngle)
                    c.SetPointOut(idx, plugin.mirrorHandleLength ? -local : (-local.Normalized() * origOutLenght));
            } else {
                c.SetPointOut(idx, local);
                if (plugin.mirrorHandleAngle)
                    c.SetPointIn(idx, plugin.mirrorHandleLength ? -local : (-local.Normalized() * origInLength));
            }
        }
    }

    public override void CommitHandle(int index, object restore, bool cancel) {
        Curve3D c = path.curve;
        if (!IsInstanceValid(c)) {
            return;
        }

        UndoRedo ur = plugin.GetUndoRedo();

        if (index < c.GetPointCount()) {
            if (cancel) {
                c.SetPointPosition(index, (Vector3)restore);
                return;
            }
            ur.CreateAction(("Set Curve Point Position"));
            ur.AddDoMethod(c, "set_point_position", index, c.GetPointPosition(index));
            ur.AddUndoMethod(c, "set_point_position", index, restore);
            ur.CommitAction();

            return;
        }

        index = index - c.GetPointCount() + 1;

        int idx = index / 2;
        int t = index % 2;

        if (t == 0) {
            if (cancel) {
                c.SetPointIn(index, (Vector3)restore);
                return;
            }

            ur.CreateAction(("Set Curve In Position"));
            ur.AddDoMethod(c, "set_point_in", idx, c.GetPointIn(idx));
            ur.AddUndoMethod(c, "set_point_in", idx, restore);

            if (plugin.mirrorHandleAngle) {
                ur.AddDoMethod(c, "set_point_out", idx, plugin.mirrorHandleLength ? -c.GetPointIn(idx) : (-c.GetPointIn(idx).Normalized() * origOutLenght));
                ur.AddUndoMethod(c, "set_point_out", idx, plugin.mirrorHandleLength ? -(Vector3)restore : (-((Vector3)restore).Normalized() * origOutLenght));
            }
            ur.CommitAction();

        } else {
            if (cancel) {
                c.SetPointOut(idx, (Vector3)restore);
                return;
            }

            ur.CreateAction(("Set Curve Out Position"));
            ur.AddDoMethod(c, "set_point_out", idx, c.GetPointOut(idx));
            ur.AddUndoMethod(c, "set_point_out", idx, restore);

            if (plugin.mirrorHandleAngle) {
                ur.AddDoMethod(c, "set_point_in", idx, plugin.mirrorHandleLength ? -c.GetPointOut(idx) : (-c.GetPointOut(idx).Normalized() * origInLength));
                ur.AddUndoMethod(c, "set_point_in", idx, plugin.mirrorHandleLength ? -(Vector3)(restore) : (-((Vector3)restore).Normalized() * origInLength));
            }
            ur.CommitAction();
        }
    }

    public override void Redraw( ) {
        Clear();
        //GD.Print($"Redraw {GD.Randi()%100}");
        var pathMaterial = gizmoPlugin.GetMaterial("path_material", this);
        var pathThinMaterial = gizmoPlugin.GetMaterial("path_thin_material", this);
        var handlesMaterial = gizmoPlugin.GetMaterial("handles",this);
        Curve3D c = path.curve;
        if (!IsInstanceValid(c)) {
            GD.Print("Invalid curve");
            return;
        }

        var v3a = c.Tessellate();
        //PoolVector<Vector3> v3a=c.get_baked_points();
        
        int v3s = v3a.Length;
        if (v3s == 0)
            return;
        var v3p = new Vector3[v3s*2];
        int v3pCount = 0;

        // BUG: the following won't work when v3s, avoid drawing as a temporary workaround.
        for (int i = 0; i < v3s - 1; i++) {
            v3p = v3p.Add(ref v3pCount, v3a[i]);
            v3p = v3p.Add(ref v3pCount, v3a[i + 1]);
            //v3p.push_back(r[i]);
            //v3p.push_back(r[i]+Vector3(0,0.2,0));
        }
        
        if (v3pCount > 1) {
            v3p = v3p.Trim(ref v3pCount);
            AddLines((Vector3[])v3p.Clone(), pathMaterial);
            AddCollisionSegments((Vector3[])v3p.Clone());
        }

        if (plugin.path == path) {
            v3p = v3p.Clear(ref v3pCount,true);
            int pointCount = c.GetPointCount();
            var handles = new Vector3[pointCount];
            int handlesCount = 0;
            var secHandles = new Vector3[pointCount];
            int secHandlesCount = 0;
            
            for (int i = 0; i < pointCount; i++) {

                Vector3 p = c.GetPointPosition(i);
                handles = handles.Add(ref handlesCount, p);
                if (i > 0) {
                    v3p = v3p.Add(ref v3pCount, p);
                    v3p = v3p.Add(ref v3pCount, p + c.GetPointIn(i));
                    secHandles = secHandles.Add(ref secHandlesCount, p + c.GetPointIn(i));
                }

                if (i < pointCount - 1) {
                    v3p = v3p.Add(ref v3pCount, p);
                    v3p = v3p.Add(ref v3pCount, p + c.GetPointOut(i));
                    secHandles = secHandles.Add(ref secHandlesCount, p + c.GetPointOut(i));
                }
            }

            v3p = v3p.Trim(ref v3pCount);
            handles = handles.Trim(ref handlesCount);
            secHandles = secHandles.Trim(ref secHandlesCount);

            if (v3pCount > 1) {
                AddLines(v3p, pathThinMaterial);
            }
            if (handlesCount > 0) {
               
                AddHandles(handles, handlesMaterial);
            }
            if (secHandlesCount > 0) {
                AddHandles(secHandles, handlesMaterial, false, true);
            }
        }
    }
}

#endif