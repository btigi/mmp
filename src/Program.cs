/*
Playlist
  always show
Progress?
Starting volume
Handles args to play passed in music
When adding music if we specify a directory (or a directory then \* ?) just add all supported files (and sub directories...?)
*/

using Microsoft.Extensions.Configuration;
using mmp.Model;
using NAudio.Wave;
using System.Drawing;
using System.Text;

namespace mmp
{
    static class Program
    {
        static bool playMusic = false;
        static bool pauseMusic = false;
        static IWavePlayer waveOut;
        static AudioFileReader audioFileReader;
        static string filePath = string.Empty;
        static string currentSong = "none";
        static float volume = 0.5f;
        static int currentTrackIndex = 0;
        static RepeatMode RepeatMode;
        static readonly List<string> playlist = [];
        static readonly char[] trimChars = ['"', ' '];
        static bool swapped = false;

        static ConsoleColor currentTextForegroundConsoleColour;
        static ConsoleColor currentTextBackgroundConsoleColour;
        static ConsoleColor currentSongForegroundConsoleColour;
        static ConsoleColor currentSongBackgroundConsoleColour;
        static ConsoleColor volumeTextForegroundConsoleColour;
        static ConsoleColor volumeTextBackgroundConsoleColour;
        static ConsoleColor volumeForegroundConsoleColour;
        static ConsoleColor volumeBackgroundConsoleColour;
        static ConsoleColor textForegroundConsoleColour;
        static ConsoleColor textBackgroundConsoleColour;
        static ConsoleColor playlistForegroundConsoleColour;
        static ConsoleColor playlistBackgroundConsoleColour;

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                            .AddJsonFile($"appsettings.json", true, true);
            var configuration = builder.Build();
            var settings = new AppSettings();
            configuration.Bind(settings);

            var titleForegroundColour = Color.FromName(settings.TitleForegroundColour ?? "Red");
            var titleForegroundConsoleColour = FromColour(titleForegroundColour);

            var titleBackgroundColour = Color.FromName(settings.TitleBackgroundColour ?? "Black");
            var titleBackgroundConsoleColour = FromColour(titleBackgroundColour);

            var currentTextForegroundColour = Color.FromName(settings.CurrentTextForegroundColour ?? "White");
            currentTextForegroundConsoleColour = FromColour(currentTextForegroundColour);

            var currentTextBackgroundColour = Color.FromName(settings.CurrentTextBackgroundColour ?? "Black");
            currentTextBackgroundConsoleColour = FromColour(currentTextBackgroundColour);

            var currentSongForegroundColour = Color.FromName(settings.CurrentSongForegroundColour ?? "Blue");
            currentSongForegroundConsoleColour = FromColour(currentSongForegroundColour);

            var currentSongBackgroundColour = Color.FromName(settings.CurrentSongBackgroundColour ?? "Black");
            currentSongBackgroundConsoleColour = FromColour(currentSongBackgroundColour);

            var volumeTextForegroundColour = Color.FromName(settings.VolumeTextForegroundColour ?? "White");
            volumeTextForegroundConsoleColour = FromColour(volumeTextForegroundColour);

            var volumeTextBackgroundColour = Color.FromName(settings.VolumeTextBackgroundColour ?? "Black");
            volumeTextBackgroundConsoleColour = FromColour(volumeTextBackgroundColour);

            var volumeForegroundColour = Color.FromName(settings.VolumeForegroundColour ?? "Blue");
            volumeForegroundConsoleColour = FromColour(volumeForegroundColour);

            var volumeBackgroundColour = Color.FromName(settings.VolumeBackgroundColour ?? "Black");
            volumeBackgroundConsoleColour = FromColour(volumeBackgroundColour);

            var textForegroundColour = Color.FromName(settings.TextForegroundColour ?? "White");
            textForegroundConsoleColour = FromColour(textForegroundColour);

            var textBackgroundColour = Color.FromName(settings.TextBackgroundColour ?? "Black");
            textBackgroundConsoleColour = FromColour(textBackgroundColour);

            var playlistForegroundColour = Color.FromName(settings.PlaylistForegroundColour ?? "Yellow");
            playlistForegroundConsoleColour = FromColour(playlistForegroundColour);

            var playlistBackgroundColour = Color.FromName(settings.PlaylistBackgroundColour ?? "Black");
            playlistBackgroundConsoleColour = FromColour(playlistBackgroundColour);

            Console.Title = "My Media Player";
            Console.Clear();

