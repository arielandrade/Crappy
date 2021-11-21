using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Crappy.Pieces;

//TODO: Organize this class with sections for uci and non uci methods
namespace Crappy.UCI
{
    /// <summary>
    /// Handles all UCI communication
    /// </summary>
    public class UCIHandler
    {
        private Engine _engine;
        public Engine Engine
        {
            get => _engine ?? throw new NullReferenceException("No engine set");
            set
            {
                _engine = value;
                _engine.OnInfo = SendInfo;
            }
        }

        private readonly List<string> PositionHistory = new List<string>(); 
                
        private Position _currentPosition;
        private Position CurrentPosition 
        {
            get => _currentPosition ?? throw new InvalidOperationException("No position set");
            set 
            {
                _currentPosition = value;
                PositionHistory.Add(value.RemoveCounters());
            }
        }

        private bool _quit = false;

        private readonly string LogFileName = $@"./log/uci{DateTime.Now.ToString("yyyyMMddhhmmssff")}.log";

        public string[] ProcessCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            string[] tokens = input.Split(' ');
            string command = tokens.FirstOrDefault().ToLower();
            string[] parameters = tokens.Skip(1).ToArray();
            string parametersLine = string.Join(" ", parameters); //Case sensitive
            parameters = parameters.Select(x => x.ToLower()).ToArray(); //Case insensitive

            switch (command)
            {
                case "uci":
                    return Initialize();

                case "isready":
                    return new[] { "readyok" };

                case "ucinewgame":
                    return null;

                case "position":
                    SetPosition(parametersLine);
                    return null;

                case "go":
                    return Search(parameters);

                case "stop":
                    return null;

                case "setoption":
                    return SetOption(parameters);

                case "quit":
                    _quit = true;
                    return null;

                //Custom non-UCI commands
                case "nuci":
                    return NonUCI(parameters);

                default:
                    return new [] { $"Unknown command: {command}" };
            }
        }

        private string[] Initialize()
        {
            IEnumerable<string> GetOptions()
            {
                foreach (Setting setting in Configuration.Get().Settings.Values)
                {
                    string item = 
                        $"option name {setting.SettingType.ToString()} " + 
                        $"type {setting.OptionType.ToString().ToLower()} default {setting.Default} ";

                    if (setting.Minimum != null) 
                    {
                        item += $"min {setting.Minimum} ";
                    }

                    if (setting.Maximum != null) 
                    {
                        item += $"max {setting.Maximum} ";
                    }

                    yield return item;
                }
            }

            return
                new[]
                {
                    $"id author {Engine.AUTHOR}",
                    $"id name {Engine.NAME}",
                }.
                Concat(GetOptions()).
                Append("uciok").
                ToArray();
        }
                
        private void PlayMove(string move)
        {
            CurrentPosition = CurrentPosition.PlayMove(UCIParser.ParseMove(CurrentPosition, move)); 
        }
        
