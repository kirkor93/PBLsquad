﻿using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PBLgame.Engine;
using PBLgame.Engine.Components;
using PBLgame.Engine.GameObjects;
using PBLgame.Engine.GUI;
using PBLgame.Engine.Singleton;

namespace PBLgame.GamePlay
{
    class PlayerScript : CharacterHandler
    {
        #region Variables
        #region Public
        public PlayerStatistics Stats { get; private set; }
        #endregion
        #region Private
        /// <summary>
        /// Get character turn (look direction) from walk direciton if right stick is floating (not pushed).
        /// </summary>
        private bool _syncAngles = true;

        private Bar _healthBar;
        private Bar _manaBar;
        #endregion
        #endregion

        #region Properties
        #endregion

        #region Methods

        public PlayerScript(GameObject gameObj) : base(gameObj)
        {
            Stats = new PlayerStatistics(100, 100);
            InputManager.Instance.OnTurn   += CharacterRotation;
            InputManager.Instance.OnMove   += CharacterTranslate;
            InputManager.Instance.OnButton += CharacterAction;

            SpeedMultiplier = 70f;
            //getting controls from gui
            _healthBar = HUD.Instance.GetGuiObject("Health_bar") as Bar;
            _manaBar = HUD.Instance.GetGuiObject("Mana_bar") as Bar;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _healthBar.FillAmount = ConvertRange(Convert.ToSingle(Stats.Health.Value), 0.0f, Stats.Health.MaxValue, 0.0f, 1.0f);
            _manaBar.FillAmount = ConvertRange(Convert.ToSingle(Stats.Energy.Value), 0.0f, Stats.Energy.MaxValue, 0.0f, 1.0f);
        }

        private float ConvertRange(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            return ((value - oldMin) / (oldMax - oldMin)) * (newMax - newMin) + newMin;
        }
        private void CharacterRotation(Object obj, MoveArgs args)
        {
            if (args.AxisValue.LengthSquared() < 0.01)
            {
                _syncAngles = true;
            }
            else
            {
                _syncAngles = false;
                SetLookVector(args.AxisValue);
            }
        }


        private void CharacterTranslate(Object o, MoveArgs args)
        {
            UnitVelocity = new Vector2(args.AxisValue.X, -args.AxisValue.Y);
            if (_syncAngles && args.AxisValue.LengthSquared() > 1e-5f)
            {
                SetLookVector(args.AxisValue);
            }
        }


        private void CharacterAction(Object o, ButtonArgs button)
        {
            if (button.IsDown)
            {
                switch (button.ButtonName)
                {
                    case Buttons.LeftShoulder:
                        {
                            const int cost = 1;
                            if (Stats.Energy.TryDecrease(cost))
                            {
                                Console.WriteLine("telekinetic push");
                                InputManager.Instance.RumplePad(200, 1, 0.5f);
                            }
                            else
                            {
                                Console.WriteLine("Not enough mana");
                            }
                        }
                        break;

                    case Buttons.LeftTrigger:
                        {
                            const int cost = 3;
                            if (Stats.Energy.TryDecrease(cost))
                            {
                                Console.WriteLine("telekinetic shield");
                                InputManager.Instance.RumplePad(300, 0.3f, 0.7f);
                            }
                            else
                            {
                                Console.WriteLine("Not enough mana");
                            }
                        }
                        break;

                    case Buttons.RightShoulder:
                        {
                            Console.WriteLine("quick attack");
                        }
                        break;

                    case Buttons.RightTrigger:
                        {
                            Console.WriteLine("strong attack");
                            InputManager.Instance.RumplePad(200, 1, 1);
                        }
                        break;
                }
                Console.WriteLine(Stats.ToString());
            }
        }

        public override Component Copy(GameObject newOwner)
        {
            // developper doesn't get responsibility for unexpected behaviour
            return new PlayerScript(newOwner);
        }

        #endregion
    }
}
