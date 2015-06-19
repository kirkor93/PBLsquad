﻿using System;
using Microsoft.Xna.Framework;
using PBLgame.Engine.AI;
using PBLgame.Engine.Components;
using PBLgame.Engine.GameObjects;
using PBLgame.Engine.Physics;

namespace PBLgame.GamePlay
{
    public class EnemyRangedScript : EnemyScript
    {
        #region Variables
        #region Enemy Vars
        public float AttackRange = 45.0f;
        public float AttackAffectDelay;
        
        private RangeAction _currentAction = RangeAction.Stay;

        #endregion
        #region DTNodes

        private DecisionNode _distanceNode = new DecisionNode();
        private DecisionNode _distance2Node = new DecisionNode();
        private DecisionNode _hpNode = new DecisionNode();
        private DecisionNode _canAttackNode = new DecisionNode();
        private ActionNode _attackNode = new ActionNode();
        private ActionNode _chaseNode = new ActionNode();
        private ActionNode _standNode = new ActionNode();
        private ActionNode _escapeNode = new ActionNode();
        private ActionNode _escapeNattackNode = new ActionNode();

        #endregion
        #endregion
        #region Methods
        public EnemyRangedScript(GameObject owner) : base(owner, 100)
        {
            SetupScript(new Vector3(AttackRange, 10.0f, 0.0f), 5.0f);
            ChaseSpeed = 0.005f;
            _attackTimer = 1000;
            _attackDelay = 2500;
            _affectDMGDelay = 600.0f;
            _hpEscapeValue = 15;

            #region DecisionTree & AiComponentInitialize

            _distanceNode.DecisionEvent += EnemyClose;
            _hpNode.DecisionEvent += IsMyHP;
            _canAttackNode.DecisionEvent += CanAtack;
            _distance2Node.DecisionEvent += EscapeOrChase;
            _attackNode.ActionEvent += AttackPlayer;
            _chaseNode.ActionEvent += GoToPlayer;
            _standNode.ActionEvent += StandStill;
            _escapeNode.ActionEvent += Escape;
            _escapeNattackNode.ActionEvent += EscapeNAttack;

            _distanceNode.TrueChild = _hpNode;
            _distanceNode.FalseChild = _standNode;

            _hpNode.TrueChild = _canAttackNode;
            _hpNode.FalseChild = _escapeNode;

            _canAttackNode.TrueChild = _attackNode;
            _canAttackNode.FalseChild = _distance2Node;

            _distance2Node.TrueChild = _escapeNattackNode;
            _distance2Node.FalseChild = _chaseNode;


            AIComponent.MyDTree.DTreeStart = _distanceNode;
            #endregion
        }

        protected override void MakeDead(PlayerScript player)
        {
            if (player != null) player.Stats.Experience.Increase(100);
            _attackTriggerObject.Enabled = false;
            _fieldOfView.Enabled = false;
            base.MakeDead(player);
        }

