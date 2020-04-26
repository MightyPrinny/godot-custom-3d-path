 using Godot;
using System;
#if TOOLS

[Tool]
public class CustomPathEditorPlugin : EditorPlugin {
    public Separator sep;
    public ToolButton curveCreate;
    public ToolButton curveDel;
    public ToolButton curveClose;
    public ToolButton curveEdit;
    public MenuButton handleMenu;

    public EditorInterface editor;
    public PathGizmoPlugin gizmoPlugin;
    public Control baseControl;

    public CustomPath path;

    public bool handleClicked;
    public bool mirrorHandle;
    public bool mirrorHandleAngle;
    public bool mirrorHandleLength;
    public bool snapEnabled;
    public float snapLength = 1;
    public bool lockX;
    public bool lockY;
    public bool lockZ;

    public enum Options {
        Angle,
        Length,
        DoSnap,
        ConfigureSnap,
        LockX,
        LockY,
        LockZ
    }

    public override void _EnterTree() {
        path = null;
        editor = GetEditorInterface();
        baseControl = editor.GetBaseControl();
        AddCustomType("Custom3DPath","Spatial",GD.Load("res://addons/fabianlc_better3d_path/CustomPath.cs") as Script
        ,GD.Load("res://addons/fabianlc_better3d_path/Path3D.svg") as Texture);
        mirrorHandleAngle = true;
        mirrorHandleLength = true;

        gizmoPlugin = new PathGizmoPlugin(this,editor);
        AddSpatialGizmoPlugin(gizmoPlugin);

        sep = new VSeparator();
        sep.Hide();
        AddControlToContainer(CustomControlContainer.SpatialEditorMenu,sep);
        curveEdit = new ToolButton();
        curveEdit.Icon = baseControl.GetIcon("CurveEdit", "EditorIcons");
        curveEdit.ToggleMode = true;
        curveEdit.Hide();
        curveEdit.FocusMode = Control.FocusModeEnum.None;
        curveEdit.HintTooltip = (("Select Points") + "\n" + ("Shift+Drag: Select Control Points") 
        + "\n" + "KEY_CMD" + ("Click: Add Point") + "\n" + ("Right Click: Delete Point"));
        AddControlToContainer(CustomControlContainer.SpatialEditorMenu,curveEdit);
        curveCreate = new ToolButton();
        curveCreate.Icon = baseControl.GetIcon("CurveCreate", "EditorIcons");
        curveCreate.ToggleMode = true;
        curveCreate.Hide();
        curveCreate.FocusMode = Control.FocusModeEnum.None;;
        curveCreate.HintTooltip = (("Add Point (in empty space)") + "\n" + ("Split Segment (in curve)"));
        AddControlToContainer(CustomControlContainer.SpatialEditorMenu,curveCreate);
        curveDel = new ToolButton();
        curveDel.Icon = baseControl.GetIcon("CurveDelete", "EditorIcons");
        curveDel.ToggleMode = true;
        curveDel.Hide();
        curveDel.FocusMode = Control.FocusModeEnum.None;;
        curveDel.HintTooltip = (("Delete Point"));
        AddControlToContainer(CustomControlContainer.SpatialEditorMenu,curveDel);
        curveClose = new ToolButton();
        curveClose.Icon = baseControl.GetIcon("CurveClose", "EditorIcons");
        curveClose.Hide();
        curveClose.FocusMode = Control.FocusModeEnum.None;;
        curveClose.HintTooltip = (("Close Curve"));
        AddControlToContainer(CustomControlContainer.SpatialEditorMenu,curveClose);

        PopupMenu menu;

        handleMenu = new MenuButton();
        handleMenu.Text = (("Options"));
        handleMenu.Hide();
        AddControlToContainer(CustomControlContainer.SpatialEditorMenu,handleMenu);

        menu = handleMenu.GetPopup();

        menu.AddCheckItem("Mirror Handle Angles");
        menu.SetItemChecked((int)Options.Angle, mirrorHandleAngle);
        menu.AddCheckItem("Mirror Handle Lengths");
        menu.SetItemChecked((int)Options.Length, mirrorHandleLength);
        menu.AddCheckItem("Enable Snap", (int)Options.DoSnap);
        menu.SetItemChecked((int)Options.DoSnap, snapEnabled);
        menu.AddItem("Configure snap", (int)Options.ConfigureSnap);
        menu.AddCheckItem("Lock X", (int)Options.LockX);
        menu.SetItemChecked((int)Options.LockX,lockX);
        menu.AddCheckItem("Lock Y", (int)Options.LockY);
        menu.SetItemChecked((int)Options.LockY,lockY);
        menu.AddCheckItem("Lock Z", (int)Options.LockZ);
        menu.SetItemChecked((int)Options.LockZ, lockZ);

        menu.Connect("id_pressed", this, nameof(HandleOptionPressed));

        curveEdit.Pressed = (true);

        curveCreate.Connect("pressed", this, "_ModeChanged", new Godot.Collections.Array{0});
        curveEdit.Connect("pressed", this, "_ModeChanged", new Godot.Collections.Array{1});
        curveDel.Connect("pressed", this, "_ModeChanged", new Godot.Collections.Array{2});
        curveClose.Connect("pressed", this, "_CloseCurve");
    }

