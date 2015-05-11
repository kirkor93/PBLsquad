﻿using System.Collections.Generic;
using AnimationAux;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using Microsoft.Xna.Framework;
using PBLgame.Engine.GameObjects;


namespace PBLgame.Engine.Components
{
    /// <summary>
    /// Animation clip player. It maps an animation clip onto a model
    /// </summary>
    public class Animator : Component
    {
        #region Fields

        /// <summary>
        /// Current position in time in the clip
        /// </summary>
        private float _position = 0;

        /// <summary>
        /// The clip we are playing
        /// </summary>
        private AnimationClip _clip = null;

        /// <summary>
        /// We maintain a BoneInfo class for each bone. This class does
        /// most of the work in playing the animation.
        /// </summary>
        private BoneInfo[] _boneInfos;

        /// <summary>
        /// The number of bones
        /// </summary>
        private int _boneCnt;

        /// <summary>
        /// The looping option
        /// </summary>
        private bool _looping = false;

        private AnimationType _currentType = AnimationType.Other;

        #endregion

        #region Properties

        /// <summary>
        /// The position in the animation
        /// </summary>
        [Browsable(false)]
        public float Position
        {
            get { return _position; }
            set
            {
                if (value > Duration)
                    value = Duration;

                _position = value;
                foreach (BoneInfo bone in _boneInfos)
                {
                    bone.SetPosition(_position);
                }
            }
        }

        /// <summary>
        /// The associated animation clip
        /// </summary>
        [Browsable(false)]
        public AnimationClip Clip { get { return _clip; } }

        /// <summary>
        /// The clip duration
        /// </summary>
        [Browsable(false)]
        public float Duration { get { return (float) _clip.Duration; } }

        /// <summary>
        /// The looping option. Set to true if you want the animation to loop
        /// back at the end
        /// </summary>
        public bool Looping { get { return _looping; } set { _looping = value; } }

        /// <summary>
        /// Additional speed multiplier. For example to synchronize walking speed with animation.
        /// </summary>
        public float Speed { get; set; }

        public AnimatedMesh AnimMesh { get { return (AnimatedMesh)_gameObject.renderer.MyMesh; } }

        #endregion


        public Animator(GameObject owner) : base(owner)
        {
            Speed = 1.0f;
            // don't forget to implement copy constructor below
        }

        public Animator(Animator src, GameObject owner) : base(owner)
        {
            Speed = src.Speed;
        }

        /// <summary>
        /// Constructor for the animation player. It makes the 
        /// association between a clip and a model and sets up for playing
        /// </summary>
        /// <param name="clip">clip to animate</param>
        /// <param name="loop">loop animation</param>
        /// <param name="speed">speed multiplier</param>
        public void PlayAnimation(AnimationClip clip, bool loop = true, float speed = 1.0f)
        {
            this._clip = clip;
            this._looping = loop;
            this.Speed = speed;

            // Create the bone information classes
            _boneCnt = clip.Bones.Count;
            _boneInfos = new BoneInfo[_boneCnt];

            for (int b = 0; b < _boneInfos.Length; b++)
            {
                // Create it
                _boneInfos[b] = new BoneInfo(clip.Bones[b]);

                // Assign it to a model bone
                _boneInfos[b].SetModel(AnimMesh);
            }

            Rewind();
        }

        public void Walk(float velocity)
        {
            if (_currentType == AnimationType.Walk)
            {
                Speed = velocity;
            }
            else
            {
                _currentType = AnimationType.Walk;
                PlayAnimation(AnimMesh.Skeleton.Walk, true, velocity);
            }
        }

        public void Idle()
        {
            if (_currentType != AnimationType.Idle)
            {
                _currentType = AnimationType.Idle;
                PlayAnimation(AnimMesh.Skeleton.Idle, true, 1.0f);
            }
        }

        public enum AnimationType
        {
            Idle, Walk, Other
        }

        #region Update and Transport Controls


        /// <summary>
        /// Reset back to time zero.
        /// </summary>
        public void Rewind()
        {
            Position = 0;
        }


        public override void Initialize()
        {
            if (_clip == null)
            {
                PlayAnimation(AnimMesh.Skeleton.Clips[0]);
            }
        }

        /// <summary>
        /// Update the clip position. Also updates bones in model.
        /// </summary>
        /// <param name="gameTime">time</param>
        public override void Update(GameTime gameTime)
        {
            float newPosition = Position + (float) gameTime.ElapsedGameTime.TotalSeconds * _clip.Speed * Speed;

            if (_looping) {
                while (newPosition >= Duration)
                {
                    newPosition -= Duration;
                }
                while (newPosition < 0)
                {
                    newPosition = newPosition + Duration;
                }
            }
            Position = newPosition;

            AnimMesh.UpdateBonesMatrices();
        }

        #endregion

        #region BoneInfo class


        /// <summary>
        /// Information about a bone we are animating. This class connects a bone
        /// in the clip to a bone in the model.
        /// </summary>
        private class BoneInfo
        {
            #region Fields

