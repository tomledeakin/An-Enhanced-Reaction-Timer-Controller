using SimpleReactionMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleReactionMachine
{
    public class EnhancedReactionController : IController
    {
        // Settings for the game times
        private const int MAX_READY_TIME = 1000; // Maximum time in ready without pressing GoStop
        private const int MIN_WAIT_TIME = 100;   // Minimum wait time, 1 sec in ticks
        private const int MAX_WAIT_TIME = 250;   // Maximum wait time, 2.5 sec in ticks
        private const int MAX_GAME_TIME = 200;   // Maximum of 2 sec to react, in ticks
        private const int GAMEOVER_TIME = 300;   // Display result for 3 sec, in ticks
        private const int RESULTS_TIME = 500;    // Display average time for 5 sec, in ticks
        private const double TICKS_PER_SECOND = 100.0; // Based on 10ms ticks

        private State _state;
        private IGui Gui { get; set; }
        private IRandom Rng { get; set; }
        private int Ticks { get; set; }
        private int Games { get; set; }
        private int TotalReactionTime { get; set; }

        public void Connect(IGui gui, IRandom rng)
        {
            Gui = gui;
            Rng = rng;
            Init();
        }

        public void Init()
        {
            _state = new OnState(this);
        }

        public void CoinInserted()
        {
            _state.CoinInserted();
        }

        public void GoStopPressed()
        {
            _state.GoStopPressed();
        }

        public void Tick()
        {
            _state.Tick();
        }

        void SetState(State state)
        {
            _state = state;
        }

        public void PrintGames()
        {
            Console.WriteLine("Current game count: " + Games.ToString());
        }

        abstract class State
        {
            protected EnhancedReactionController controller;
            public State(EnhancedReactionController con)
            {
                controller = con;
            }
            public abstract void CoinInserted();
            public abstract void GoStopPressed();
            public abstract void Tick();
        }

        class OnState : State
        {
            public OnState(EnhancedReactionController con) : base(con)
            {
                controller.Games = 0;
                controller.TotalReactionTime = 0;
                controller.Gui.SetDisplay("Insert coin");
            }
            public override void CoinInserted()
            {
                controller.SetState(new ReadyState(controller));
            }
            public override void GoStopPressed() { }
            public override void Tick() { }
        }

        class ReadyState : State
        {
            public ReadyState(EnhancedReactionController con) : base(con)
            {
                controller.Gui.SetDisplay("Press Go!");
                controller.Ticks = 0;
            }
            public override void CoinInserted() { }
            public override void GoStopPressed()
            {
                controller.SetState(new WaitState(controller));
            }
            public override void Tick()
            {
                controller.Ticks++;
                if (controller.Ticks == MAX_READY_TIME)
                    controller.SetState(new OnState(controller));
            }
        }

        class WaitState : State
        {
            private int _waitTime;
            public WaitState(EnhancedReactionController con) : base(con)
            {
                controller.Gui.SetDisplay("Wait...");
                controller.Ticks = 0;
                _waitTime = controller.Rng.GetRandom(MIN_WAIT_TIME, MAX_WAIT_TIME);
            }
            public override void CoinInserted() { }
            public override void GoStopPressed()
            {
                controller.SetState(new OnState(controller));
            }
            public override void Tick()
            {
                controller.Ticks++;
                if (controller.Ticks == _waitTime)
                {
                    controller.Games++;
                    controller.SetState(new RunningState(controller));
                }
            }
        }

        class RunningState : State
        {
            private bool gameFinished = false;

            public RunningState(EnhancedReactionController con) : base(con)
            {
                controller.Gui.SetDisplay("0.00");
                controller.Ticks = 0;
            }

            public override void CoinInserted() { }

            public override void GoStopPressed()
            {
                if (!gameFinished)
                {
                    controller.TotalReactionTime += controller.Ticks;
                    gameFinished = true;
                }
                controller.SetState(new GameOverState(controller));
            }

            public override void Tick()
            {
                controller.Ticks++;
                controller.Gui.SetDisplay(
                    (controller.Ticks / TICKS_PER_SECOND).ToString("0.00"));
                if (controller.Ticks == MAX_GAME_TIME)
                {
                    if (!gameFinished)
                    {
                        controller.TotalReactionTime += controller.Ticks;
                        gameFinished = true;
                    }
                    controller.SetState(new GameOverState(controller));
                }
            }
        }


        class GameOverState : State
        {
            public GameOverState(EnhancedReactionController con) : base(con)
            {
                controller.Ticks = 0;
            }
            public override void CoinInserted() { }
            public override void GoStopPressed()
            {
                CheckGames();
            }
            public override void Tick()
            {
                controller.Ticks++;
                if (controller.Ticks == GAMEOVER_TIME)
                    CheckGames();
            }
            private void CheckGames()
            {
                if (controller.Games == 3)
                {
                    controller.SetState(new ResultsState(controller));
                    return;
                }
                controller.SetState(new WaitState(controller));
            }
        }

        class ResultsState : State
        {
            public ResultsState(EnhancedReactionController con) : base(con)
            {
                controller.Gui.SetDisplay("Average: " +
                    (((double)controller.TotalReactionTime / controller.Games) / TICKS_PER_SECOND)
                    .ToString("0.00"));
                controller.Ticks = 0;
            }
            public override void CoinInserted() { }
            public override void GoStopPressed()
            {
                controller.SetState(new OnState(controller));
            }
            public override void Tick()
            {
                controller.Ticks++;
                if (controller.Ticks == RESULTS_TIME)
                    controller.SetState(new OnState(controller));
            }
        }
    }
}
