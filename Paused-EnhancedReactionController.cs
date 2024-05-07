using System;
using System.Timers;

namespace SimpleReactionMachine
{
    class SimpleReactionMachine
    {
        const string TOP_LEFT_JOINT = "┌";
        const string TOP_RIGHT_JOINT = "┐";
        const string BOTTOM_LEFT_JOINT = "└";
        const string BOTTOM_RIGHT_JOINT = "┘";
        const string TOP_JOINT = "┬";
        const string BOTTOM_JOINT = "┴";
        const string LEFT_JOINT = "├";
        const string JOINT = "┼";
        const string RIGHT_JOINT = "┤";
        const char HORIZONTAL_LINE = '─';
        const char PADDING = ' ';
        const string VERTICAL_LINE = "│";

        static private IController contoller;
        static private IGui gui;

        static void Main(string[] args)
        {
            // Make a menu
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("{0}{1}{2}", TOP_LEFT_JOINT, new string(HORIZONTAL_LINE, 50), TOP_RIGHT_JOINT);
            Console.WriteLine("{0}{1}{2}", VERTICAL_LINE, new string(' ', 50), VERTICAL_LINE);
            Console.WriteLine("{0}{1}{2}", VERTICAL_LINE, new string(' ', 50), VERTICAL_LINE);
            Console.WriteLine("{0}{1}{2}", VERTICAL_LINE, new string(' ', 50), VERTICAL_LINE);
            Console.WriteLine("{0}{1}{2}", LEFT_JOINT, new string(HORIZONTAL_LINE, 50), RIGHT_JOINT);
            Console.WriteLine("{0}{1}{2}", VERTICAL_LINE, new string(' ', 50), VERTICAL_LINE);
            Console.WriteLine("{0}{1}{2}", VERTICAL_LINE, new string(' ', 50), VERTICAL_LINE);
            Console.WriteLine("{0}{1}{2}", VERTICAL_LINE, new string(' ', 50), VERTICAL_LINE);
            Console.WriteLine("{0}{1}{2}", VERTICAL_LINE, new string(' ', 50), VERTICAL_LINE);
            Console.WriteLine("{0}{1}{2}", VERTICAL_LINE, new string(' ', 50), VERTICAL_LINE);
            Console.WriteLine("{0}{1}{2}", BOTTOM_LEFT_JOINT, new string(HORIZONTAL_LINE, 50), BOTTOM_RIGHT_JOINT);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.SetCursorPosition(5, 6);
            Console.Write("{0,-20}", "- For Insert Coin press SPACE");
            Console.SetCursorPosition(5, 7);
            Console.Write("{0,-20}", "- For Go/Stop action press ENTER");
            Console.SetCursorPosition(5, 8);
            Console.Write("{0,-20}", "- For Exit press ESC");

            // Create a time for Tick event
            Timer timer = new Timer(10);
            // Hook up the Elapsed event for the timer. 
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;

            // Connect GUI with the Controller and vice versa
            contoller = new EnhancedReactionController();
            gui = new Gui();
            gui.Connect(contoller);
            contoller.Connect(gui, new RandomGenerator());

            //Reset the GUI
            gui.Init();
            // Start the timer
            timer.Enabled = true;

            // Run the menu
            bool quitePressed = false;
            while (!quitePressed)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        contoller.GoStopPressed();
                        break;
                    case ConsoleKey.Spacebar:
                        contoller.CoinInserted();
                        break;
                    case ConsoleKey.Escape:
                        quitePressed = true;
                        break;
                    case ConsoleKey.P: // Assigning the 'P' key to pause
                        contoller.PausePressed();
                        break;
                }
            }

        }

        // This event occurs every 10 msec
        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            contoller.Tick();
        }

        // Internal implementation of Random Generator
        private class RandomGenerator : IRandom
        {
            Random rnd = new Random(100);

            public int GetRandom(int from, int to)
            {
                return rnd.Next(from) + to;
            }
        }

        // Internal implementation of GUI
        private class Gui : IGui
        {
            private IController controller;
            public void Connect(IController controller)
            {
                this.controller = controller;
            }

            public void Init()
            {
                SetDisplay("Start your game!");
            }

            public void SetDisplay(string text)
            {
                PrintUserInterface(text);
            }

            private void PrintUserInterface(string text)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.SetCursorPosition(15, 2);
                Console.Write("{0,-20}", text);
                Console.SetCursorPosition(0, 10);
            }
        }
    }

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

        public void PausePressed()
        {
            _state.PausePressed();
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
            public abstract void PausePressed();
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
            public override void PausePressed() { }
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
            public override void PausePressed() { }
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
            public override void PausePressed()
            {
                controller.SetState(new PausedState(controller));
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

            public override void PausePressed() { }

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

        class PausedState : State
        {
            public PausedState(EnhancedReactionController con) : base(con)
            {
                controller.Gui.SetDisplay("Paused");
            }

            public override void CoinInserted() { }
            public override void GoStopPressed() { }
            public override void PausePressed()
            {
                controller.SetState(new WaitState(controller));
            }
            public override void Tick() { }
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
            public override void PausePressed() { }
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
            public override void PausePressed() { }
            public override void Tick()
            {
                controller.Ticks++;
                if (controller.Ticks == RESULTS_TIME)
                    controller.SetState(new OnState(controller));
            }
        }
    }
}
