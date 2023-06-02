using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PistonDerby.Drawers;
internal class TextureDrawer : IDrawer
{
    protected Effect Effect = PistonDerby.GameContent.E_TextureShader;
    protected readonly Texture2D Texture;

    internal TextureDrawer(Texture2D Texture)
    {
        this.Texture = Texture;
    }

    void IDrawer.Draw(Model Model, Matrix World)
    {
        ModelMeshCollection meshes = Model.Meshes;;

        Effect.Parameters["Texture"].SetValue(Texture);
        foreach(var mesh in Model.Meshes) {
            // El EnemyCar, si usa esta matrix, las ruedas se le colocan piola
            // pero algunos muebles se "desarman"
            // Matrix meshBoneTranslation = Matrix.CreateTranslation(mesh.ParentBone.ModelTransform.Translation);
            Matrix meshBoneTranslation = Matrix.Identity; 
            foreach(var meshPart in mesh.MeshParts) {
                meshPart.Effect = Effect;
                meshPart.Effect.Parameters["World"]?.SetValue(meshBoneTranslation*World);
            }   
            mesh.Draw();
        }
    }
}