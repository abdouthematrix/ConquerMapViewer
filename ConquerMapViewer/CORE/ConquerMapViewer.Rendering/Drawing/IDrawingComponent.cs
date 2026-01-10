using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerMapViewer.Rendering.Drawing;

public interface IDrawingComponent
{
    bool Enabled { get; set; }
    void UpdateScreen(Rectangle screenRect);
    void Draw(SpriteBatch spriteBatch, Matrix transformMatrix);
}
