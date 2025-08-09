using UnityEngine;
using Helloop.StateMachines;
using Helloop.Enemies.States;

namespace Helloop.Enemies
{
    public class EnemyStateMachine
    {
        private StateMachine<Enemy> stateMachine;
        private StateTransitionValidator<Enemy> transitionValidator;
        private Enemy owner;

        public IState<Enemy> CurrentState => stateMachine.CurrentState;
        public bool IsTransitioning => stateMachine.IsTransitioning;

        public EnemyStateMachine(Enemy enemy)
        {
            owner = enemy;
            stateMachine = new StateMachine<Enemy>(enemy);
            transitionValidator = new StateTransitionValidator<Enemy>();
            SetupTransitions();
        }

        private void SetupTransitions()
        {
            transitionValidator.AddTransition(new StateTransition<Enemy>(
                typeof(EnemyIdleState), typeof(EnemyPatrolState),
                enemy => enemy.ShouldStartPatrolling(),
                "Idle to Patrol"
            ));

            transitionValidator.AddTransition(new StateTransition<Enemy>(
                typeof(EnemyPatrolState), typeof(EnemyChaseState),
                enemy => enemy.CanSeePlayer(),
                "Patrol to Chase"
            ));

            transitionValidator.AddTransition(new StateTransition<Enemy>(
                typeof(EnemyChaseState), typeof(EnemyAttackState),
                enemy => enemy.IsInAttackRange(),
                "Chase to Attack"
            ));

            transitionValidator.AddTransition(new StateTransition<Enemy>(
                typeof(EnemyAttackState), typeof(EnemyChaseState),
                enemy => !enemy.IsCurrentlyAttacking && !enemy.IsInAttackRange() && enemy.CanSeePlayer(),
                "Attack to Chase"
            ));

            transitionValidator.AddTransition(new StateTransition<Enemy>(
                typeof(EnemyChaseState), typeof(EnemyReturnState),
                enemy => enemy.ShouldReturn(),
                "Chase to Return"
            ));

            transitionValidator.AddTransition(new StateTransition<Enemy>(
                typeof(EnemyAttackState), typeof(EnemyReturnState),
                enemy => !enemy.IsCurrentlyAttacking && enemy.ShouldReturn(),
                "Attack to Return"
            ));

            transitionValidator.AddTransition(new StateTransition<Enemy>(
                typeof(EnemyReturnState), typeof(EnemyPatrolState),
                enemy => enemy.HasReturnedToBase,
                "Return to Patrol"
            ));

            transitionValidator.AddTransition(new StateTransition<Enemy>(
                typeof(EnemyReturnState), typeof(EnemyChaseState),
                enemy => enemy.CanSeePlayer() && !enemy.ShouldReturn(),
                "Return to Chase"
            ));
        }

        public void Initialize()
        {
            stateMachine.ChangeState(new EnemyIdleState());
        }

        public void Update()
        {
            CheckForStateTransitions();
            stateMachine.Update();
        }

        private void CheckForStateTransitions()
        {
            if (owner.IsDead)
            {
                if (!stateMachine.IsInState<EnemyDeathState>())
                {
                    ForceChangeState(new EnemyDeathState());
                }
                return;
            }

            IState<Enemy> validTransition = transitionValidator.GetValidTransition(owner, stateMachine.CurrentState);
            if (validTransition != null)
            {
                stateMachine.ChangeState(validTransition);
            }
        }

        public void ForceChangeState(IState<Enemy> newState)
        {
            stateMachine.ForceState(newState);
        }
    }
}