            /// <summary>
            /// The current keyframe. Our position is a time such that the 
            /// we are greater than or equal to this keyframe's time and less
            /// than the next keyframes time.
            /// </summary>
            private int currentKeyframe = 0;

            /// <summary>
            /// Bone in a model that this keyframe bone is assigned to
            /// </summary>
            private Bone assignedBone = null;

            /// <summary>
            /// We are not valid until the rotation and translation are set.
            /// If there are no keyframes, we will never be valid
            /// </summary>
            public bool valid = false;

            /// <summary>
            /// Current animation rotation
            /// </summary>
            private Quaternion rotation;

            /// <summary>
            /// Current animation translation
            /// </summary>
            public Vector3 translation;

            /// <summary>
            /// We are at a location between Keyframe1 and Keyframe2 such 
            /// that Keyframe1's time is less than or equal to the current position
            /// </summary>
            public AnimationClip.Keyframe Keyframe1;

            /// <summary>
            /// Second keyframe value
            /// </summary>
            public AnimationClip.Keyframe Keyframe2;

            #endregion

            #region Properties

            /// <summary>
            /// The bone in the actual animation clip
            /// </summary>
            public AnimationClip.Bone ClipBone { get; set; }

            /// <summary>
            /// The bone this animation bone is assigned to in the model
            /// </summary>
            public Bone ModelBone { get { return assignedBone; } }

            #endregion

            #region Constructor

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="bone"></param>
            public BoneInfo(AnimationClip.Bone bone)
            {
                this.ClipBone = bone;
                SetKeyframes();
                SetPosition(0);
            }


            #endregion

            #region Position and Keyframes

            /// <summary>
            /// Set the bone based on the supplied position value
            /// </summary>
            /// <param name="position"></param>
            public void SetPosition(float position)
            {
                List<AnimationClip.Keyframe> keyframes = ClipBone.Keyframes;
                if (keyframes.Count == 0)
                    return;

                // If our current position is less that the first keyframe
                // we move the position backward until we get to the right keyframe
                while (position < Keyframe1.Time && currentKeyframe > 0)
                {
                    // We need to move backwards in time
                    currentKeyframe--;
                    SetKeyframes();
                }

                // If our current position is greater than the second keyframe
                // we move the position forward until we get to the right keyframe
                while (position >= Keyframe2.Time && currentKeyframe < ClipBone.Keyframes.Count - 2)
                {
                    // We need to move forwards in time
                    currentKeyframe++;
                    SetKeyframes();
                }

                if (Keyframe1 == Keyframe2)
                {
                    // Keyframes are equal
                    rotation = Keyframe1.Rotation;
                    translation = Keyframe1.Translation;
                }
                else
                {
                    // Interpolate between keyframes
                    float t = (float)((position - Keyframe1.Time) / (Keyframe2.Time - Keyframe1.Time));
                    rotation = Quaternion.Slerp(Keyframe1.Rotation, Keyframe2.Rotation, t);
                    translation = Vector3.Lerp(Keyframe1.Translation, Keyframe2.Translation, t);
                }

                valid = true;
                if (assignedBone != null)
                {
                    // Send to the model
                    // Make it a matrix first
                    Matrix m = Matrix.CreateFromQuaternion(rotation);
                    m.Translation = translation;
                    assignedBone.SetCompleteTransform(m);
                }
            }



            /// <summary>
            /// Set the keyframes to a valid value relative to 
            /// the current keyframe
            /// </summary>
            private void SetKeyframes()
            {
                if (ClipBone.Keyframes.Count > 0)
                {
                    Keyframe1 = ClipBone.Keyframes[currentKeyframe];
                    if (currentKeyframe == ClipBone.Keyframes.Count - 1)
                        Keyframe2 = Keyframe1;
                    else
                        Keyframe2 = ClipBone.Keyframes[currentKeyframe + 1];
                }
                else
                {
                    // If there are no keyframes, set both to null
                    Keyframe1 = null;
                    Keyframe2 = null;
                }
            }

            /// <summary>
            /// Assign this bone to the correct bone in the model
            /// </summary>
            /// <param name="model"></param>
            public void SetModel(AnimatedMesh model)
            {
                // Find this bone
                assignedBone = model.FindBone(ClipBone.Name);

            }

            #endregion
        }

        #region XML Serialization

        public override void ReadXml(XmlReader reader)
        {
            base.ReadXml(reader);
            reader.Read();
        }

        public override void WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);
        }

        #endregion

        #endregion
    }

    public class Skeleton
    {
        public int Id;
        public List<AnimationClip> Clips { get; private set; }

        public AnimationClip Idle { get { return Clips.First(c => c.Type == "Idle"); } }
        public AnimationClip Walk { get { return Clips.First(c => c.Type == "Walk"); } }


        public Skeleton(int id)
        {
            Id = id;
            Clips = new List<AnimationClip>();
        }

        public void AddClip(AnimationClip animation)
        {
            Clips.Add(animation);
        }
    }
}
