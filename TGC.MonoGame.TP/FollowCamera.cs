﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace TGC.MonoGame.TP
{
    class FollowCamera
    {
        private float AxisDistanceToTarget = 2000f;

        private float AngleFollowSpeed = 0.015f;

        private float AngleThreshold = 0.85f;

        public Matrix Projection { get; private set; }

        public Matrix View { get; private set; }

        private Vector3 CurrentRightVector { get; set; } = Vector3.Right;

        private float RightVectorInterpolator { get; set; } = 0f;

        private Vector3 PastRightVector { get; set; } = Vector3.Right;

        public FollowCamera(float aspectRatio)
        {
            Projection = Matrix.CreatePerspectiveFieldOfView(MathF.PI / 3f, aspectRatio, 0.1f, 100000f);
        }

        public void Mover(KeyboardState keyboardState){
            var multiplicador = 1f;
            if(keyboardState.IsKeyDown(Keys.LeftShift)){
                multiplicador = 10f;
            }   
            if(keyboardState.IsKeyDown(Keys.Down)){
                AxisDistanceToTarget += 20f*multiplicador;
            }
            if(keyboardState.IsKeyDown(Keys.Up)){
                AxisDistanceToTarget -= 20f*multiplicador;
            }
        }

        public void Update(GameTime gameTime, Matrix followedWorld)
        {
            var elapsedTime = Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds);
            
            var followedPosition = followedWorld.Translation;

            var followedRight = followedWorld.Right;

            if (Vector3.Dot(followedRight, PastRightVector) > AngleThreshold)
            {
                RightVectorInterpolator += elapsedTime * AngleFollowSpeed;

                RightVectorInterpolator = MathF.Min(RightVectorInterpolator, 1f);

                CurrentRightVector = Vector3.Lerp(CurrentRightVector, followedRight, RightVectorInterpolator * RightVectorInterpolator);
            }
            else
                RightVectorInterpolator = 0f;

            PastRightVector = followedRight;
            
            var offsetedPosition = followedPosition 
                + CurrentRightVector * AxisDistanceToTarget
                + Vector3.Up * AxisDistanceToTarget;

            var forward = (followedPosition - offsetedPosition);
            forward.Normalize();

            var right = Vector3.Cross(forward, Vector3.Up);
            var cameraCorrectUp = Vector3.Cross(right, forward);

            View = Matrix.CreateLookAt(offsetedPosition, followedPosition, cameraCorrectUp);
        }
    }
}
