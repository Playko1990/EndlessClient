using System;
using Microsoft.Xna.Framework;
using XNAControls;

namespace EndlessClient.HUD.Panels
{
    public interface IHudPanel : IGameComponent
    {
        bool Visible { get; set; }

        int UpdateOrder { get; set; }

        int DrawOrder { get; set; }

        Vector2 DrawPosition { get; set; }

        Rectangle DrawArea { get; set; }

        bool IsBeingDragged { get; }

        /// <summary>
        /// True when HUD chrome uses the enlarged resizable layout.
        /// </summary>
        bool ResizableHud { get; }

        event Action DragCompleted;
    }
}