        public override void Update(GameTime gameTime)
        {
            if (_hp > 0)
            {
                base.Update(gameTime);
                _attackTriggerObject.Update(gameTime);
                _fieldOfView.Update(gameTime);
                Vector3 dir;
                if (_pushed)
                {
                    _pushTimer += (gameTime.ElapsedGameTime.Milliseconds / 1000f);
                    _gameObject.transform.Position += _pushValue;
                    if (_pushTimer > 1.0f) _pushed = false;
                }
                else
                {
                    switch (_currentAction)
                    {
                        // TODO fix border points b/w attack & idle - now is flickering
                        case RangeAction.EscapeNAttack:
                            dir = gameObject.transform.Position - AISystem.Player.transform.Position;
                            Random rand = new Random();
                            int x = rand.Next(0, 100);
                            int y = rand.Next(0, 100);
                            if (_hp < _hpEscapeValue) SetLookVector(dir);
                            else SetLookVector(-dir);
                            dir.X *= x / 100.0f;
                            dir.Z *= y / 100.0f;
                            UnitVelocity = new Vector2(dir.X, dir.Z) * 0.02f;
                            _attackTimer += gameTime.ElapsedGameTime.Milliseconds;
                            if (_attackTimer > _attackDelay)
                            {
                                _attackTimer = 0.0f;
                                //gameObject.animator.Attack();
                                _attackFlag = true;
                                _affectDMGTimer = 0.0f;
                            }
                            if(_attackFlag)
                            {
                                _affectDMGTimer += gameTime.ElapsedGameTime.Milliseconds;
                                if(_affectDMGTimer > _affectDMGDelay)
                                {
                                    _attackTriggerObject.collision.Enabled = true;
                                    foreach (GameObject go in PhysicsSystem.CollisionObjects)
                                    {
                                        if (_attackTriggerObject != go && go.collision.Enabled && _attackTriggerObject.collision.MainCollider.Contains(go.collision.MainCollider) != ContainmentType.Disjoint)
                                        {
                                            _attackTriggerObject.collision.ChceckCollisionDeeper(go);
                                        }
                                    }
                                    _attackTriggerObject.collision.Enabled = false;
                                    _attackFlag = false;
                                }
                            }
                            break;
                        case RangeAction.Attack:
                            dir = AISystem.Player.transform.Position - gameObject.transform.Position;
                            UnitVelocity = Vector2.Zero;
                            SetLookVector(dir);
                            _attackTimer += gameTime.ElapsedGameTime.Milliseconds;
                            if (_attackTimer > _attackDelay)
                            {
                                _attackTimer = 0.0f;
                                gameObject.animator.Attack();
                                _attackFlag = true;
                                _affectDMGTimer = 0.0f;
                            }
                            if(_attackFlag)
                            {
                                _affectDMGTimer += gameTime.ElapsedGameTime.Milliseconds;
                                if(_affectDMGTimer > _affectDMGDelay)
                                {
                                    _attackTriggerObject.collision.Enabled = true;
                                    foreach (GameObject go in PhysicsSystem.CollisionObjects)
                                    {
                                        if (_attackTriggerObject != go && go.collision.Enabled && _attackTriggerObject.collision.MainCollider.Contains(go.collision.MainCollider) != ContainmentType.Disjoint)
                                        {
                                            _attackTriggerObject.collision.ChceckCollisionDeeper(go);
                                        }
                                    }
                                    _attackTriggerObject.collision.Enabled = false;
                                    _attackFlag = false;
                                }
                            }
                            break;
                        case RangeAction.Chase:
                            dir = AISystem.Player.transform.Position - gameObject.transform.Position;
                            SetLookVector(dir);
                            //Vector3 chaseDir = Vector3.Lerp(_chaseStartPosition, AISystem.Player.transform.Position, _chaseTimer * ChaseSpeed);
                            UnitVelocity = new Vector2(dir.X, dir.Z) * ChaseSpeed;
                            break;
                        case RangeAction.Escape:
                            dir = gameObject.transform.Position - AISystem.Player.transform.Position;
                            //Random rand2 = new Random();
                            //int x2 = rand2.Next(0, 100);
                            //int y2 = rand2.Next(0, 100);
                            if (_hp < _hpEscapeValue) SetLookVector(dir);
                            else SetLookVector(-dir);
                            //dir.X *= x2 / 100.0f;
                            //dir.Z *= y2 / 100.0f;
                            UnitVelocity = Vector2.Normalize(new Vector2(dir.X, dir.Z)) * ChaseSpeed * 100f;
                            break;
                        case RangeAction.Stay:
                            UnitVelocity = Vector2.Zero;
                            break;
                    }
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            _attackTriggerObject.Draw(gameTime);
            _fieldOfView.Draw(gameTime);
        }

        public override Component Copy(GameObject newOwner)
        {
            return new EnemyRangedScript(newOwner);
        }

        private bool EnemyClose()
        {
            if (Vector3.Distance(gameObject.transform.Position, AISystem.Player.transform.Position) < 130.0f)
            {
                foreach (GameObject go in PhysicsSystem.CollisionObjects)
                {
                    if (_fieldOfView != go && go.collision.Enabled && _fieldOfView.collision.MainCollider.Contains(go.collision.MainCollider) != ContainmentType.Disjoint)
                    {
                        _fieldOfView.collision.ChceckCollisionDeeper(go);
                    }
                }
                return _enemySeen;
            }
            else return false;
        }

        private bool CanAtack()
        {
            float dist = Math.Abs(Vector3.Distance(gameObject.transform.Position, AISystem.Player.transform.Position));
            return (dist > AttackRange -5 && dist < AttackRange + 5);
        }

        private bool EscapeOrChase()
        {
            float dist = Math.Abs(Vector3.Distance(gameObject.transform.Position, AISystem.Player.transform.Position));
            if (dist + 5 > AttackRange) return false;
            return true;
        }

        private void EscapeNAttack()
        {
            _currentAction = RangeAction.EscapeNAttack;
        }

        private void AttackPlayer()
        {
            _currentAction = RangeAction.Attack;
        }

        private void GoToPlayer()
        {
            _currentAction = RangeAction.Chase;
        }

        private void StandStill()
        {
            _currentAction = RangeAction.Stay;
        }

        private void Escape()
        {
            _currentAction = RangeAction.Escape;
        }
        #endregion

        enum RangeAction
        {
            Chase = 0,
            Attack,
            Escape,
            Stay,
            EscapeNAttack
        }
    }

}