    public override void _ExitTree() {
        RemoveCustomType("Custom3DPath");
        path = null;
        handleMenu.QueueFree();
        curveClose.QueueFree();
        curveDel.QueueFree();
        curveCreate.QueueFree();
        curveEdit.QueueFree();
        RemoveSpatialGizmoPlugin(gizmoPlugin);
        gizmoPlugin = null;
        sep.QueueFree();
    }

    public void OpenPopup() {
        var popup = new AcceptDialog();
        popup.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
        popup.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;
        popup.PopupExclusive = true;
        popup.WindowTitle = "Configure snap";
        var container = new VBoxContainer();
        var snapSpin = new SpinBox();
        snapSpin.MinValue = 0;
        snapSpin.MaxValue = 99;
        snapSpin.Step = 0.0001f;
        snapSpin.Value = snapLength;
        snapSpin.Connect("value_changed", this, "SetSnapLength");
        var snapHBox = new HBoxContainer();
        var snapLabel = new Label();
        snapLabel.Text = "Snap length";
        snapHBox.AddChild(snapLabel);
        snapHBox.AddChild(snapSpin);
        container.AddChild(snapHBox);
        popup.AddChild(container);
        
        popup.Connect("popup_hide", popup, "queue_free");
        baseControl.AddChild(popup);
        
        popup.PopupCentered(new Vector2(200,100));
    }

    public void SetSnapLength(float value) {
        snapLength = value;
    }

    public override void _Ready() {

    }

    public override string GetPluginName() {
        return "CustomPathEditorPlugin";
    }

    public override void Edit(Godot.Object obj) {
        if (IsInstanceValid(obj)) {
            if (obj is CustomPath pth) {
                path = pth;
                if (IsInstanceValid(path.curve)) {
                    path.curve.EmitSignal("changed");
                }
                GD.Print("Edit");
            }
        } else {
            CustomPath pre = path;
            path = null;
            if (IsInstanceValid(pre)) {
                pre.curve.EmitSignal("changed");
            }
        }
    }

    public override bool Handles(Godot.Object @object) {
        return IsInsideTree() && @object is CustomPath;
    }

    public override void MakeVisible(bool visible) {
        if (visible) {
            GD.Print("Visible");
            curveCreate.Show();
            curveEdit.Show();
            curveDel.Show();
            curveClose.Show();
            handleMenu.Show();
            sep.Show();
            
        } else {
            GD.Print("Hide");
            curveCreate.Hide();
            curveEdit.Hide();
            curveDel.Hide();
            curveClose.Hide();
            handleMenu.Hide();
            sep.Hide();

            {
                CustomPath pre = path;
                path = null;
                if (IsInstanceValid(pre) && IsInstanceValid(pre.curve) ) {
                    pre.curve.EmitSignal("changed");
                }
            }
        }
    }

    public const int clickDist = 10;

