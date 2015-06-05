﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using PBLgame.Engine.AI;
using PBLgame.Engine.Components;
using PBLgame.Engine.GameObjects;
using PBLgame.Engine.Physics;

namespace PBLgame.GamePlay
{
    public class EnemyMeleeScript : CharacterHandler
    {
        #region Variables
        #region Enemy Vars
        public AIComponent AIComponent;
        public float ChaseSpeed = 0.001f;
        private float _attackDelay = 2500;

        private int _hp;
        private Vector3 _startingPosition;
        private Vector3 _chaseStartPosition;

        private float _attackTimer = 2000;

        private float _chaseTimer;

        private MeleeAction _currentAction = MeleeAction.Stay;

        private GameObject _attackTriggerObject;

        #endregion  
        #region DTNodes

        private DecisionNode _distanceNode = new DecisionNode();
        private DecisionNode _hpNode = new DecisionNode();
        private DecisionNode _canAttackNode = new DecisionNode();
        private ActionNode _attackNode = new ActionNode();
        private ActionNode _chaseNode = new ActionNode();
        private ActionNode _standNode = new ActionNode();
        private ActionNode _escapeNode = new ActionNode();

        #endregion
        #endregion
        #region Methods
        public EnemyMeleeScript(GameObject owner) : base(owner)
        {
            _hp = 100;

            _attackTriggerObject = new GameObject();
            _attackTriggerObject.Tag = "EnemyWeapon";
            _attackTriggerObject.transform.Position = new Vector3(0.0f, 10.0f, 15.0f);
            _attackTriggerObject.parent = this.gameObject;

            _attackTriggerObject.collision = new Collision(_attackTriggerObject);
            _attackTriggerObject.collision.Rigidbody = false;
            _attackTriggerObject.collision.Static = false;
            _attackTriggerObject.collision.MainCollider = new SphereCollider(_attackTriggerObject.collision, 5.0f, true);
            _attackTriggerObject.collision.Enabled = false;

            gameObject.collision.OnTrigger += GetHitMethod;

            _startingPosition = gameObject.transform.Position;
            _chaseTimer = 0.0f;

            #region DecisionTree & AiComponentInitialize
            _distanceNode.DecisionEvent += EnemyClose;
            _hpNode.DecisionEvent += IsMyHP;
            _canAttackNode.DecisionEvent += CanAtack;
            _attackNode.ActionEvent += AttackPlayer;
            _chaseNode.ActionEvent += GoToPlayer;
            _standNode.ActionEvent += StandStill;
            _escapeNode.ActionEvent += Escape;

            AIComponent = _gameObject.GetComponent<AIComponent>();
            if (AIComponent == null)
            {
                AIComponent = new AIComponent(owner);
                _gameObject.AddComponent<AIComponent>(AIComponent);
            }


            _distanceNode.TrueChild = _hpNode;
            _distanceNode.FalseChild = _standNode;

            _hpNode.TrueChild = _canAttackNode;
            _hpNode.FalseChild = _escapeNode;

            _canAttackNode.TrueChild = _attackNode;
            _canAttackNode.FalseChild = _chaseNode;

            AIComponent.MyDTree.DTreeStart = _distanceNode;
            #endregion  
         }

        public int HP
        {
            get { return _hp; }
            set { _hp = value; }
        }

        public void GetHitMethod(Object o, ColArgs args)
        {
            if (args.EnemyBox != null && args.EnemyBox.Owner.gameObject.Tag == "Weapon")
            {
                PlayerScript stats = args.EnemyBox.Owner.gameObject.parent.GetComponent<PlayerScript>();
                if (stats != null)
                {
                    _hp -= (stats.Stats.BasePhysicalDamage.Value + stats.Stats.FastAttackDamageBonus.Value);
                }
            }
            else if (args.EnemySphere != null && args.EnemySphere.Owner.gameObject.Tag == "Weapon")
            {
                PlayerScript stats = args.EnemySphere.Owner.gameObject.parent.GetComponent<PlayerScript>();
                if (stats != null)
                {
                    _hp -= (stats.Stats.BasePhysicalDamage.Value + stats.Stats.FastAttackDamageBonus.Value);
                }
            }
            Console.WriteLine("Enemy hp value = " + _hp);
            if (HP <= 0)
            {
                gameObject.Enabled = false;
                _attackTriggerObject.Enabled = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (_hp > 0)
            {
                base.Update(gameTime);
                _attackTriggerObject.Update(gameTime);
                Vector3 dir;
                switch (_currentAction)
                {
                    case MeleeAction.Attack:
                        dir = AISystem.Player.transform.Position - gameObject.transform.Position;
                        SetLookVector(new Vector2(dir.Z, dir.X));
                        _attackTimer += gameTime.ElapsedGameTime.Milliseconds;
                        if (_attackTimer > _attackDelay)
                        {
                            _attackTriggerObject.collision.Enabled = true;
                            _attackTimer = 0.0f;
                            foreach (GameObject go in PhysicsSystem.CollisionObjects)
                            {
                                if (_attackTriggerObject != go && go.collision.Enabled && _attackTriggerObject.collision.MainCollider.Contains(go.collision.MainCollider) != ContainmentType.Disjoint)
                                {
                                    _attackTriggerObject.collision.ChceckCollisionDeeper(go);
                                }
                            }
                            _attackTriggerObject.collision.Enabled = false;
                        } 
                        break;
                    case MeleeAction.Chase:
                        _chaseTimer += gameTime.ElapsedGameTime.Milliseconds;
                        dir = AISystem.Player.transform.Position - gameObject.transform.Position;
                        SetLookVector(new Vector2(dir.Z, dir.X));
                        gameObject.transform.Position = Vector3.Lerp(_chaseStartPosition, AISystem.Player.transform.Position, _chaseTimer * ChaseSpeed);
                        break;
                    case MeleeAction.Escape:
                        dir = gameObject.transform.Position - AISystem.Player.transform.Position;
                        Random rand = new Random();
                        int x = rand.Next(0, 100);
                        int y = rand.Next(0, 100);
                        SetLookVector(new Vector2(dir.Z, dir.X));
                        dir.X *= x / 100.0f;
                        dir.Z *= y / 100.0f;
                        gameObject.transform.Position += (new Vector3(dir.X, 0.0f, dir.Z) * 0.02f);
                        break;
                    case MeleeAction.Stay:
                        break;
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            _attackTriggerObject.Draw(gameTime);
        }

        private bool EnemyClose()
        {
            if (Vector3.Distance(gameObject.transform.Position, AISystem.Player.transform.Position) < 100.0f) return true;
            else return false;
        }

        private bool CanAtack()
        {
            if (Vector3.Distance(gameObject.transform.Position, AISystem.Player.transform.Position) < 20.0f) return true;
            else return false;
        }

        private bool IsMyHP()
        {
            if (_hp > 10.0f) return true;
            else return false;
        }

        private void AttackPlayer()
        {
            _currentAction = MeleeAction.Attack;
        }

        private void GoToPlayer()
        {
            _currentAction = MeleeAction.Chase;
            _chaseTimer = 0.0f;
            _chaseStartPosition = gameObject.transform.Position;
        }

        private void StandStill()
        {
            _currentAction = MeleeAction.Stay;
        }

        private void Escape()
        {
            _currentAction = MeleeAction.Escape;
        }
#endregion

        enum MeleeAction
        {
            Chase = 0,
            Attack,
            Escape,
            Stay
        }
    }

}