            Console.ForegroundColor = titleForegroundConsoleColour;
            Console.BackgroundColor = titleBackgroundConsoleColour;
            Console.WriteLine("My Media Player");

            Console.ForegroundColor = currentSongForegroundConsoleColour;
            Console.BackgroundColor = currentSongBackgroundConsoleColour;
            Console.WriteLine("Current: none");
            //Console.WriteLine("Progress: 00:00 / 00:00");

            Console.ForegroundColor = volumeTextForegroundConsoleColour;
            Console.BackgroundColor = volumeTextBackgroundConsoleColour;
            Console.Write("Volume: ");
            Console.ForegroundColor = volumeForegroundConsoleColour;
            Console.BackgroundColor = volumeBackgroundConsoleColour;
            Console.WriteLine("50%");

            Console.ForegroundColor = currentTextForegroundConsoleColour;
            Console.BackgroundColor = currentTextBackgroundConsoleColour;
            Console.WriteLine("Enter filename:");
            filePath = ReadLineWithTabCompletion(string.Empty);
            AddToPlaylist(filePath);

            var musicThread = new Thread(PlayMusic);
            musicThread.Start();

            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == (ConsoleKey)settings.Stop)
                {
                    StopMusic();
                }
                else if (key == (ConsoleKey)settings.Play)
                {
                    StartMusic();
                }
                else if (key == (ConsoleKey)settings.PlayPause)
                {
                    PauseOrResumeMusic();
                }
                else if (key == (ConsoleKey)settings.NewSong)
                {
                    ClearPlaylistDisplay();
                    Console.SetCursorPosition(0, 4);
                    ClearLine();
                    Console.ForegroundColor = currentTextForegroundConsoleColour;
                    Console.BackgroundColor = currentTextBackgroundConsoleColour;
                    Console.WriteLine("Enter filename:");
                    var initialDirectory = string.IsNullOrEmpty(filePath) ? string.Empty : Path.GetDirectoryName(filePath) ?? ".";
                    var newFilePath = ReadLineWithTabCompletion(initialDirectory);
                    ClearPlaylist();
                    AddToPlaylist(newFilePath);
                    currentTrackIndex = 0;
                    ChangeMusic(newFilePath);
                }
                else if (key == (ConsoleKey)settings.AddPlaylist)
                {
                    ClearPlaylistDisplay();
                    Console.SetCursorPosition(0, 4);
                    ClearLine();
                    Console.ForegroundColor = currentTextForegroundConsoleColour;
                    Console.BackgroundColor = currentTextBackgroundConsoleColour;
                    Console.WriteLine("Enter filename:");
                    var initialDirectory = string.IsNullOrEmpty(filePath) ? string.Empty : Path.GetDirectoryName(filePath) ?? ".";
                    var newFilePath = ReadLineWithTabCompletion(initialDirectory);
                    AddToPlaylist(newFilePath);
                }
                else if (key == (ConsoleKey)settings.ShowPlaylist)
                {
                    DisplayPlaylist();
                }
                else if (key == (ConsoleKey)settings.TrackPrev)
                {
                    MoveToPreviousTrack();
                }
                else if (key == (ConsoleKey)settings.TrackNext)
                {
                    MoveToNextTrack();
                }
                else if (key == (ConsoleKey)settings.VolumeUp)
                {
                    ChangeVolume(0.1f); // Increase volume by 10%
                }
                else if (key == (ConsoleKey)settings.VolumeDown)
                {
                    ChangeVolume(-0.1f); // Decrease volume by 10%
                }
                else if (key == (ConsoleKey)settings.Repeat)
                {
                    RepeatMode = RepeatMode switch
                    {
                        RepeatMode.NoRepeat => RepeatMode.RepeatTrack,
                        RepeatMode.RepeatTrack => RepeatMode.RepeatPlaylist,
                        RepeatMode.RepeatPlaylist => RepeatMode.NoRepeat,
                        _ => RepeatMode
                    };
                    DisplayCurrentSong();
                }
            }
        }

        static void PlayMusic()
        {
            while (true)
            {
                if (playMusic && !pauseMusic)
                {
                    if (waveOut == null || waveOut.PlaybackState == PlaybackState.Stopped)
                    {
                        while (currentTrackIndex < playlist.Count)
                        {
                            filePath = playlist[currentTrackIndex];

                            if (File.Exists(filePath))
                            {
                                waveOut = new WaveOutEvent();
                                audioFileReader = new AudioFileReader(filePath);
                                waveOut.Init(audioFileReader);
                                waveOut.Volume = volume;
                                waveOut.Play();

                                currentSong = Path.GetFileName(filePath);
                                DisplayCurrentSong();
                                break;
                            }
                            else
                            {
                                currentTrackIndex++;
                            }
                        }

                        if (currentTrackIndex >= playlist.Count)
                        {
                            playMusic = false;
                            continue;
                        }
                    }

                    while (waveOut != null && (waveOut.PlaybackState == PlaybackState.Playing || waveOut.PlaybackState == PlaybackState.Paused))
                    {
                        Thread.Sleep(100);
                        // DisplayProgress();
                    }

                    if (waveOut != null && waveOut.PlaybackState == PlaybackState.Stopped)
                    {
                        waveOut.Dispose();
                        waveOut = null;

                        if (swapped)
                        {
                            swapped = false;
                            DisplayPlaylist();
                            continue;
                        }
                        if (RepeatMode == RepeatMode.NoRepeat)
                        {
                            currentTrackIndex++;
                        }
                        else if (RepeatMode == RepeatMode.RepeatTrack)
                        {
                            // Do nothing, replay current track
                        }
                        else if (RepeatMode == RepeatMode.RepeatPlaylist && currentTrackIndex == playlist.Count - 1)
                        {
                            currentTrackIndex = 0;
                        }
                        else
                        {
                            currentTrackIndex++;
                        }

                        DisplayPlaylist();
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        static void StopMusic()
        {
            playMusic = false;
            waveOut?.Stop();
            waveOut?.Dispose();
            audioFileReader?.Dispose();
            currentSong = "none";
            DisplayCurrentSong();
        }

        static void StartMusic()
        {
            if (waveOut == null || waveOut.PlaybackState == PlaybackState.Stopped)
            {
                playMusic = true;
                pauseMusic = false;
            }
        }

        static void PauseOrResumeMusic()
        {
            if (waveOut?.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Pause();
                pauseMusic = true;
            }
            else if (waveOut?.PlaybackState == PlaybackState.Paused)
            {
                waveOut.Play();
                pauseMusic = false;
            }
        }

        static void ChangeMusic(string newFilePath)
        {
            filePath = newFilePath;
            currentTrackIndex = playlist.IndexOf(newFilePath);
            StopMusic();
            StartMusic();
        }

        static void AddToPlaylist(string newFilePath)
        {
            newFilePath = newFilePath?.Trim(trimChars);
            if (File.Exists(newFilePath))
            {
                if (Path.GetExtension(newFilePath) == ".m3u" || Path.GetExtension(newFilePath) == ".m3u8")
                {
                    var lines = File.ReadAllLines(newFilePath);
                    lines = lines.Where(w => !w.StartsWith('#')).ToArray();
                    foreach (var line in lines)
                    {
                        var directory = Path.GetDirectoryName(newFilePath);
                        var path = Path.Combine(directory, line);
                        if (File.Exists(path))
                        {
                            playlist.Add(path);
                        }
                    }
                }

                if (Path.GetExtension(newFilePath) == ".mp3" || Path.GetExtension(newFilePath) == ".flac")
                {
                    playlist.Add(newFilePath);
                }
            }
        }

        static void ClearPlaylist()
        {
            playlist.Clear();
            currentTrackIndex = 0;
        }

        static void MoveToPreviousTrack()
        {
            if (currentTrackIndex > 0)
            {
                currentTrackIndex--;
                ChangeMusic(playlist[currentTrackIndex]);
                swapped = true;
            }
        }

        static void MoveToNextTrack()
        {
            if (currentTrackIndex < playlist.Count - 1)
            {
                currentTrackIndex++;
                ChangeMusic(playlist[currentTrackIndex]);
                swapped = true;
            }
        }

        static void DisplayCurrentSong()
        {
            Console.ForegroundColor = currentSongForegroundConsoleColour;
            Console.BackgroundColor = currentSongBackgroundConsoleColour;
            var currentTop = Console.CursorTop;
            Console.SetCursorPosition(0, 1);
            Console.Write($"Current: {currentSong} Repeat: {RepeatMode}".PadRight(Console.WindowWidth));
            Console.SetCursorPosition(0, currentTop);
        }

        static void DisplayVolume()
        {
            var currentTop = Console.CursorTop;
            Console.SetCursorPosition(0, 2);
            var volumePercentage = (int)(volume * 100);
            //Console.Write($"Volume: {volumePercentage}%".PadRight(Console.WindowWidth));

            Console.ForegroundColor = volumeTextForegroundConsoleColour;
            Console.BackgroundColor = volumeTextBackgroundConsoleColour;
            Console.Write("Volume: ");
            Console.ForegroundColor = volumeForegroundConsoleColour;
            Console.BackgroundColor = volumeBackgroundConsoleColour;
            Console.WriteLine(volumePercentage);

            Console.SetCursorPosition(0, currentTop);
        }

        static void ChangeVolume(float change)
        {
            volume = Math.Clamp(volume + change, 0.0f, 1.0f);
            if (waveOut != null)
            {
                waveOut.Volume = volume;
            }
            DisplayVolume();
        }

        static void ClearLine()
        {
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        static void ClearPlaylistDisplay()
        {
            for (int i = 5; i < Console.WindowHeight; i++)
            {
                Console.SetCursorPosition(0, i);
                ClearLine();
            }
        }

        static void DisplayPlaylist()
        {
            Console.ForegroundColor = playlistForegroundConsoleColour;
            Console.BackgroundColor = playlistBackgroundConsoleColour;
            ClearPlaylistDisplay();
            Console.SetCursorPosition(0, 6);

            for (int i = 0; i < playlist.Count; i++)
            {
                string trackDisplay = playlist[i];
                if (i == currentTrackIndex)
                {
                    trackDisplay = "* " + trackDisplay;
                }
                else
                {
                    trackDisplay = "  " + trackDisplay;
                }

                Console.WriteLine(trackDisplay.PadRight(Console.WindowWidth));
            }
        }

        static string ReadLineWithTabCompletion(string initialDirectory)
        {
            var input = new StringBuilder(initialDirectory);
            var suggestions = new List<string>();
            int suggestionIndex = -1;

            Console.ForegroundColor = textForegroundConsoleColour;
            Console.BackgroundColor = textBackgroundConsoleColour;
            Console.Write(input.ToString());

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return input.ToString();
                }
                else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input.Remove(input.Length - 1, 1);
                    Console.Write("\b \b");
                }
                else if (key.Key == ConsoleKey.Tab)
                {
                    if (suggestions.Count == 0)
                    {
                        //TODO: Handle invalid input, i.e. directory does not exist
                        var prefix = input.ToString();
                        var directory = Path.GetDirectoryName(prefix) ?? ".";
                        var fileName = Path.GetFileName(prefix) ?? string.Empty;
                        suggestions = Directory.GetFiles(directory, fileName + "*")
                            .Concat(Directory.GetDirectories(directory, fileName + "*"))
                            .ToList();
                        suggestionIndex = -1;
                    }

                    if (suggestions.Count > 0)
                    {
                        if ((key.Modifiers & ConsoleModifiers.Shift) != 0)
                        {
                            suggestionIndex = (suggestionIndex - 1 + suggestions.Count) % suggestions.Count;
                        }
                        else
                        {
                            suggestionIndex = (suggestionIndex + 1) % suggestions.Count;
                        }

                        var suggestion = suggestions[suggestionIndex];
                        Console.Write(new string('\b', input.Length) + new string(' ', input.Length) + new string('\b', input.Length));
                        input.Clear();
                        input.Append(suggestion);
                        Console.Write(suggestion);
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    input.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                    suggestions.Clear();
                    suggestionIndex = -1;
                }
            }
        }

        static void DisplayProgress()
        {
            Console.CursorVisible = false;
            if (audioFileReader != null && waveOut != null)
            {
                TimeSpan currentPosition = audioFileReader.CurrentTime;
                TimeSpan totalLength = audioFileReader.TotalTime;
                string progressText = $"Progress: {currentPosition:hh\\:mm\\:ss} / {totalLength:hh\\:mm\\:ss}";
                Console.SetCursorPosition(0, 3);
                Console.Write(progressText);
            }
            else
            {
                Console.SetCursorPosition(0, 3);
                Console.Write("Progress: 00:00 / 00:00");
            }
            Console.CursorVisible = true;
        }

        // https://stackoverflow.com/a/29192463/9659
        static ConsoleColor FromColour(Color c)
        {
            int index = (c.R > 128 | c.G > 128 | c.B > 128) ? 8 : 0; // Bright bit
            index |= (c.R > 64) ? 4 : 0; // Red bit
            index |= (c.G > 64) ? 2 : 0; // Green bit
            index |= (c.B > 64) ? 1 : 0; // Blue bit
            return (System.ConsoleColor)index;
        }
    }
}