﻿using System.Collections.Generic;
using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using PistonDerby.Elementos;
using PistonDerby.Collisions;
using PistonDerby.Utils;
using PistonDerby.Gizmo;
using PistonDerby.HUD;
using PistonDerby.Autos;
using TGC.MonoGame.Samples.Geometries;

namespace PistonDerby;

public class PistonDerby : Game 
{
    public const float S_METRO = 250f;
    internal static bool DEVELOPER_MODE = false;
    internal static bool DEBUG_GIZMOS = false;
    internal static bool FULL_SCREEN = false;
    internal static bool INITIAL_ANIMATION = true;
    private GraphicsDeviceManager Graphics;
    private SpriteBatch SpriteBatch;
    private FullScreenQuad FullScreenQuad;
    internal static GameSimulation Simulation;
    internal static Content GameContent;
    internal static GameMenu GameMenu;
    internal static Gizmos Gizmos;
    private CarHUD CarHUD;
    internal static AudioPlayer Reproductor;
    private Camera Camera; 
    private Casa Casa;
    private Auto Auto; 
    private List<AutoDummy> AutosDummy;
    private List<AutoAI> AutosAI;
    internal static List<ElementoDinamico> ElementosDinamicos = new List<ElementoDinamico>(); //Lista temporal que contiene Elementos Dinamicos de manera Global || Probablemente Casa deba ser Global y contener esta lista
    private RenderTarget2D MainSceneRenderTarget ;
    private RenderTarget2D FirstPassBloomRenderTarget ;
    private RenderTarget2D SecondPassBloomRenderTarget ;

    public PistonDerby() {
        Graphics = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
    }