        private void SetPosition(string parameters)
        {
            try
            {
                PositionHistory.Clear();
                
                string[] sections = parameters.Trim().Split(" moves ");

                string fen =
                    sections.First() == "startpos" ?
                    FEN.START :
                    string.Join(string.Empty, sections.First().Split("fen ", StringSplitOptions.RemoveEmptyEntries));

                CurrentPosition = FEN.Parse(fen);
                
                if (sections.Length == 2)
                {
                    foreach (string move in sections[1].Split(' '))
                    {
                        PlayMove(move);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error setting position: {parameters}", ex);
            }
        }

        private string GetParameterValue(IList<string> parameterList, string parameterName)
        {
            int parameterIndex = parameterList.IndexOf(parameterName);

            return 
                parameterIndex < 0 ?
                null : 
                parameterList.ElementAtOrDefault(parameterIndex + 1);
        }

        private int? GetIntParameterValue(IList<string> parameterList, string parameterName)
        {
            string value = GetParameterValue(parameterList, parameterName);
            return int.TryParse(value, out int result) ? result : (int?)null;
        }

        private (IEnumerable<Move> Moves, IList<string> RemainingParameters) GetSearchMoves(IList<string> parameters)
        {
            int startIndex = parameters.IndexOf("searchmoves");

            if (startIndex < 0)
            {
                return (Moves: CurrentPosition.GetLegalMoves(), RemainingParameters: parameters);
            }
            else
            {
                var moves = new List<Move>();

                foreach (string token in parameters.Skip(startIndex + 1))
                {
                    try
                    {
                        moves.Add(UCIParser.ParseMove(CurrentPosition, token));
                    }
                    catch
                    {
                        break;
                    }
                }

                List<string> remainingParameters = parameters.ToList();
                remainingParameters.RemoveRange(startIndex, moves.Count);

                return (moves, remainingParameters);
            }
        }

        private string[] Search(IList<string> parameters)
        {
            (IEnumerable<Move> moves, IList<string> remainingParameters) = GetSearchMoves(parameters);

            bool infinite = remainingParameters.Contains("infinite");
            int? moveTime = GetIntParameterValue(remainingParameters, "movetime");
            int? whiteTime = GetIntParameterValue(remainingParameters, "wtime");
            int? blackTime = GetIntParameterValue(remainingParameters, "btime");
            int? whiteIncrement = GetIntParameterValue(remainingParameters, "winc");
            int? blackIncrement = GetIntParameterValue(remainingParameters, "winc");
            int? depth = GetIntParameterValue(remainingParameters, "depth");

            (Move move, decimal score) = Engine.Search(
                CurrentPosition,
                moves, 
                PositionHistory, 
                infinite, 
                moveTime, 
                whiteTime, 
                blackTime, 
                whiteIncrement, 
                blackIncrement, 
                depth);

            SendInfo($"Real score: {score * CurrentPosition.SideToMove.Sign()}");
            
            int centipawns = Math.Max(Convert.ToInt32(score * 100), -800);
            
            return new[]
            {
                $"info score cp {centipawns}",
                $"bestmove {UCIParser.MoveToString(move)}"
            };
        }

        private void SendInfo(string info)
        {
            WriteResponse($"info string {info}");
        }

        private string[] SetOption(string[] parameters)
        {
            string settingName = parameters.ElementAtOrDefault(1) ?? throw new ArgumentException("No setting specified");
            string settingValue = GetParameterValue(parameters, "value");

            try
            {
                try
                {
                    Setting setting = Configuration.Get().Settings.Values.SingleOrDefault(x => x.SettingType.ToString().ToLower() == settingName);

                    if (setting is null)
                    {
                        return new[] { $"info string Unknown option: {settingName}" };
                    }

                    setting.Value = settingValue;

                    return new string[] { $"info string {settingName} set to {settingValue}" };
                }
                finally
                {
                    Configuration.Get().Save();
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid value for {settingName}: {settingValue}", ex);
            }
        }

        private string[] GetASCIIBoard() =>
            CurrentPosition?.Board.
            Select(rank => string.Join(string.Empty, rank.Select(piece => piece?.ToString() ?? " "))).
            Reverse().
            ToArray();

        private void WriteResponse(string response) => WriteResponse(new[] { response });

        private void WriteResponse(string[] response)
        {
            if (response != null)
            {
                foreach (string line in response)
                {
                    Log(line);
                    Console.WriteLine(line);
                }
            }
        }

        private string[] GetCommandExceptionResponse(string command, Exception exception)
        {
            string[] GetExceptionMessageTrace(Exception ex) =>
                ex is null ?
                new string[] { } :
                new[] { ex.Message }.Concat(GetExceptionMessageTrace(ex?.InnerException)).ToArray();

            return
                new[]
                {
                    "Error processing command:",
                    $"\t* {command}",
                    "Message trace:"
                }.
                Concat(
                    GetExceptionMessageTrace(exception).
                    Select(x => $"\t* {x}")).
                Concat(
                    new[]
                    {
                        "Stack trace:",
                        $"\t* {exception.StackTrace}"
                    }).
                ToArray();
        }

        private void ReplyCommand(string command)
        {
            string[] response;

            try
            {
                response = ProcessCommand(command);
            }
            catch (Exception ex)
            {
                response = GetCommandExceptionResponse(command, ex);
            }

            if (response != null) 
            {
                WriteResponse(response.Append(string.Empty).ToArray());
            }            
        }

        private bool? _logToFile;
        private readonly object _logLock = new object();
        private bool _logDirectoryCreated = false;
       
        private void Log(string line)
        {
            if (_logToFile is null)
            {
                _logToFile = Configuration.Get().GetValue<bool>(SettingType.LogToFile);
            }

            if (_logToFile.Value)
            {
                lock (_logLock)
                {
                    if (!_logDirectoryCreated)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(LogFileName));
                        _logDirectoryCreated = true;
                    }

                    File.AppendAllLines(LogFileName, new[] { line });
                }
            }
        }

        public void Loop()
        {
            while (!_quit)
            {
                string command = Console.ReadLine();
                Log(command);
                ReplyCommand(command);
            }
        }

        private string[] NonUCI(string[] parameters)
        {
            if (!parameters.Any())
            {
                return new[] { "Non UCI command not supplied" };
            }
            
            switch (parameters.First())
            {
                case "fen":
                    return new[] { CurrentPosition?.ToString() };

                case "board":
                    return GetASCIIBoard();

                case "flatscore":
                    return GetFlatScore(parameters[1]);

                case "script":
                    RunScript(parameters[1]);
                    return null;

                case "tree":
                    return GetTree(parameters.Skip(1).ToArray());
                
                case "clearcache":
                    Engine.ClearCache();
                    return null;
                
                default:
                    return new[] { $"Unknown non UCI command: {parameters.First()}" };

            }
        }

        private void RunCommand(string command)
        {
            WriteResponse($"Executing command '{command}'...");
            ReplyCommand(command);
        }

        private void RunScript(string fileName)
        {
            if (File.Exists(fileName)) 
            {
                string[] script = File.ReadAllLines(fileName);

                WriteResponse("Running script...");

                foreach (string command in script.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    RunCommand(command);
                }

                WriteResponse("Script ended.");
            }
            else
                throw new FileNotFoundException($"File '{fileName}' not found");
        }

        private string[] GetFlatScore(string parameter)
        {
            switch (parameter)
            {
                case "moves":
                    var result = new List<string>();
                    
                    foreach(Move move in CurrentPosition.GetAllMoves(CurrentPosition.SideToMove))
                    {
                        result.Add($"{move}: {Engine.FlatScore(CurrentPosition.PlayMove(move))}");
                    }

                    return result.ToArray();
                
                case "position":
                    return new[] { $"Score: {Engine.FlatScore(CurrentPosition)}" };

                default:
                    return new[] { $"Unknown parameter '{parameter}'" };
            }
        }

        private (Move move, Position position)[] GetBranches(Position sourcePosition, int remainingDepth)
        {
            remainingDepth--;

            return
                sourcePosition.
                GetLegalMoves().
                ToArray(). //!
                Select(x => (move: x, position: sourcePosition.PlayMove(x))).
                SelectMany(
                    current => 
                        remainingDepth == 0 ? 
                        new[] { current } : 
                        GetBranches(current.position, remainingDepth)).
                ToArray();
        }

        private string[] GetTree(string[] parameters)
        {
            int depth = GetIntParameterValue(parameters, "depth") ?? Engine.Depth;

            Task<(Move move, Position position)[]>[] taskList =
                CurrentPosition.
                GetLegalMoves().
                Select(x => Task.Run(() => GetBranches(CurrentPosition.PlayMove(x), depth - 1))).
                ToArray();

            (Move move, Position position)[] list = taskList.SelectMany(x => x.Result).ToArray();

            int count = list.Length;

            return
                new[]
                {
                    $"Depth: {depth}",
                    $"Moves: {count}"
                };
        }
    }
}