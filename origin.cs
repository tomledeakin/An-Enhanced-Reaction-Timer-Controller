using SimpleReactionMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleReactionMachine
{
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
        private const int MAX_READY_TIME = 1000; // Maximum time in ready without pressing Go/Stop
        private const int MIN_WAIT_TIME = 100; // Minimum wait time, 1 sec in ticks
        private const int MAX_WAIT_TIME = 250; // Maximum wait time, 2.5 sec in ticks
        private const int MAX_GAME_TIME = 200; // Maximum of 2 sec to react, in ticks
        private const int GAMEOVER_TIME = 300; // Display result for 3 sec, in ticks
        private const int RESULTS_TIME = 500; // Display average time for 5 sec, in ticks
        private const double TICKS_PER_SECOND = 100.0; // Based on 10 ms ticks

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
                    _gui.SetDisplay("Average: " + (((double)_totalReactionTime / _games) / TICKS_PER_SECOND).ToString("0.00"));
                    break;
                default:
                    break;
            }
        }
    }
}