    protected override void Initialize() 
    {
        var rasterizerState = new RasterizerState();
        rasterizerState.CullMode = CullMode.None;
        GraphicsDevice.RasterizerState = rasterizerState; // CullCounterClockwise; para activar el Culling
        GraphicsDevice.BlendState = BlendState.AlphaBlend;     

        Graphics.PreferredBackBufferHeight = (DEVELOPER_MODE)?
                                             GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height : 
                                             GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        Graphics.PreferredBackBufferWidth  = (DEVELOPER_MODE)?
                                             Graphics.PreferredBackBufferHeight*16/9 :
                                             GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        
        Graphics.PreferredBackBufferWidth  = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width * 3/4;
        Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height * 3/4;

        Graphics.IsFullScreen = FULL_SCREEN;

        
        Graphics.ApplyChanges();
    
        Camera = new Camera(GraphicsDevice.Viewport.AspectRatio);
        Simulation = new GameSimulation();
        if(!DEVELOPER_MODE) Reproductor = new AudioPlayer();
        Gizmos = new Gizmos();
        Gizmos.Enabled = DEBUG_GIZMOS;
        Casa = new Casa();

        AutosDummy = new List<AutoDummy>();
        AutosAI = new List<AutoAI>();
        base.Initialize();
    }
    protected override void LoadContent() 
    {
        base.LoadContent();

        GameContent = new Content(Content, GraphicsDevice);
        ConfigureRenderTargets();
        
        SpriteBatch = new SpriteBatch(GraphicsDevice);

        GameMenu = new GameMenu(Graphics.PreferredBackBufferWidth, Graphics.PreferredBackBufferHeight);
        
        Gizmos.LoadContent(GraphicsDevice, new ContentManager(Content.ServiceProvider, "Content"));
        Reproductor?.LoadContent();
        Casa.LoadContent();

        foreach (var e in GameContent.Efectos) e.Parameters["Projection"]?.SetValue(Camera.Projection);
        foreach (var e in GameContent.EfectosHUD) e.Parameters["Projection"]?.SetValue(Camera.Projection);
        PistonDerby.GameContent.E_BlurEffect.Parameters["screenSize"]
                .SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));



        ConfiguracionBlinnPhong(PistonDerby.GameContent.E_BlinnPhong);
        ConfiguracionBlinnPhong(PistonDerby.GameContent.E_BlinnPhongTiles);

        AutosDummy.Add(new AutoDummy (Casa.PuntoCentro(0) * 0.75f));
        AutosDummy.Add(new AutoDummy (Casa.PuntoCentro(1)));
        AutosDummy.Add(new AutoDummy (Casa.PuntoCentro(2)));

        Auto   = new Auto (Casa.PuntoCentro(0));
        AutosAI.Add(new AutoAI (Auto, Casa.PuntoCentro(0)*0.5f));
        AutosAI.Add(new AutoAI (Auto, Casa.PuntoCentro(1)*0.5f));
        AutosAI.Add(new AutoAI (Auto, Casa.PuntoCentro(2)*0.5f));
        
        
        CarHUD = new CarHUD(Graphics.PreferredBackBufferWidth, Graphics.PreferredBackBufferHeight);
        Auto.AsociarHUD(CarHUD);
    }

    private void ConfigureRenderTargets()
    {
        // Create a full screen quad to post-process
        FullScreenQuad = new FullScreenQuad(GraphicsDevice);
     
        // Create render targets. 
        // MainRenderTarget is used to store the scene color
        // BloomRenderTarget is used to store the bloom color and switches with MultipassBloomRenderTarget
        // depending on the pass count, to blur the bloom color
        MainSceneRenderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0,
            RenderTargetUsage.DiscardContents);
        FirstPassBloomRenderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0,
            RenderTargetUsage.DiscardContents);
        SecondPassBloomRenderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.DiscardContents);
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        float dTime = Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds); 

        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();
        if (Keyboard.GetState().IsKeyDown(Keys.G)) Gizmos.Enabled = !Gizmos.Enabled;
        if(DEBUG_GIZMOS) Gizmos.UpdateViewProjection(Camera.View, Camera.Projection);

        if(GameMenu.isRunning() && INITIAL_ANIMATION) {
            GameMenu.Update(gameTime, keyboardState, Mouse.GetState());
            return;
        }

        // Actualizo la posición de la cámara en el shader de PBR
        foreach(Effect e in PistonDerby.GameContent.Efectos){
            e.Parameters["eyePosition"]?.SetValue(Camera.CameraPosition);
            e.Parameters["Time"]?.SetValue((float)gameTime.TotalGameTime.TotalSeconds);
        }

        Reproductor?.Update(dTime, Keyboard.GetState());                      

        Casa.Update(dTime, keyboardState);
        Auto.Update(dTime, Keyboard.GetState());
        foreach(AutoDummy a in AutosDummy)
            a.Update(dTime);
        foreach(AutoAI a in AutosAI)
            a.Update(dTime);

        foreach(ElementoDinamico e in ElementosDinamicos) e.Update(dTime, keyboardState);

        Camera.Mover(keyboardState);
        Camera.Update(Auto.World);

        Simulation.Update();
        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        if(GameMenu.isRunning() && INITIAL_ANIMATION) {
            GameMenu.Draw(gameTime);
            return;
        }
        
        // Use the default blend and depth configuration
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        
        ////// DIBUJAMOS LA ESCENA PRINCIPAL

        GraphicsDevice.SetRenderTarget(MainSceneRenderTarget);
        GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);


        foreach (var e in GameContent.Efectos){
            e.Parameters["View"]?.SetValue(Camera.View);
        }
        foreach (ElementoDinamico elementoDinamico in ElementosDinamicos)
            elementoDinamico.Draw();

        Auto.Draw();
        foreach(AutoDummy a in AutosDummy) a.Draw();
        foreach(AutoAI a in AutosAI) a.Draw();
                 
        Casa.Draw();
        Gizmos.Draw();
        CarHUD.Draw();

        this.DebugGizmos();

        //// DRAW BLOOM
        // Set the render target as our bloomRenderTarget, we are drawing the bloom color into this texture
        GraphicsDevice.SetRenderTarget(FirstPassBloomRenderTarget);
        GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);

        Effect BloomEffect = PistonDerby.GameContent.E_BloomEffect;
        BloomEffect.CurrentTechnique = BloomEffect.Techniques["BloomPass"];
        BloomEffect.Parameters["baseTexture"].SetValue(PistonDerby.GameContent.TA_LightEmissionMap);

        // We get the base transform for each mesh
        // var modelMeshesBaseTransforms = new Matrix[Auto.Model.Bones.Count];
        // Auto.Model.CopyAbsoluteBoneTransformsTo(modelMeshesBaseTransforms);
        // Effect.Parameters["WorldViewProjection"].SetValue(Auto.World * Camera.View * Camera.Projection);
        foreach (var modelMesh in Auto.Model.Meshes)
        {
            foreach (var part in modelMesh.MeshParts)
                part.Effect = BloomEffect;

            // We set the main matrices for each mesh to draw
            // var worldMatrix = modelMeshesBaseTransforms[modelMesh.ParentBone.Index];

            // WorldViewProjection is used to transform from model space to clip space
            BloomEffect.Parameters["WorldViewProjection"].SetValue(Auto.World * Camera.View * Camera.Projection);

            // Once we set these matrices we draw
            modelMesh.Draw();
        }

        //// BLUR BLOOM

        
        #region Multipass Bloom
        Effect BlurEffect = PistonDerby.GameContent.E_BlurEffect;

        // Now we apply a blur effect to the bloom texture
        // Note that we apply this a number of times and we switch
        // the render target with the source texture
        // Basically, this applies the blur effect N times
        BlurEffect.CurrentTechnique = BlurEffect.Techniques["Blur"];

        var bloomTexture = FirstPassBloomRenderTarget;
        var finalBloomRenderTarget = SecondPassBloomRenderTarget;

        
        // Set the render target as null, we are drawing into the screen now!
        GraphicsDevice.SetRenderTarget(finalBloomRenderTarget);
        GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);

        BlurEffect.Parameters["baseTexture"].SetValue(bloomTexture);
        FullScreenQuad.Draw(BlurEffect);

        #endregion

        #region Final Pass

        // Set the depth configuration as none, as we don't use depth in this pass
        GraphicsDevice.DepthStencilState = DepthStencilState.None;

        // Set the render target as null, we are drawing into the screen now!
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        // Set the technique to our blur technique
        // Then draw a texture into a full-screen quad
        // using our rendertarget as texture
        BloomEffect.CurrentTechnique = BloomEffect.Techniques["Integrate"];
        BloomEffect.Parameters["baseTexture"].SetValue(MainSceneRenderTarget);
        BloomEffect.Parameters["bloomTexture"].SetValue(finalBloomRenderTarget);
        FullScreenQuad.Draw(BloomEffect);

        #endregion

    }
    private void DebugGizmos()
    {
        Casa.DebugGizmos();
        BoundingBox aabb;
        foreach(ElementoDinamico e in ElementosDinamicos){
            aabb = e.Body().BoundingBox.ToBoundingBox(); 
            PistonDerby.Gizmos.DrawCube((aabb.Max + aabb.Min) / 2f, aabb.Max - aabb.Min, Color.DeepPink);
        }
    }
    protected override void UnloadContent()
    {
        Simulation.Dispose();
        base.UnloadContent();
        FullScreenQuad.Dispose();
        FirstPassBloomRenderTarget.Dispose();
        MainSceneRenderTarget.Dispose();
        SecondPassBloomRenderTarget.Dispose();
    }
    private void ConfiguracionBlinnPhong(Effect e)
    {
        e.Parameters["ambientColor"].SetValue(new Vector3(255, 255, 255) / 255f);
        e.Parameters["diffuseColor"].SetValue(new Vector3(255, 255, 255) / 255f); // Adjusted the blue component to reduce yellow tint
        e.Parameters["specularColor"].SetValue(new Vector3(0, 148, 148) / 255f);

        e.Parameters["KAmbient"].SetValue(0.15f);
        e.Parameters["KDiffuse"].SetValue(0.3f);
        e.Parameters["KSpecular"].SetValue(0.3f);

        e.Parameters["shininess"].SetValue(60); // No shininess, as there are no specular highlights

    }
}
