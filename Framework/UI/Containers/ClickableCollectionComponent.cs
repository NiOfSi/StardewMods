﻿using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace Entoarox.Framework.UI
{
    class ClickableCollectionComponent : GenericCollectionComponent
    {
        public event ClickHandler Handler;
        public ClickableCollectionComponent(Point size, ClickHandler handler = null, List<IMenuComponent> components = null) : base(size, components)
        {
            if (handler != null)
                Handler += handler;
        }
        public ClickableCollectionComponent(Rectangle area, ClickHandler handler = null, List<IMenuComponent> components = null) : base(area, components)
        {
            if (handler != null)
                Handler += handler;
        }
        public override void LeftHeld(Point p, Point o)
        {
            
        }
        public override void LeftUp(Point p, Point o)
        {
            
        }
        public override void LeftClick(Point p, Point o)
        {
            Handler?.Invoke(this, Parent, Parent.GetAttachedMenu());
        }
        public override void RightClick(Point p, Point o)
        {
            
        }
    }
}