    public override bool ForwardSpatialGuiInput(Camera camera, InputEvent @event) {
        if (!IsInstanceValid(path))
            return false;
        Curve3D c = path.curve;
        if (!IsInstanceValid(c))
            return false;
        Transform gt = path.GlobalTransform;
        Transform it = gt.AffineInverse();

        if (@event is InputEventMouseButton mb) {

            Vector2 mbPos = mb.Position;

            if (!mb.Pressed)
                SetHandleClicked(false);

            if (mb.Pressed && mb.ButtonIndex == (int)ButtonList.Left && (curveCreate.Pressed || (curveEdit.Pressed && mb.Control)) ) {
                //click into curve, break it down
                Vector3[] v3a = c.Tessellate();
                int idx = 0;
                int rc = v3a.Length;
                int closestSeg = -1;
                Vector3 closestSegPoint = new Vector3();
                float closest_d = float.MaxValue;

                if (rc >= 2) {

                    if (camera.UnprojectPosition(gt.Xform(c.GetPointPosition(0))).DistanceTo(mbPos) < clickDist)
                        return false; //nope, existing

                    for (int i = 0; i < c.GetPointCount() - 1; i++) {
                        //find the offset and point index of the place to break up
                        int j = idx;
                        if (camera.UnprojectPosition(gt.Xform(c.GetPointPosition(i + 1))).DistanceTo(mbPos) < clickDist)
                            return false; //nope, existing

                        while (j < rc && c.GetPointPosition(i + 1) != v3a[j]) {

                            Vector3 from = v3a[j];
                            Vector3 to = v3a[j + 1];
                            float cdist = from.DistanceTo(to);
                            from = gt.Xform(from);
                            to = gt.Xform(to);
                            if (cdist > 0) {
                                Vector2[] s = new Vector2[2];
                                s[0] = camera.UnprojectPosition(from);
                                s[1] = camera.UnprojectPosition(to);
                                Vector2 inters = GetClosestPointToSegment2D(mbPos, s);
                                float d = inters.DistanceTo(mbPos);

                                if (d < 10 && d < closest_d) {

                                    closest_d = d;
                                    closestSeg = i;
                                    Vector3 ray_from = camera.ProjectRayOrigin(mbPos);
                                    Vector3 ray_dir = camera.ProjectRayNormal(mbPos);

                                    Vector3 ra, rb;
                                    GetClosestPointsBetweenSegments(ray_from, ray_from + ray_dir * 4096, from, to, out ra,out rb);

                                    closestSegPoint = it.Xform(rb);
                                }
                            }
                            j++;
                        }
                        if (idx == j)
                            idx++; //force next
                        else
                            idx = j; //swap

                        if (j == rc)
                            break;
                    }
                }

                var ur = GetUndoRedo();
                if (closestSeg != -1) {
                    //subdivide

                    ur.CreateAction("Split Path");
                    ur.AddDoMethod(c, "add_point", closestSegPoint, new Vector3(), new Vector3(), closestSeg + 1);
                    ur.AddUndoMethod(c, "remove_point", closestSeg + 1);
                    ur.CommitAction();
                    return true;

                } else {

                    Vector3 org;
                    if (c.GetPointCount() == 0)
                        org = path.Transform.origin;
                    else
                        org = gt.Xform(c.GetPointPosition(c.GetPointCount() - 1));
                    Plane p = new Plane();
                    p.Normal = camera.Transform.basis.GetColumn(2);
                    p.D = p.Normal.Dot(org);
                    Vector3 ray_from = camera.ProjectRayOrigin(mbPos);
                    Vector3 ray_dir = camera.ProjectRayNormal(mbPos);

                    Vector3? inters = p.IntersectRay(ray_from, ray_dir);
                    if (inters != null) {

                        ur.CreateAction("Add Point to Curve");
                        ur.AddDoMethod(c, "add_point", it.Xform((Vector3)inters), new Vector3(),new Vector3(), -1);
                        ur.AddUndoMethod(c, "remove_point", c.GetPointCount());
                        ur.CommitAction();
                        return true;
                    }

                    //add new at pos
                }

            } else if (mb.Pressed && ((mb.ButtonIndex == (int)ButtonList.Left && curveDel.Pressed) || (mb.ButtonIndex == (int)ButtonList.Right && curveEdit.Pressed))) {

                for (int i = 0; i < c.GetPointCount(); i++) {
                    float dist_to_p = camera.UnprojectPosition(gt.Xform(c.GetPointPosition(i))).DistanceTo(mbPos);
                    float dist_to_p_out = camera.UnprojectPosition(gt.Xform(c.GetPointPosition(i) + c.GetPointOut(i))).DistanceTo(mbPos);
                    float dist_to_p_in = camera.UnprojectPosition(gt.Xform(c.GetPointPosition(i) + c.GetPointIn(i))).DistanceTo(mbPos);

                    // Find the offset and point index of the place to break up.
                    // Also check for the control points.
                    var ur = GetUndoRedo();
                    if (dist_to_p < clickDist) {
                        ur.CreateAction("Remove Path Point");
                        ur.AddDoMethod(c, "remove_point", i);
                        ur.AddUndoMethod(c, "add_point", c.GetPointPosition(i), c.GetPointIn(i), c.GetPointOut(i), i);
                        ur.CommitAction();
                        return true;
                    } else if (dist_to_p_out < clickDist) {
                        ur.CreateAction(("Remove Out-Control Point"));
                        ur.AddDoMethod(c, "set_point_out", i, new Vector3());
                        ur.AddUndoMethod(c, "set_point_out", i, c.GetPointOut(i));
                        ur.CommitAction();
                        return true;
                    } else if (dist_to_p_in < clickDist) {
                        ur.CreateAction(("Remove In-Control Point"));
                        ur.AddDoMethod(c, "set_point_in", i, new Vector3());
                        ur.AddUndoMethod(c, "set_point_in", i, c.GetPointOut(i));
                        ur.CommitAction();
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void SetHandleClicked(bool clicked) {
        handleClicked = clicked;
    }

    public void _ModeChanged(int p_idx) {

        curveCreate.Pressed = (p_idx == 0);
        curveEdit.Pressed = (p_idx == 1);
        curveDel.Pressed = (p_idx == 2);
    }

    public void _CloseCurve() {
        Curve3D c = path.curve;
        if (!IsInstanceValid(c))
            return;
        if (c.GetPointCount() < 2)
            return;
        c.AddPoint(c.GetPointPosition(0), c.GetPointIn(0), c.GetPointOut(0));
    }

    void HandleOptionPressed(Options opId) {
        PopupMenu pm = handleMenu.GetPopup();

        switch (opId) {
            case Options.Angle: {
                bool is_checked = pm.IsItemChecked((int)opId);
                mirrorHandleAngle = !is_checked;
                pm.SetItemChecked((int)opId, mirrorHandleAngle);
                pm.SetItemDisabled((int)Options.Length, !mirrorHandleAngle);
            } break;
            case Options.Length: {
                bool is_checked = pm.IsItemChecked((int)Options.Length);
                mirrorHandleLength = !is_checked;
                pm.SetItemChecked((int)Options.Length, mirrorHandleLength);
            } break;
            case Options.DoSnap: {
                bool isChecked = pm.IsItemChecked((int)Options.DoSnap);
                snapEnabled = !isChecked;
                pm.SetItemChecked((int)Options.DoSnap,snapEnabled);
            }break;
            case Options.ConfigureSnap: {
                OpenPopup();
            }break;
            case Options.LockX: {
                bool isChecked = pm.IsItemChecked((int)Options.LockX);
                lockX = !isChecked;
                pm.SetItemChecked((int)Options.LockX,lockX);
            }break;
            case Options.LockY: {
                bool isChecked = pm.IsItemChecked((int)Options.LockY);
                lockY = !isChecked;
                pm.SetItemChecked((int)Options.LockY,lockY);
            }break;
            case Options.LockZ: {
                bool isChecked = pm.IsItemChecked((int)Options.LockZ);
                lockZ = !isChecked;
                pm.SetItemChecked((int)Options.LockZ,lockZ);
            }break;
        }
    }

    public Vector3 RestrictPoint(Vector3 original, Vector3 newPoint) {
        bool lx = lockX;
        bool ly = lockY;
        bool lz = lockZ;

        bool xPressed = Input.IsKeyPressed((int)KeyList.Z);
        bool zPressed = Input.IsKeyPressed((int)KeyList.X);
        bool yPressed = Input.IsKeyPressed((int)KeyList.C);

        if(xPressed || zPressed || yPressed) {
            lx = true;
            ly = true;
            lz = true;
        }

        if(xPressed) {
            lx = false;
        }

        if(zPressed) {
            lz = false;
        }

        if(yPressed) {
            ly = false;
        }

        if(lx) {
            newPoint = new Vector3(original.x, newPoint.y, newPoint.z);
        }

        if(ly) {
            newPoint = new Vector3(newPoint.x, original.y, newPoint.z);
        }

        if(lz) {
            newPoint = new Vector3(newPoint.x, newPoint.y, original.z);
        }

        return newPoint;
    }

    static Vector2 GetClosestPointToSegment2D(Vector2 point, Vector2[] segment) {

		Vector2 p = point - segment[0];
		Vector2 n = segment[1] - segment[0];
		float l2 = n.LengthSquared();
		if (l2 < float.Epsilon)
			return segment[0]; // Both points are the same, just give any.

		float d = n.Dot(p) / l2;

		if (d <= 0.0)
			return segment[0]; // Before first point.
		else if (d >= 1.0)
			return segment[1]; // After first point.
		else
			return segment[0] + n * d; // Inside.
	}

    static float GetClosestPointsBetweenSegments(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2, ref Vector2 c1, ref Vector2 c2) {

		Vector2 d1 = q1 - p1; // Direction vector of segment S1.
		Vector2 d2 = q2 - p2; // Direction vector of segment S2.
		Vector2 r = p1 - p2;
		float a = d1.Dot(d1); // Squared length of segment S1, always nonnegative.
		float e = d2.Dot(d2); // Squared length of segment S2, always nonnegative.
		float f = d2.Dot(r);
		float s, t;
		// Check if either or both segments degenerate into points.
		if (a <= float.Epsilon && e <= float.Epsilon) {
			// Both segments degenerate into points.
			c1 = p1;
			c2 = p2;
			return Mathf.Sqrt((c1 - c2).Dot(c1 - c2));
		}
		if (a <= float.Epsilon) {
			// First segment degenerates into a point.
			s = 0.0f;
			t = f / e; // s = 0 => t = (b*s + f) / e = f / e
			t = Mathf.Clamp(t, 0.0f, 1.0f);
		} else {
			float c = d1.Dot(r);
			if (e <= float.Epsilon) {
				// Second segment degenerates into a point.
				t = 0.0f;
				s = Mathf.Clamp(-c / a, 0.0f, 1.0f); // t = 0 => s = (b*t - c) / a = -c / a
			} else {
				// The general nondegenerate case starts here.
				float b = d1.Dot(d2);
				float denom = a * e - b * b; // Always nonnegative.
				// If segments not parallel, compute closest point on L1 to L2 and
				// clamp to segment S1. Else pick arbitrary s (here 0).
				if (denom != 0.0f) {
					s = Mathf.Clamp((b * f - c * e) / denom, 0.0f, 1.0f);
				} else
					s = 0.0f;
				// Compute point on L2 closest to S1(s) using
				// t = Dot((P1 + D1*s) - P2,D2) / Dot(D2,D2) = (b*s + f) / e
				t = (b * s + f) / e;

				//If t in [0,1] done. Else clamp t, recompute s for the new value
				// of t using s = Dot((P2 + D2*t) - P1,D1) / Dot(D1,D1)= (t*b - c) / a
				// and clamp s to [0, 1].
				if (t < 0.0f) {
					t = 0.0f;
					s = Mathf.Clamp(-c / a, 0.0f, 1.0f);
				} else if (t > 1.0f) {
					t = 1.0f;
					s = Mathf.Clamp((b - c) / a, 0.0f, 1.0f);
				}
			}
		}
		c1 = p1 + d1 * s;
		c2 = p2 + d2 * t;
		return Mathf.Sqrt((c1 - c2).Dot(c1 - c2));
	}

    // Do the function 'd' as defined by pb. I think is is dot product of some sort.
    static float d_of(Vector3 m, Vector3 n, Vector3 o, Vector3 p) { 
        return ((m.x - n.x) * (o.x - p.x) + (m.y - n.y) * (o.y - p.y) + (m.z - n.z) * (o.z - p.z));
    
    }

    static void GetClosestPointsBetweenSegments(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2, out Vector3 c1, out Vector3 c2) {
		// Calculate the parametric position on the 2 curves, mua and mub.
		float mua = (d_of(p1, q1, q2, q1) * d_of(q2, q1, p2, p1) - d_of(p1, q1, p2, p1) * d_of(q2, q1, q2, q1)) / (d_of(p2, p1, p2, p1) * d_of(q2, q1, q2, q1) - d_of(q2, q1, p2, p1) * d_of(q2, q1, p2, p1));
		float mub = (d_of(p1, q1, q2, q1) + mua * d_of(q2, q1, p2, p1)) / d_of(q2, q1, q2, q1);

		// Clip the value between [0..1] constraining the solution to lie on the original curves.
		if (mua < 0) mua = 0;
		if (mub < 0) mub = 0;
		if (mua > 1) mua = 1;
		if (mub > 1) mub = 1;
		c1 = p1.LinearInterpolate(p2, mua);
		c2 = q1.LinearInterpolate(q2, mub);
	}

}

#endif