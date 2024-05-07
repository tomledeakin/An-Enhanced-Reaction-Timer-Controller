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

    public enum State
    {
        On,
        Ready,
        Wait,
        Running,
        GameOver,
        Results
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

        // Instance variables and properties
        private State _state;
        private IGui _gui;
        private IRandom _rng;
        private int _ticks;
        private int _games;
        private int _totalReactionTime;
        private bool _gameFinished;
        private int _waitTime;

        public void Connect(IGui gui, IRandom rng)
        {
            _gui = gui;
            _rng = rng;
            Init();
        }

        public void Init()
        {
            Next(State.On);
        }

        public void CoinInserted()
        {
            switch (_state)
            {
                case State.On:
                    Next(State.Ready);
                    return;
                default:
                    return;
            }
        }

        public void GoStopPressed()
        {
            _gameFinished = false;
            switch (_state)
            {
                case State.Ready:
                    Next(State.Wait);
                    return;
                case State.Wait:
                    Next(State.On);
                    return;
                case State.Running:
                    if (!_gameFinished)
                    {
                        _totalReactionTime += _ticks;
                        _gameFinished = true;
                    }
                    Next(State.GameOver);
                    return;
                case State.GameOver:
                    CheckGames();
                    return;
                case State.Results:
                    Next(State.On);
                    return;
                default:
                    return;
            }
        }

        public void CheckGames()
        {
            if (_games == 3)
            {
                Next(State.Results);
                return;
            }
            Next(State.Wait);
        }

        public void Tick()
        {
            _gameFinished = false;
            switch (_state)
            {
                case State.Ready:
                    _ticks++;
                    if (_ticks == MAX_READY_TIME)
                        Next(State.On);
                    return;
                case State.Wait:
                    _ticks++;
                    if (_ticks == _waitTime)
                    {
                        _games++;
                        Next(State.Running);
                    }
                    return;
                case State.Running:
                    _ticks++;
                    _gui.SetDisplay((_ticks / TICKS_PER_SECOND).ToString("0.00"));
                    if (_ticks == MAX_GAME_TIME)
                    {
                        if (!_gameFinished)
                        {
                            _totalReactionTime += _ticks;
                            _gameFinished = true;
                        }
                        Next(State.GameOver);
                    }
                    return;
                case State.GameOver:
                    _ticks++;
                    if (_ticks == GAMEOVER_TIME)
                        CheckGames();
                    return;
                case State.Results:
                    _ticks++;
                    if (_ticks == RESULTS_TIME)
                        Next(State.On);
                    return;
                default:
                    return;
            }
        }

        void Next(State state)
        {
            _state = state;
            _ticks = 0;

            switch (_state)
            {
                case State.On:
                    _gui.SetDisplay("Insert coin");
                    _games = 0;
                    _totalReactionTime = 0;
                    break;
                case State.Ready:
                    _gui.SetDisplay("Press GO!");
                    break;
                case State.Wait:
                    _waitTime = _rng.GetRandom(MIN_WAIT_TIME, MAX_WAIT_TIME);
                    _gui.SetDisplay("Wait...");
                    break;
                case State.Running:
                    _gui.SetDisplay("0.00");
                    break;
                case State.Results:
                    _gui.SetDisplay("Average: "
                    + (((double)_totalReactionTime / _games) / TICKS_PER_SECOND)
                    .ToString("0.00"));
                    break;
                default:
                    break;
            }
        }
    }
}