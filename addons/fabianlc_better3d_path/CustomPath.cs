using Godot;
using System;
using System.Collections.Generic;

[Tool]
public class CustomPath : Spatial {
    [Export]
    public Curve3D curve;

    public override void _EnterTree() {
        if(!IsInstanceValid(curve)) {
            curve = new Curve3D();
        }

        if(!curve.IsConnected("changed", this, "update_gizmo")) {
            curve.Connect("changed", this, "update_gizmo");
        }
    }
}